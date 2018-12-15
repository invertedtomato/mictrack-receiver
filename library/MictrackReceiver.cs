using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    public class MictrackReceiver : IDisposable
    {
        /// <summary>
        /// Raised when a beacon arrives from a GPS.
        /// </summary>
        public event OnBeaconEventHandler OnBeacon;

        /// <summary>
        /// Raised when an error occurs when communicating with a GPS. This generally not fatal as the GPS should try again shortly.
        /// </summary>
        public event OnErrorEventHandler OnError;

        /// <summary>
        /// If the receiver has been disposed and needs to be reinstantiated before use.
        /// </summary>
        public Boolean IsDisposed { get; private set; }

        /// <summary>
        /// If the receiver is currently running and available to recieve beacons.
        /// </summary>
        public Boolean IsRunning { get; private set; }

        /// <summary>
        /// The local endpoint used for listening. This is where you can set the listening port and/or IP address.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 5000); // TODO: Is there an "official" default port?

        /// <summary>
        /// The number of pending connections that will be queued waiting to be processed.
        /// <summary>
        public Int32 ConnectionBacklogLimit { get; set; } = 10;

        /// <summary>
        /// Size of first (byte array) receive buffer. Messages longer than this can be received, however will take multiple context switches.
        /// </summary>
        private const Int32 RXBUFFER1_LENGTH = 256;

        private const String MESSAGE_SEPERATOR = "##\r\n";

        public delegate void OnBeaconEventHandler(Object sender, OnBeaconEventArgs e);

        public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);

        private readonly Socket Listener;

        private readonly Object Sync = new Object();

        public MictrackReceiver()
        {
            // Create listener socket
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Start listening for beacons.
        /// </summary>
        public void Start()
        {
            // Check that we are NOT already running in a thread-safe manner
            lock (Sync)
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException("Receiver has been disposed.");
                }
                if (IsRunning)
                {
                    throw new InvalidOperationException("Receiver already running.");
                }
                IsRunning = true;
            }

            // Bind socket and listen for connections
            Listener.Bind(LocalEndPoint);
            Listener.Listen(ConnectionBacklogLimit);

            // Seed accepting
            AcceptStart();
        }

        private void AcceptStart()
        {
            try
            {
                Listener.BeginAccept(new AsyncCallback(AcceptEnd), null);
            }
            catch (ObjectDisposedException) { } // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
        }

        private void AcceptEnd(IAsyncResult result)
        {
            Socket connection;
            try
            {
                // Complete accept
                connection = Listener.EndAccept(result);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort accept
            }

            // Create state
            var state = new ConnectionState()
            {
                Buffer1 = new byte[RXBUFFER1_LENGTH],
                Buffer2 = string.Empty,
                Connection = connection,
                RemoteAddressString = ((IPEndPoint)connection.RemoteEndPoint).Address.ToString() // This is not available on the socket once it's error'd, so capture it now
            };

            // Start read cycle
            ReceiveStart(state);

            // Start next accept cycle
            AcceptStart();
        }

        private void ReceiveStart(ConnectionState state)
        {
            // Begin receive
            try
            {
                state.Connection.BeginReceive(state.Buffer1, 0, state.Buffer1.Length, SocketFlags.None, new AsyncCallback(ReceiveEnd), state);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }
            catch (SocketException ex) // Occurs when there is a connection error
            {
                // Report error
                OnError?.Invoke(this, new OnErrorEventArgs()
                {
                    Message = ex.Message,
                    RemoteAddressString = state.RemoteAddressString
                });
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }
        }

        private void ReceiveEnd(IAsyncResult ar)
        {
            // Retrieve state
            var state = (ConnectionState)ar.AsyncState;

            // Complete read
            Int32 len;
            try
            {
                len = state.Connection.EndReceive(ar);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort reawd
            }
            catch (SocketException ex) // Occurs when there is a connection error
            {
                // Report error
                OnError?.Invoke(this, new OnErrorEventArgs()
                {
                    Message = ex.Message,
                    RemoteAddressString = state.RemoteAddressString
                });
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }

            // Decode chunk in first buffer
            var chunk = Encoding.ASCII.GetString(state.Buffer1, 0, len);

            // Append to second buffer
            state.Buffer2 += chunk;

            // Cycle through each message in the buffer (0 or more)
            var pos = state.Buffer2.IndexOf(MESSAGE_SEPERATOR); // TODO: this buffer management smells bad - it'll work, but I'm sure there's a more performant design
            while (pos > 0)
            {
                // Extract next message from buffer
                var message = state.Buffer2.Substring(0, pos + MESSAGE_SEPERATOR.Length);

                // Trim buffer
                state.Buffer2 = state.Buffer2.Substring(pos + MESSAGE_SEPERATOR.Length);

                // Find next message, if present
                pos = state.Buffer2.IndexOf(MESSAGE_SEPERATOR);

                // Process message
                try
                {
                    // Parse message
                    var beacon = MessageParser.Parse(message);

                    // Raise event
                    OnBeacon?.Invoke(this, new OnBeaconEventArgs()
                    {
                        Beacon = beacon,
                        RemoteAddressString = state.RemoteAddressString
                    });
                }
                catch (ProtocolViolationException ex)
                {
                    // Report error
                    OnError?.Invoke(this, new OnErrorEventArgs()
                    {
                        Message = ex.Message,
                        RemoteAddressString = state.RemoteAddressString
                    });
                }
            }

            // Receive more
            ReceiveStart(state);
        }
        protected virtual void Dispose(bool disposing)
        {
            lock (Sync)
            {
                if (IsDisposed)
                {
                    return;
                }
                IsDisposed = true;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects)
                Listener?.Dispose();
                IsRunning = false;
            }
        }

        /// <summary>
        /// Stop listening for beacons if started, and then destroy all managed resourceas.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}