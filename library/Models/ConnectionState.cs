using System;
using System.Net.Sockets;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class ConnectionState
    {
        public Socket Connection { get; set; }

        public Int32 Position { get; set; }

        public Byte[] Buffer { get; set; }

        public String RemoteAddressString { get; set; }
    }
}