using System;
using System.Net.Sockets;
using System.Text;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class ConnectionState
    {
        public Socket Connection { get; set; }

        public Byte[] BinaryBuffer { get; set; }

        public String StringBuffer { get; set; } // Considered StringBuilder, but it would probably result in poorer performance in this scenario (untested theory)

        public String RemoteAddressString { get; set; }
    }
}