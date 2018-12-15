using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnBeaconEventArgs
    {
        public String RemoteAddressString { get; set; }

        public Beacon Beacon { get; set; }
    }
}