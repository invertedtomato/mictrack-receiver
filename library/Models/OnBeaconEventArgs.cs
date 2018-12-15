using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class OnBeaconEventArgs
    {
        public String RemoteAddressString { get; set; }

        public MictrackBeacon Beacon { get; set; }
    }
}