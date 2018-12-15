using System;
using System.Collections.Generic;

namespace InvertedTomato.IO.Mictrack.Models
{
    public class Beacon
    {
        /// <summary>
        /// IMEI (15 digits).
        /// </summary>
        public String IMEI { get; set; } // 15 digits

        /// <summary>
        /// GPRS user name.
        /// </summary>
        public String GPRSUsername { get; set; }

        /// <summary>
        /// GPRS password.
        /// </summary>
        public String GPRSPassword { get; set; }

        /// <summary>
        /// Upload status.
        /// </summary>
        public Statuses Status { get; set; }

        /// <summary>
        /// Position records.
        /// </summary>
        public IList<BeaconRecord> Records { get; set; }

        public enum Statuses : Byte
        {
            /// <summary>
            /// Normal mode.
            /// </summary>
            None,
            /// <summary>
            /// Power saving mode and vehicle stopped.
            /// </summary>
            PowerSaveStopped,
            /// <summary>
            /// Power saving mode and vehicle moving.
            /// </summary>
            PowerSaveMoving,
            /// <summary>
            /// Call alert (MP90 only).
            /// </summary>
            Call,
            /// <summary>
            /// Unplug alert.
            /// </summary>
            Disconnect,
            /// <summary>
            /// High temperature alert.
            /// </summary>
            HighTemperature,
            /// <summary>
            /// Internal battery low voltage.
            /// </summary>
            InternalBatteryLow,
            /// <summary>
            /// External battery low voltage.
            /// </summary>
            ExternalBatteryLow,
            /// <summary>
            /// Out of the Geo-fence alarm.
            /// </summary>
            GeoFenceEnter,
            /// <summary>
            /// Enter the Geo-fence alarm.
            /// </summary>
            GeoFenceExit,
            /// <summary>
            /// Over-speed alarm.
            /// </summary>
            SpeedLimitOver,
            /// <summary>
            /// Safe-speed alarm.
            /// </summary>
            SpeedLimitUnder
        }
    }
}