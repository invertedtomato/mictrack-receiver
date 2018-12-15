using System;
using InvertedTomato.IO.Mictrack;
using InvertedTomato.IO.Mictrack.Models;
using Xunit;

namespace InvertedTomato.IO.Mictrace
{
    public class MessageParserTests
    {
        private readonly String[] Messages = new String[]{
            "#861108034747229#MT600#0000#AUTOLOW#1\r\n#00018b5fc03$GPRMC,093808.00,A,2741.6724,S,15309.1364,E,0.05,,121218,,,A*52\r\n##\r\n", // Actual GPS
            "#863835023427631#MT600#0000#AUTO#1\r\n#a52d15e5803$GPRMC,094632.00,A,2237.7776,N,11402.1399,E,0.07,309.62,030116,,,A*49\r\n##\r\n" // Example from manual
        };

        [Fact]
        public void ParseHeaderImei()
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal("861108034747229", beacon.IMEI);

            beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal("863835023427631", beacon.IMEI);
        }

        [Fact]
        public void ParseHeaderGrpsUsername()
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal("MT600", beacon.GPRSUsername);

            beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal("MT600", beacon.GPRSUsername);
        }

        [Fact]
        public void ParseHeaderGprsPassword()
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal("0000", beacon.GPRSPassword);

            beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal("0000", beacon.GPRSPassword);
        }

        [Fact]
        public void ParseHeaderStatus() // TODO: Check other status'
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal(Beacon.Statuses.PowerSaveStopped, beacon.Status);

            beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal(Beacon.Statuses.None, beacon.Status);
        }
        
        /*
        [Fact]
        public void ParseBaseIdentifier()
        {
            var beacon = MessageParser.Parse(Messages[0]);
            Assert.Equal("0000", beacon.BaseIdentifier);

            beacon = MessageParser.Parse(Messages[1]);
            Assert.Equal("0000", beacon.BaseIdentifier);
        }*/
    }
}
