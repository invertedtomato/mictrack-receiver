using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnBeaconEventArgs
    {
        /// <summary>
        /// The remote IP address as a string.
        /// </summary>
        /// <remarks>
        /// This is provided as a string rather than an IPAddress so that the consumer does not require System.Net.
        /// </remarks>
        public String RemoteAddressString { get; set; }

        /// <summary>
        /// The received beacon.
        /// </summary>
        public Beacon Beacon { get; set; }
    }
}