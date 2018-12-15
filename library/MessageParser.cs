
using System;
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


            var lines = message.Split(new Char[] { '\r', '\n' });
            if (lines.Length != 2)
            {
                throw new ProtocolViolationException("Message does not contain precisely two '\\r\\n's.");
            }

            var tokens1 = lines[0].Split('#');
            if (tokens1.Length != 4)
            {
                throw new ProtocolViolationException("First line does not contain precisely 5 '#'s.");
            }

            var tokens2 = lines[1].Split(',');
            if (tokens2.Length != 13)
            {
                throw new ProtocolViolationException("Second lines does not contain precisely 13 ','s.");
            }

            if (lines[2] != "##")
            {
                throw new ProtocolViolationException("Third line does not consist of exactly '##'.");
            }

            // IMEI
            var imei = tokens1[1];

            // GPRSUsername
            var gprsUsername = tokens1[2];

            // GPRSPassword
            var gprsPassword = tokens1[3];

            // Event ("status")
            Beacon.Events evt;
            switch (tokens1[4])
            {
                case "AUTO":
                    evt = Beacon.Events.None;
                    break;
                case "AUTOLOW":
                    evt = Beacon.Events.PowerSaveStopped;
                    break;
                case "TOWED":
                    evt = Beacon.Events.PowerSaveMoving;
                    break;
                case "CALL":
                    evt = Beacon.Events.Call;
                    break;
                case "DEF":
                    evt = Beacon.Events.Disconnect;
                    break;
                case "HT":
                    evt = Beacon.Events.HighTemperature;
                    break;
                case "BLP":
                    evt = Beacon.Events.InternalBatteryLow;
                    break;
                case "CLP":
                    evt = Beacon.Events.ExternalBatteryLow;
                    break;
                case "OS":
                    evt = Beacon.Events.GeoFenceExit;
                    break;
                case "RS":
                    evt = Beacon.Events.GeoFenceEnter;
                    break;
                case "OVERSPEED":
                    evt = Beacon.Events.SpeedLimitOver;
                    break;
                case "SAFESPEED":
                    evt = Beacon.Events.SpeedLimitUnder;
                    break;
                default:
                    throw new ProtocolViolationException($"Unable to parse Event from '{tokens1[4]}'.");
            }

            // "Quantity"
            var dataQuantity = tokens1[5];

            // BaseID
            var baseIdentifier = tokens2[1];

            // "MessageID"
            if (tokens2[2] != "$GPRMC")
            {
                throw new ProtocolViolationException($"Incorrect message header. Found '{tokens2[2]}' but expecting '$GPRMC'.");

            }

            // At
            if (!DateTime.TryParseExact(tokens2[11] + " " + tokens2[3], "DDMMYY hhMMss.ss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var at))
            {
                throw new ProtocolViolationException($"Unable to parse At from '{tokens2[11]} {tokens2[3]}'.");
            }

            // Status
            Beacon.Statuses status;
            switch (tokens2[12])
            {
                case "A":
                    status = Beacon.Statuses.Valid;
                    break;
                case "V":
                    status = Beacon.Statuses.Invalid;
                    break;
                default:
                    throw new ProtocolViolationException($"Unable to parse Status from '{tokens2[12]}'.");
            }

            // Latitude
            if (!Double.TryParse(tokens2[13], out var latitude))
            {
                throw new ProtocolViolationException($"Unable to parse Latitude from '{tokens2[13]}'.");
            }

            // LatitudeIndicator
            Beacon.LatitudeIndicators latitudeIndicator;
            switch (tokens2[14])
            {
                case "N":
                    latitudeIndicator = Beacon.LatitudeIndicators.North;
                    break;
                case "S":
                    latitudeIndicator = Beacon.LatitudeIndicators.South;
                    break;
                default:
                    throw new ProtocolViolationException($"Unable to parse LatitudeIndicator from '{tokens2[14]}'.");
            }

            // Longitude
            if (!Double.TryParse(tokens2[15], out var longitude))
            {
                throw new ProtocolViolationException($"Unable to parse Latitude from '{tokens2[15]}'.");
            }

            // LongitudeIndicator
            Beacon.LongitudeIndicators longitudeIndicator;
            switch (tokens2[16])
            {
                case "E":
                    longitudeIndicator = Beacon.LongitudeIndicators.East;
                    break;
                case "W":
                    longitudeIndicator = Beacon.LongitudeIndicators.West;
                    break;
                default:
                    throw new ProtocolViolationException($"Unable to parse LongitudeIndicator from '{tokens2[16]}'.");
            }

            // GroundSpeed
            if (!Double.TryParse(tokens2[17], out var groundSpeed))
            {
                throw new ProtocolViolationException($"Unable to parse GroundSpeed from '{tokens2[17]}'.");
            }

            // Bearing
            if (!Double.TryParse(tokens2[18], out var bearing))
            {
                throw new ProtocolViolationException($"Unable to parse Bearing from '{tokens2[18]}'.");
            }

            // "Checksum"
            var checksum = tokens2[19];

            // Create beacon
            return new Beacon()
            {
                IMEI = imei,
                GPRSUsername = gprsUsername,
                GPRSPassword = gprsPassword,
                Event = evt,

                BaseIdentifier = baseIdentifier,

                At = at,
                Status = status,
                Latitude = latitude,
                LatitudeIndicator = latitudeIndicator,
                Longitiude = longitude,
                LongitiudeIndicator = longitudeIndicator,
                GroundSpeed = groundSpeed,
                Bearing = bearing
            };
        }
    }
}