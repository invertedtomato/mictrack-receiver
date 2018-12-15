using System;
using System.Net.Sockets;
using System.Text;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class ConnectionState
    {
        public Socket Connection { get; set; }

        public Byte[] Buffer1 { get; set; }

        public String Buffer2 { get; set; } // Minimal advantage of using a StringBuilder here

        public String RemoteAddressString { get; set; }
    }
}