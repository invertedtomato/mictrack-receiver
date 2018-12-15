# InvertedTomato.MictrackReceiver
## Overview
MictrackReceiver is a lightweight .NET Standard library to receive beacons from Mictrack GPS trackers, like their MT600. The library receives locations beacons, parses them, and then exposes them to your code via a callback event.

## Sample
Here's a base-basics example of it's use. A more involved sample can be found in the included "sample" project.
```c#
// Instantiate receiver
var receiver = new MictrackReceiver();

// Setup handler for when a beacon arrives - in this case print it to the console
receiver.OnBeacon += (sender, e) =>
{
    Console.WriteLine($"IMEI {e.Beacon.IMEI} ({e.Beacon.Status})");
    foreach(var record in e.Beacon.Records){
        Console.WriteLine($"  BaseID {record.BaseIdentifier} {record.At}"); 
        Console.WriteLine($"    Location: {record.Latitude} {record.LatitudeIndicator}, {record.Longitude} {record.LongitudeIndicator} ({record.Status})"); 
        Console.WriteLine($"    Heading:  {(record.Bearing.HasValue ? record.Bearing.Value.ToString() : "-")} @ {record.GroundSpeed} knots");
    }
};

// Start listening
receiver.Start();

// Keep your app running :)
Console.ReadKey(true);
```

## TCP Ports
With an apparent absence of a default port, this uses port 5000 as default. You can change this, and the locally bound IP by modifying the LocalEndPoint field before starting.

## Protocol
MictrackReceiver is based on the [official protocol documentation](https://www.mictrack.com/downloads/protocols/Mictrack_Communication_Protocol_For_MP90&MP90-NB.pdf). For protocol documentation it's pretty rough and assumptions have been made in some places. To verify those assumptions the library has been tested with MT600s.