using System;
using System.Net;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnErrorEventArgs
    {
        public IPEndPoint RemoteAddress { get; set; }

        public String RemoteAddressString { get { return RemoteAddress.ToString(); } }

        public String Message { get; set; }
    }
}