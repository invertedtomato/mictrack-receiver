using System;
using InvertedTomato.IO.Mictrack;
using InvertedTomato.IO.Mictrack.Models;
using Xunit;
using System.Linq;
using System.Net;

namespace InvertedTomato.IO.Mictrace
{
    public class MessageParserTests
    {

        [Fact]
        public void ParseHeaderStatus()
        {
            // Standard cases
            Assert.Equal(Beacon.Statuses.None, MessageParser.ParseHeaderStatus("AUTO"));
            Assert.Equal(Beacon.Statuses.PowerSaveStationary, MessageParser.ParseHeaderStatus("AUTOLOW"));
            Assert.Equal(Beacon.Statuses.PowerSaveMoving, MessageParser.ParseHeaderStatus("TOWED"));
            Assert.Equal(Beacon.Statuses.Call, MessageParser.ParseHeaderStatus("CALL"));
            Assert.Equal(Beacon.Statuses.Disconnect, MessageParser.ParseHeaderStatus("DEF"));
            Assert.Equal(Beacon.Statuses.HighTemperature, MessageParser.ParseHeaderStatus("HT"));
            Assert.Equal(Beacon.Statuses.InternalBatteryLow, MessageParser.ParseHeaderStatus("BLP"));
            Assert.Equal(Beacon.Statuses.ExternalBatteryLow, MessageParser.ParseHeaderStatus("CLP"));
            Assert.Equal(Beacon.Statuses.GeoFenceExit, MessageParser.ParseHeaderStatus("OS"));
            Assert.Equal(Beacon.Statuses.GeoFenceEnter, MessageParser.ParseHeaderStatus("RS"));
            Assert.Equal(Beacon.Statuses.SpeedLimitOver, MessageParser.ParseHeaderStatus("OVERSPEED"));
            Assert.Equal(Beacon.Statuses.SpeedLimitUnder, MessageParser.ParseHeaderStatus("SAFESPEED"));

            // Edge cases
            Assert.Equal(Beacon.Statuses.None, MessageParser.ParseHeaderStatus("auto"));
            Assert.Equal(Beacon.Statuses.SpeedLimitUnder, MessageParser.ParseHeaderStatus("safespeed"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseHeaderStatus(""); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseHeaderStatus("CAKE"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseHeaderStatus(" AUTO"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseHeaderStatus("AUTO "); });
        }

        [Fact]
        public void ParseAt()
        {
            // Standard cases
            Assert.Equal(new DateTime(2001, 1, 1, 1, 1, 1, 0), MessageParser.ParseAt("010101", "010101.000"));
            Assert.Equal(new DateTime(2020, 12, 31, 23, 59, 59, 999), MessageParser.ParseAt("311220", "235959.999"));

            // Edge cases
            Assert.Equal(new DateTime(2001, 1, 1, 1, 1, 1, 0), MessageParser.ParseAt("010101", "010101"));
            Assert.Equal(new DateTime(2001, 1, 1, 1, 1, 1, 100), MessageParser.ParseAt("010101", "010101.1"));
            Assert.Equal(new DateTime(2001, 1, 1, 1, 1, 1, 110), MessageParser.ParseAt("010101", "010101.11"));
            Assert.Equal(new DateTime(2001, 1, 1, 1, 1, 1, 111), MessageParser.ParseAt("010101", "010101.111"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseAt("000000", "000000"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseAt("", ""); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseAt("321218", "010101.000"); });
        }

        [Fact]
        public void ParseStatus()
        {
            // Standard cases
            Assert.Equal(BeaconRecord.Statuses.Valid, MessageParser.ParseStatus("A"));
            Assert.Equal(BeaconRecord.Statuses.Invalid, MessageParser.ParseStatus("V"));

            // Edge cases
            Assert.Equal(BeaconRecord.Statuses.Valid, MessageParser.ParseStatus("a"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseStatus(" V"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseStatus("V "); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseStatus("B"); });
        }

        [Fact]
        public void ParseLatitudeIndicator()
        {
            // Standard cases
            Assert.Equal(BeaconRecord.LatitudeIndicators.North, MessageParser.ParseLatitudeIndicator("N"));
            Assert.Equal(BeaconRecord.LatitudeIndicators.South, MessageParser.ParseLatitudeIndicator("S"));

            // Edge cases
            Assert.Equal(BeaconRecord.LatitudeIndicators.North, MessageParser.ParseLatitudeIndicator("n"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLatitudeIndicator(" N"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLatitudeIndicator("N "); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLatitudeIndicator("B"); });
        }

        [Fact]
        public void ParseLongitudeIndicator()
        {
            // Standard cases
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, MessageParser.ParseLongitudeIndicator("E"));
            Assert.Equal(BeaconRecord.LongitudeIndicators.West, MessageParser.ParseLongitudeIndicator("W"));

            // Edge cases
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, MessageParser.ParseLongitudeIndicator("e"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLongitudeIndicator(" E"); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLongitudeIndicator("E "); });
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseLongitudeIndicator("B"); });
        }

        [Fact]
        public void ParseGenericString()
        {
            // Standard cases
            Assert.Equal("Cake", MessageParser.ParseGenericString("Cake", "Field"));

            // Edge cases
            Assert.Equal(" ", MessageParser.ParseGenericString(" ", "Field"));

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseGenericString("", "Field"); });
        }

