using System;

namespace InvertedTomato.IO.Mictrack.Models
{
    /// <summary>
    /// Beacons can potentially contain multiple records providing a location and heading at a specific time.
    /// </summary>
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
        /// Latitude at time of reading (ddmm.mmmm).
        /// </summary>
        public Double Latitude { get; set; }

        /// <summary>
        /// North or south.
        /// </summary>
        public LatitudeIndicators LatitudeIndicator { get; set; }

        /// <summary>
        /// Longitude at time of reading (Dddmm.mmmm).
        /// </summary>
        public Double Longitude { get; set; }

        /// <summary>
        /// East or west.
        /// </summary>
        public LongitudeIndicators LongitudeIndicator { get; set; }

        /// <summary>
        /// Speed at ground level, in knots.
        /// </summary>
        public Double GroundSpeed { get; set; }

        /// <summary>
        /// Bearing, in degreese.
        /// </summary>
        /// <remarks>
        /// When ground-speed is around 0 this becomes somewhat random.
        /// </remarks>
        public Double Bearing { get; set; }

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