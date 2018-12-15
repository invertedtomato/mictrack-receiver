
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    public static class MessageParser
    {
        public static Beacon Parse(String message)
        {
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

            // Prepare output
            var beacon = new Beacon()
            {
                Records = new List<BeaconRecord>()
            };

            // Prepare parse header
            var tokens = lines[0].Split('#');
            if (tokens.Length < 6)
            {
                throw new ProtocolViolationException($"Header line contains {tokens.Length} hashes, rather than the minimum 6 expected.");
            }

            // IMEI
            beacon.IMEI = tokens[1];

            // GPRSUsername
            beacon.GPRSUsername = tokens[2];

            // GPRSPassword
            beacon.GPRSPassword = tokens[3];

            // Event ("status")
            switch (tokens[4])
            {
                case "AUTO":
                    beacon.Status = Beacon.Statuses.None;
                    break;
                case "AUTOLOW":
                    beacon.Status = Beacon.Statuses.PowerSaveStationary;
                    break;
                case "TOWED":
                    beacon.Status = Beacon.Statuses.PowerSaveMoving;
                    break;
                case "CALL":
                    beacon.Status = Beacon.Statuses.Call;
                    break;
                case "DEF":
                    beacon.Status = Beacon.Statuses.Disconnect;
                    break;
                case "HT":
                    beacon.Status = Beacon.Statuses.HighTemperature;
                    break;
                case "BLP":
                    beacon.Status = Beacon.Statuses.InternalBatteryLow;
                    break;
                case "CLP":
                    beacon.Status = Beacon.Statuses.ExternalBatteryLow;
                    break;
                case "OS":
                    beacon.Status = Beacon.Statuses.GeoFenceExit;
                    break;
                case "RS":
                    beacon.Status = Beacon.Statuses.GeoFenceEnter;
                    break;
                case "OVERSPEED":
                    beacon.Status = Beacon.Statuses.SpeedLimitOver;
                    break;
                case "SAFESPEED":
                    beacon.Status = Beacon.Statuses.SpeedLimitUnder;
                    break;
                default:
                    throw new ProtocolViolationException($"Unable to parse Header Status of '{tokens[4]}'.");
            }

            // "Quantity"
            if (!Int32.TryParse(tokens[5], out var dataQuantity))
            {
                throw new ProtocolViolationException($"Unable to parse Data Quantity of '{tokens[5]}'.");
            }
            if (lines.Length - 3 != dataQuantity)
            {
                throw new ProtocolViolationException($"Number of lines does not match Data Quantity of '{dataQuantity}'.");
            }

            for (var i = 1; i < lines.Length - 2; i++)
            {
                tokens = lines[i].Split(',');
                if (tokens.Length < 13)
                {
                    throw new ProtocolViolationException($"Lines contains {tokens.Length} tokens, rather than the minimum expected 13.");
                }

                // BaseID
                var baseIdentifier = tokens[0];

                // At
                if (!DateTime.TryParseExact(tokens[9] + " " + tokens[1], "ddMMyy HHmmss.ff", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var at))
                {
                    throw new ProtocolViolationException($"Unable to parse At from '{tokens[9]} {tokens[1]}'.");
                }

                // Status
                BeaconRecord.Statuses status;
                switch (tokens[2])
                {
                    case "A":
                        status = BeaconRecord.Statuses.Valid;
                        break;
                    case "V":
                        status = BeaconRecord.Statuses.Invalid;
                        break;
                    default:
                        throw new ProtocolViolationException($"Unable to parse Status from '{tokens[2]}'.");
                }

                // Latitude
                if (!Double.TryParse(tokens[3], out var latitude))
                {
                    throw new ProtocolViolationException($"Unable to parse Latitude from '{tokens[3]}'.");
                }

                // LatitudeIndicator
                BeaconRecord.LatitudeIndicators latitudeIndicator;
                switch (tokens[4])
                {
                    case "N":
                        latitudeIndicator = BeaconRecord.LatitudeIndicators.North;
                        break;
                    case "S":
                        latitudeIndicator = BeaconRecord.LatitudeIndicators.South;
                        break;
                    default:
                        throw new ProtocolViolationException($"Unable to parse LatitudeIndicator from '{tokens[4]}'.");
                }

                // Longitude
                if (!Double.TryParse(tokens[5], out var longitude))
                {
                    throw new ProtocolViolationException($"Unable to parse Latitude from '{tokens[5]}'.");
                }

                // LongitudeIndicator
                BeaconRecord.LongitudeIndicators longitudeIndicator;
                switch (tokens[6])
                {
                    case "E":
                        longitudeIndicator = BeaconRecord.LongitudeIndicators.East;
                        break;
                    case "W":
                        longitudeIndicator = BeaconRecord.LongitudeIndicators.West;
                        break;
                    default:
                        throw new ProtocolViolationException($"Unable to parse LongitudeIndicator from '{tokens[6]}'.");
                }

                // GroundSpeed
                Double groundSpeed = 0;
                if (tokens[7] != string.Empty && !Double.TryParse(tokens[7], out groundSpeed))
                {
                    throw new ProtocolViolationException($"Unable to parse GroundSpeed from '{tokens[7]}'.");
                }

                // Bearing
                Double? bearing = null;
                if (tokens[8] != string.Empty) // TODO: this block smells bad
                {
                    if (Double.TryParse(tokens[8], out var bearingCompute))
                    {
                        bearing = bearingCompute;
                    }
                    else
                    {
                        throw new ProtocolViolationException($"Unable to parse Bearing from '{tokens[8]}'.");
                    }
                }

                // "Checksum"
                var checksum = tokens[12];

                beacon.Records.Add(new BeaconRecord()
                {
                    BaseIdentifier = baseIdentifier,

                    At = at,
                    Status = status,
                    Latitude = latitude,
                    LatitudeIndicator = latitudeIndicator,
                    Longitude = longitude,
                    LongitudeIndicator = longitudeIndicator,
                    GroundSpeed = groundSpeed,
                    Bearing = bearing
                });
            }

            return beacon;

        }
    }
}