        [Fact]
        public void ParseGenericInteger()
        {
            // Standard cases
            Assert.Equal(0, MessageParser.ParseGenericInteger("0", "Field"));
            Assert.Equal(5, MessageParser.ParseGenericInteger("5", "Field"));
            Assert.Equal(Int32.MaxValue, MessageParser.ParseGenericInteger(Int32.MaxValue.ToString(), "Field"));

            // Edge cases

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseGenericInteger("", "Field"); });
        }

        [Fact]
        public void ParseGenericDouble()
        {
            // Standard cases
            Assert.Equal(0, MessageParser.ParseGenericDouble("0", "Field"));
            Assert.Equal(5.123, MessageParser.ParseGenericDouble("5.123", "Field"));
            Assert.Equal(999999.999, MessageParser.ParseGenericDouble("999999.999", "Field")); // Interestingly the Double.Parse can't seem to handle Double.MaxValue! Not a problem for our usecase though

            // Edge cases
            Assert.Equal(0, MessageParser.ParseGenericDouble("", "Field")); // Weird case that I've been seeing from the GPS

            // Broken cases
            Assert.Throws<ProtocolViolationException>(() => { MessageParser.ParseGenericInteger("cake", "Field"); });
        }

        [Fact]
        public void EndToEnd_RealGPS()
        {
            var beacon = MessageParser.Parse("#861108034747229#MT600#0000#AUTOLOW#1\r\n#00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52\r\n##\r\n");
            Assert.Equal("861108034747229", beacon.IMEI);
            Assert.Equal("MT600", beacon.GPRSUsername);
            Assert.Equal("0000", beacon.GPRSPassword);
            Assert.Equal(Beacon.Statuses.PowerSaveStationary, beacon.Status);

            var record = beacon.Records.Single();
            Assert.Equal("2018-12-12T09:38:08.0000000Z", record.At.ToString("o"));
            Assert.Equal(BeaconRecord.Statuses.Valid, record.Status);
            Assert.Equal(2741.6724, record.Latitude);
            Assert.Equal(BeaconRecord.LatitudeIndicators.South, record.LatitudeIndicator);
            Assert.Equal(15309.1364, record.Longitude);
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, record.LongitudeIndicator);
            Assert.Equal(0.05, record.GroundSpeed);
            Assert.Equal(0, record.Bearing);
        }

        [Fact]
        public void EndToEnd_ManualSample()
        {
            var beacon = MessageParser.Parse("#863835023427631#MT600#0000#AUTO#1\r\n#a52d15e5803$GPRMC,094632.00,A,2237.7776,N,11402.1399,E,0.07,309.62,030116,,,A*49\r\n##\r\n");
            Assert.Equal("863835023427631", beacon.IMEI);
            Assert.Equal("MT600", beacon.GPRSUsername);
            Assert.Equal("0000", beacon.GPRSPassword);
            Assert.Equal(Beacon.Statuses.None, beacon.Status);

            var record = beacon.Records.Single();
            Assert.Equal("2016-01-03T09:46:32.0000000Z", record.At.ToString("o"));
            Assert.Equal(BeaconRecord.Statuses.Valid, record.Status);
            Assert.Equal(2237.7776, record.Latitude);
            Assert.Equal(BeaconRecord.LatitudeIndicators.North, record.LatitudeIndicator);
            Assert.Equal(11402.1399, record.Longitude);
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, record.LongitudeIndicator);
            Assert.Equal(0.07, record.GroundSpeed);
            Assert.Equal(309.62, record.Bearing);
        }

        [Fact]
        public void EndToEnd_ContrivedExtream()
        {
            var beacon = MessageParser.Parse("#963835023427632#MT600#0000#AUTO#1\r\n#a52d15e5803$GPRMC,134632.00,V,0,N,0,W,,,301220,,,\r\n##\r\n");
            Assert.Equal("963835023427632", beacon.IMEI);
            Assert.Equal("MT600", beacon.GPRSUsername);
            Assert.Equal("0000", beacon.GPRSPassword);
            Assert.Equal(Beacon.Statuses.None, beacon.Status);

            var record = beacon.Records.Single();
            Assert.Equal("2020-12-30T13:46:32.0000000Z", record.At.ToString("o"));
            Assert.Equal(BeaconRecord.Statuses.Invalid, record.Status);
            Assert.Equal(0, record.Latitude);
            Assert.Equal(BeaconRecord.LatitudeIndicators.North, record.LatitudeIndicator);
            Assert.Equal(0, record.Longitude);
            Assert.Equal(BeaconRecord.LongitudeIndicators.West, record.LongitudeIndicator);
            Assert.Equal(0, record.GroundSpeed);
            Assert.Equal(0, record.Bearing);
        }
    }
}
