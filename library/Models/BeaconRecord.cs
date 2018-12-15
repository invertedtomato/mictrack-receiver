using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class BeaconRecord
    {
        /// <summary>
        /// LAC+CI (Hex)
        /// </summary>
        /// <example>
        /// 262c0f48 (2G SIM Card)
        /// a52d15e5803 (3G SIM Card)
        /// </summary>
        public String BaseIdentifier { get; set; }

        /// <summary>
        /// Date and time the record was captured.
        /// </summary>
        public DateTime At { get; set; }

        /// <summary>
        /// Valid or invalid.
        /// </summary>
        public Statuses Status { get; set; }

        /// <summary>
        /// Latitude at time of reading (ddmm.mmmm)
        /// </summary>
        public Double Latitude { get; set; }

        /// <summary>
        /// North or south.
        /// </summary>
        public LatitudeIndicators LatitudeIndicator { get; set; }

        /// <summary>
        /// Longitude at time of reading (Dddmm.mmmm)
        /// </summary>
        public Double Longitiude { get; set; }

        /// <summary>
        /// East or west.
        /// </summary>
        public LongitudeIndicators LongitiudeIndicator { get; set; }

        /// <summary>
        /// Speed at ground level, in knots.
        /// </summary>
        public Double GroundSpeed { get; set; }

        /// <summary>
        /// Bearing, in degreese.
        /// </summary>
        public Double? Bearing { get; set; } // Degrees

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