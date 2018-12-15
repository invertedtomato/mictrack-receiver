using System;
using InvertedTomato.IO.Mictrack;
using InvertedTomato.IO.Mictrack.Models;
using Xunit;
using System.Linq;

namespace InvertedTomato.IO.Mictrace
{
    public class MessageParserTests
    {
        private readonly String[] Messages = new String[]{
            "#861108034747229#MT600#0000#AUTOLOW#1\r\n#00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52\r\n##\r\n", // Actual GPS
            "#863835023427631#MT600#0000#AUTO#1\r\n#a52d15e5803$GPRMC,094632.00,A,2237.7776,N,11402.1399,E,0.07,309.62,030116,,,A*49\r\n##\r\n" // Example from manual
        };

        [Fact]
        public void EndToEnd_1()
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal("861108034747229", beacon.IMEI);
            Assert.Equal("MT600", beacon.GPRSUsername);
            Assert.Equal("0000", beacon.GPRSPassword);
            Assert.Equal(Beacon.Statuses.PowerSaveStopped, beacon.Status);

            var record = beacon.Records.Single();
            Assert.Equal("12/12/2018 09:38:08", record.At.ToString());
            Assert.Equal(BeaconRecord.Statuses.Valid, record.Status);
            Assert.Equal(2741.6724, record.Latitude);
            Assert.Equal(BeaconRecord.LatitudeIndicators.South, record.LatitudeIndicator);
            Assert.Equal(15309.1364, record.Longitude);
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, record.LongitudeIndicator);
            Assert.Equal(0.05, record.GroundSpeed);
            Assert.Null(record.Bearing);
        }

        [Fact]
        public void EndToEnd_2()
        {
            var beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal("863835023427631", beacon.IMEI);
            Assert.Equal("MT600", beacon.GPRSUsername);
            Assert.Equal("0000", beacon.GPRSPassword);
            Assert.Equal(Beacon.Statuses.None, beacon.Status);

            var record = beacon.Records.Single();
            Assert.Equal("01/03/2016 09:46:32", record.At.ToString());
            Assert.Equal(BeaconRecord.Statuses.Valid, record.Status);
            Assert.Equal(2237.7776, record.Latitude);
            Assert.Equal(BeaconRecord.LatitudeIndicators.North, record.LatitudeIndicator);
            Assert.Equal(11402.1399, record.Longitude);
            Assert.Equal(BeaconRecord.LongitudeIndicators.East, record.LongitudeIndicator);
            Assert.Equal(0.07, record.GroundSpeed);
            Assert.Equal(309.62, record.Bearing);
        }
    }
}
