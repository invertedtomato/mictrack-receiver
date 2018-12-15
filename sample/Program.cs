﻿using System;

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
                Console.WriteLine("Beacon!");
            };
            receiver.OnError += (sender, e) =>
            {
                Console.WriteLine("Error!");
            };
            Console.WriteLine("done.");

            Console.Write("Starting... ");
            receiver.Start();
            Console.WriteLine("done.");

            Console.WriteLine();
            Console.WriteLine("Running. Press 'Q' to terminate.");
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
        }
    }
}