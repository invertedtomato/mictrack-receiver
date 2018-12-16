using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Preparing... ");
            var receiver = new MictrackReceiver();
            receiver.OnBeacon += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"IMEI {e.Beacon.IMEI} ({e.Beacon.Status})");
                foreach (var record in e.Beacon.Records)
                {
                    Console.WriteLine($"  BaseID {record.BaseIdentifier} {record.At}");

                    // Current location
                    if (record.Status == BeaconRecord.Statuses.Valid)
                    {
                        Console.WriteLine($"    Location: {record.Latitude} {record.LatitudeIndicator}, {record.Longitude} {record.LongitudeIndicator}");
                    }
                    else
                    {
                        Console.WriteLine($"    Location: unknown");
                    }

                    // Heading
                    if (record.Bearing.HasValue)
                    {
                        Console.WriteLine($"    Heading:  {record.Bearing} deg @ {record.GroundSpeed} knots");
                    }
                    else
                    {
                        Console.WriteLine($"    Heading:  not currently moving");
                    }
                }
            };
            receiver.OnError += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.RemoteAddressString + ": " + e.Message);
            };
            Console.WriteLine("done.");

            Console.Write("Starting... ");
            receiver.Start();
            Console.WriteLine("done.");

            Console.WriteLine();
            Console.WriteLine("Running. Press 'Q' to terminate.");
            if (Debugger.IsAttached) // TODO: Fix properly
            {
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
        }
    }
}
