using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    public class MictrackReceiver : IDisposable
    {
        private const Int32 RXBUFFER_LENGTH = 128;

        public delegate void OnBeaconEventHandler(Object sender, OnBeaconEventArgs e);

        public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);

        public event OnBeaconEventHandler OnBeacon;

        public event OnErrorEventHandler OnError;

        public Boolean IsDisposed { get; private set; }

        public Boolean IsRunning { get; private set; }

        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 5000); // TODO: Is there an "official" default port?

        public Int32 ConnectionBacklogLimit { get; set; } = 10;

        private readonly Socket Listener;
        private readonly Object Sync = new Object();

        public MictrackReceiver()
        {
            // Create listener socket
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            // Check that we are NOT already running in a thread-safe manner
            lock (Sync)
            {
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
                ProcessMessage(message);
            }
            else
            {
                // Receive more
                ReceiveStart(state);
            }
        }
        private void ProcessMessage(String message)
        {
            // #861108034747229#MT600#0000#AUTOLOW#1
            // #00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52
            // ##
        }
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                // Dispose managed state (managed objects)
                Listener?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}