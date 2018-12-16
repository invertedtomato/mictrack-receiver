using System;
using System.Net;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnErrorEventArgs
    {
        /// <summary>
        /// The remote IP address as a string.
        /// </summary>
        /// <remarks>
        /// This is provided as a string rather than an IPAddress so that the consumer does not require System.Net.
        /// </remarks>
        public String RemoteAddressString { get; set; }

        /// <summary>
        /// The error message.
        /// </summary>
        public String Message { get; set; }
    }
}