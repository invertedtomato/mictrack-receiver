using System;
using System.Net;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnErrorEventArgs
    {
        public String RemoteAddressString { get; set; }

        public String Message { get; set; }
    }
}