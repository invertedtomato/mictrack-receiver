using System;
using System.Net;
using System.Net.Sockets;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    public class MictrackReceiver : IDisposable
    {
        public delegate void OnBeaconEventHandler(Object sender, OnBeaconEventArgs e);
        
        public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);

        public event OnBeaconEventHandler OnBeacon;

        public event OnErrorEventHandler OnError;

        public Boolean IsDisposed { get; private set; }

        public Boolean IsRunning { get; private set; }

        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 5000); // TODO: Is there an "official" default port?

        private readonly Socket Listener;
        private readonly Object Sync = new Object();

        public MictrackReceiver() { }

        public void Start() { }

        /*
         // Create listener socket
                    Console.WriteLine("Starting...");
                    var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        // Listen for connections
                        listener.Bind(new IPEndPoint(IPAddress.Any, LISTEN_PORT));
                        listener.Listen(RECIEVE_BACKLOG);

                        // Loop accepting connections forever
                        while (true)
                        {
                            listener.BeginAccept(new AsyncCallback((result) =>
                            {
                                var socket = listener.EndAccept(result);
                                OnAccept(socket);
                            }), null);
                        }
                    }
                    finally
                    {
                        // Close socket and release all resources
                        listener.Close();
                    }

        static void OnAccept(Socket socket)
                {
                    // Isolate remote endpoint
                    var endPoint = (IPEndPoint)socket.RemoteEndPoint;

                    // Prepare buffer
                    var buffer = new byte[BUFFER_LENGTH];
                    var pos = 0;

                    try
                    {
                        // The message may be split over one or more packets, loop receiving until we have received all packets or we know an error has occured
                        do
                        {
                            // Receive one or more packets
                            pos += socket.Receive(buffer, pos, buffer.Length - pos, SocketFlags.None);
                        } while (pos < 2 ||     // Hasn't received enough characters for the message to possibly be complete
                            buffer[pos] != '#' || buffer[pos - 1] != '#'); // Message doesn't finish as expected with "##"
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"{endPoint}: Error occured processing message. {ex.Message} ");
                        return;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine($"{endPoint}: Message exceeds {BUFFER_LENGTH} byte limit.");
                        return;
                    }
                    finally
                    {
                        socket.Close();
                    }

                    // Decode payload
                    var message = Encoding.ASCII.GetString(buffer, 0, pos);

                    // Process message
                    OnMessage(endPoint.Address, message);
                }

                private static void OnMessage(IPAddress source, string message)
                {
                    var lines = message.Split('\n');

                    // Write to output
                    output.WriteLine(lines[1]);

                    Console.WriteLine(message);

                    // #861108034747229#MT600#0000#AUTOLOW#1
                    // #00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52
                    // ##
                } */
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