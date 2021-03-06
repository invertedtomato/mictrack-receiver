
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    /// <summary>
    /// Internal message parsing according to the Mictrack MP90 communication protocol (which appears to be based around the RMC protocol) in accordance with https://www.mictrack.com/downloads/protocols/Mictrack_Communication_Protocol_For_MP90&MP90-NB.pdf
    /// </summary>
    public static class MessageParser
    {
        /// <summary>
        /// Character used to separate tokens (CSV style) on the header line.
        /// </summary>
        private const Char HEADER_TOKENSEPERATOR = '#';

        /// <summary>
        /// Character used to separate tokens (CSV style) on the header line.
        /// </summary>
        private const Char RECORD_TOKENSEPERATOR = ',';

        /// <summary>
        /// Format strings used to parse date/time strings on records.
        /// </summary>
        private static readonly String[] AT_FORMATS = new String[] { "ddMMyy HHmmss", "ddMMyy HHmmss.f", "ddMMyy HHmmss.ff", "ddMMyy HHmmss.fff" };

        /// <summary>
        /// Parse a full message.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the message does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static Beacon Parse(String message)
        {
            if (null == message)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // See the protocol documentation https://www.mictrack.com/downloads/protocols/Mictrack_Communication_Protocol_For_MP90&MP90-NB.pdf
            // Inbound messages take the following form:
            // ----------------
            // #<IMEI>#<A>#<B>#<C>#<D>[CR][LF]
            // #<Base ID><Message ID>,<UTC time>,<Status>,<Latitude>,<N/S Indicator>, <Longitude>,<E/W Indicator>,<Speed Over Ground>,<Course Over Ground>,<Date>,,,<Checksum>,[CR][LF]
            // ##
            // ----------------

            // For example:
            // ----------------
            // #861108034747229#MT600#0000#AUTOLOW#1
            // #00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52
            // ##
            // ----------------

            // Split into lines
            var lines = message.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            // Check basic sanity
            if (lines.Length < 3)
            {
                throw new ProtocolViolationException("Message does not contain at least three lines.");
            }
            if (lines[lines.Length - 2] != "##")
            {
                throw new ProtocolViolationException("Second last line of message is not '##'.");
            }
            if (lines[lines.Length - 1] != "")
            {
                throw new ProtocolViolationException("Last line of message is not blank.");
            }

            // Split header into tokens
            var tokens = lines[0].Split(HEADER_TOKENSEPERATOR); // Header tokens are separated by "#"
            if (tokens.Length < 6)
            {
                throw new ProtocolViolationException($"Header line contains {tokens.Length} tokens, rather than the minimum 6 expected.");
            }

            // Parse header
            var beacon = new Beacon()
            {
                IMEI = ParseGenericString(tokens[1], "IMEI"),
                GPRSUsername = ParseGenericString(tokens[2], "GPRSUsername"),
                GPRSPassword = ParseGenericString(tokens[3], "GPRSPassword"),
                Status = ParseHeaderStatus(tokens[4]),
                Records = new List<BeaconRecord>()
            };

            // Data Quantity
            var dataQuantity = ParseGenericInteger(tokens[5], "DataQuantity");
            if (lines.Length - 3 != dataQuantity)
            {
                throw new ProtocolViolationException($"Number of lines does not match Data Quantity of '{dataQuantity}'.");
            }

            // Loop through each record line
            for (var i = 1; i < lines.Length - 2; i++) // Ignoring first header line and last two tail lines
            {
                // Split record into tokens
                tokens = lines[i].Split(RECORD_TOKENSEPERATOR); // Record tokens are seperated by ','. No - I don't know why this differs from the header.
                if (tokens.Length < 13)
                {
                    throw new ProtocolViolationException($"Lines contains {tokens.Length} tokens, rather than the minimum expected 13.");
                }

                // Add record to output
                beacon.Records.Add(new BeaconRecord()
                {
                    BaseIdentifier = ParseGenericString(tokens[0], "BaseIdentifier"),
                    At = ParseAt(tokens[9], tokens[1]),
                    Status = ParseStatus(tokens[2]),
                    Latitude = ParseGenericDouble(tokens[3], "Latitude"),
                    LatitudeIndicator = ParseLatitudeIndicator(tokens[4]),
                    Longitude = ParseGenericDouble(tokens[5], "Longitude"),
                    LongitudeIndicator = ParseLongitudeIndicator(tokens[6]),
                    GroundSpeed = ParseGenericDouble(tokens[7], "GroundSpeed"),
                    Bearing = ParseGenericDouble(tokens[8], "Bearing")
                });
            }

            return beacon;
        }

        /// <summary>
        /// Parse the beacon status found in the header.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static Beacon.Statuses ParseHeaderStatus(String input)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }

            switch (input.ToUpperInvariant())
            {
                case "AUTO": return Beacon.Statuses.None;
                case "AUTOLOW": return Beacon.Statuses.PowerSaveStationary;
                case "TOWED": return Beacon.Statuses.PowerSaveMoving;
                case "CALL": return Beacon.Statuses.Call;
                case "DEF": return Beacon.Statuses.Disconnect;
                case "HT": return Beacon.Statuses.HighTemperature;
                case "BLP": return Beacon.Statuses.InternalBatteryLow;
                case "CLP": return Beacon.Statuses.ExternalBatteryLow;
                case "OS": return Beacon.Statuses.GeoFenceExit;
                case "RS": return Beacon.Statuses.GeoFenceEnter;
                case "OVERSPEED": return Beacon.Statuses.SpeedLimitOver;
                case "SAFESPEED": return Beacon.Statuses.SpeedLimitUnder;
                default: throw new ProtocolViolationException($"Unable to parse Header Status of '{input}'.");
            }
        }

        /// <summary>
        /// Parse a record date/time.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static DateTime ParseAt(String dateInput, String timeInput)
        {
            if (null == dateInput)
            {
                throw new ArgumentNullException(nameof(dateInput));
            }
            if (null == timeInput)
            {
                throw new ArgumentNullException(nameof(timeInput));
            }


            var input = $"{dateInput} {timeInput}";
            if (!DateTime.TryParseExact(input, AT_FORMATS, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var output))
            {
                throw new ProtocolViolationException($"Unable to parse At from '{input}'.");
            }
            return DateTime.SpecifyKind(output, DateTimeKind.Utc); // TODO: Need to consider this one, without this the output is correct, but has been converted to local TZ
        }

        /// <summary>
        /// Parse a record status.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static BeaconRecord.Statuses ParseStatus(String input)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }

            switch (input.ToUpperInvariant())
            {
                case "A": return BeaconRecord.Statuses.Valid;
                case "V": return BeaconRecord.Statuses.Invalid;
                default: throw new ProtocolViolationException($"Unable to parse Status from '{input}'.");
            }
        }

        /// <summary>
        /// Parse a latitude indicator.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static BeaconRecord.LatitudeIndicators ParseLatitudeIndicator(String input)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }

            switch (input.ToUpperInvariant())
            {
                case "N": return BeaconRecord.LatitudeIndicators.North;
                case "S": return BeaconRecord.LatitudeIndicators.South;
                default: throw new ProtocolViolationException($"Unable to parse LatitudeIndicator from '{input}'.");
            }
        }

        /// <summary>
        /// Parse a longitude indicator.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static BeaconRecord.LongitudeIndicators ParseLongitudeIndicator(String input)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }

            switch (input.ToUpperInvariant())
            {
                case "E": return BeaconRecord.LongitudeIndicators.East;
                case "W": return BeaconRecord.LongitudeIndicators.West;
                default: throw new ProtocolViolationException($"Unable to parse LongitudeIndicator from '{input}'.");
            }
        }

        /// <summary>
        /// Parse a string. I know this seems dumb, but it allows for consistency and it does handle empty strings as required.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static String ParseGenericString(String input, String field)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (null == field)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (input == String.Empty)
            {
                throw new ProtocolViolationException($"{field} is blank when it must contain a value.");
            }

            return input;
        }

        /// <summary>
        /// Parse a integer.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        public static Int32 ParseGenericInteger(String input, String field)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (null == field)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (!Int32.TryParse(input, out var output))
            {
                throw new ProtocolViolationException($"Unable to parse {field} from '{input}'.");
            }
            return output;
        }

        /// <summary>
        /// Parse a double.
        /// </summary>
        /// <exception cref="ProtocolViolationException">
        /// Raised when the field does not comply with the protocol standard and cannot be parsed.
        /// </exception>
        /// <remarks>
        /// GPS Trackers implementing the protocol seem to provide nil values at empty strings. This seems strange, but it occurs at least on a nil ground speed on at MT600. This is mapped back to 0 in this parser.
        /// </remarks>
        public static Double ParseGenericDouble(String input, String field)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (null == field)
            {
                throw new ArgumentNullException(nameof(field));
            }

            // This protocol seems to consider empty strings and zeros as identically for doubles (check out a stationary GPS's ground speed), therefore make this a little more sane so it's easier for the downstream to handle
            if (input == String.Empty)
            {
                return 0;
            }

            if (!Double.TryParse(input, out var output))
            {
                throw new ProtocolViolationException($"Unable to parse {field} from '{input}'.");
            }
            return output;
        }
    }
}