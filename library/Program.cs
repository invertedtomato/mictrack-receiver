using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GpsServer
{
    class Program
    {
        // Size of the message receive buffer. This must be at least as long as the longest possible message. It is believed that the longest message is 64 characters.
        private const int BUFFER_LENGTH = 128;

        // The number of connections that can be queued waiting to be processed.
        private const int RECIEVE_BACKLOG = 5;

        static void Main(string[] args)
        {

            // #861108034747229#MT600#0000#AUTOLOW#1
            // #00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52
            // ##

            var output = File.AppendText("output.csv");
            output.AutoFlush = true;

            // Create listener socket
            Console.WriteLine("Starting...");
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Loop listening and accepting sockets
            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Any, 5000));
                listener.Listen(RECIEVE_BACKLOG);

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
                listener.Dispose();
            }
        }

        static void OnAccept(Socket socket)
        {
            try
            {
                // Prepare buffer
                var buffer = new byte[BUFFER_LENGTH];
                var pos = 0;

                // The message may be split over one or more packets, loop receiving until we have received all packets or we know an error has occured
                do
                {
                    // Receive one or more packets
                    pos += socket.Receive(buffer, pos, buffer.Length - pos, SocketFlags.None);

                    if (pos == buffer.Length)
                    {
                        // Response exceeded length of buffer and is invalid
                    }
                } while (pos < 2 ||     // Hasn't received enough characters for the message to possibly be complete
                    buffer[pos] != '#' || buffer[pos - 1] != '#'); // Message doesn't finish as expected with "##"

                socket.Close();
                socket.Dispose();

                // Decode payload
                var raw = Encoding.ASCII.GetString(buffer, 0, pos);
                var lines = raw.Split('\n');

                // Write to output
                output.WriteLine(lines[1]);

                Console.WriteLine(raw);
            }
            catch (SocketException ex)
            {

            }
            finally
            {
                socket.Dispose();
            }
        }


    }
}
