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
        /// Size of receive buffer - this must be at least as long as the longest possible message
        /// </summary>
        private const Int32 RXBUFFER_LENGTH = 256; // Documtation example is 121 bytes

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
                Buffer = new Byte[RXBUFFER_LENGTH],
                Position = 0, // Obviously not required, but here for completeness
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
            // Check buffer isn't alreay full
            if (state.Position == state.Buffer.Length)
            {
                // Report error
                OnError?.Invoke(this, new OnErrorEventArgs()
                {
                    Message = $"Message exceeds length limit of {state.Buffer.Length} bytes.",
                    RemoteAddressString = state.RemoteAddressString
                });
                return; // Abort receive 
            }

            // Begin receive
            // TODO: Handle case where we receive more data than buffer capacity
            try
            {
                state.Connection.BeginReceive(state.Buffer, state.Position, state.Buffer.Length - state.Position, SocketFlags.None, new AsyncCallback(ReceiveEnd), state);
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
            try
            {
                state.Position += state.Connection.EndReceive(ar);
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

            // If the message is complete...
            if (state.Position > 2 && state.Buffer[state.Position] == '#' && state.Buffer[state.Position - 1] == '#')
            {
                // Decode message
                var message = Encoding.ASCII.GetString(state.Buffer, 0, state.Position);

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
            else
            {
                // Receive more
                ReceiveStart(state);
            }
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