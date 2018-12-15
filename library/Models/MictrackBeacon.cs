using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class MictrackBeacon
    {
        public String IMEI { get; set; } // 15 digits
        public String GPRSUsername { get; set; }
        public String GPRSPassword { get; set; }
        public Events Event { get; set; }
        public String BaseIdentifier { get; set; }
        public DateTime At { get; set; }
        public Statuses Status { get; set; }
        public Double Latitude { get; set; }
        public LatitudeIndicators LatitudeIndicator { get; set; }
        public Double Longitiude { get; set; }
        public LongitudeIndicators LongitiudeIndicator { get; set; }
        public Double GroundSpeed { get; set; } // Knots
        public Double Bearing { get; set; } // Degrees

        public enum Events : Byte
        {
            None,
            PowerSaveStopped,
            PowerSaveMoving,
            Call,
            Disconnect,
            HighTemperature,
            InternalBatteryLow,
            ExternalBatteryLow,
            GeoFenceEnter,
            GeoFenceExit,
            SpeedLimitOver,
            SpeedLimitUnder
        }

        public enum Statuses : Byte
        {
            Valid,
            Invalid
        }

        public enum LatitudeIndicators : Byte
        {
            North,
            South
        }

        public enum LongitudeIndicators : Byte
        {
            East,
            West
        }
    }



}