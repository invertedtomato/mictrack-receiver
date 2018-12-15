using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.IO.Mictrack.Models;

namespace InvertedTomato.IO.Mictrack
{
    public class MictrackReceiver : IDisposable
    {
        // Size of receive buffer - this must be at least as long as the longest possible message
        private const Int32 RXBUFFER_LENGTH = 256; // Documtation example is 121 bytes

        public delegate void OnBeaconEventHandler(Object sender, OnBeaconEventArgs e);

        public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);

        public event OnBeaconEventHandler OnBeacon;

        public event OnErrorEventHandler OnError;

        public Boolean IsDisposed { get; private set; }

        public Boolean IsRunning { get; private set; }

        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 5000); // TODO: Is there an "official" default port?

        public Int32 ConnectionBacklogLimit { get; set; } = 10;

        private readonly Socket Listener;
        private readonly Object Sync = new Object();

        public MictrackReceiver()
        {
            // Create listener socket
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            // Check that we are NOT already running in a thread-safe manner
            lock (Sync)
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("Receiver already running.");
                }
                IsRunning = true;
            }

            // Bind socket and listen for connections
            Listener.Bind(LocalEndPoint);
            Listener.Listen(ConnectionBacklogLimit);

            // Seed accepting
            AcceptStart();
        }

        private void AcceptStart()
        {
            try
            {
                Listener.BeginAccept(new AsyncCallback(AcceptEnd), null);
            }
            catch (ObjectDisposedException) { } // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
        }

        private void AcceptEnd(IAsyncResult result)
        {
            Socket connection;
            try
            {
                // Complete accept
                connection = Listener.EndAccept(result);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort accept
            }

            // Create state
            var state = new ConnectionState()
            {
                Buffer = new Byte[RXBUFFER_LENGTH],
                Position = 0, // Obviously not required, but here for completeness
                Connection = connection,
                RemoteAddressString = ((IPEndPoint)connection.RemoteEndPoint).Address.ToString() // This is not available on the socket once it's error'd, so capture it now
            };

            // Start read cycle
            ReceiveStart(state);

            // Start next accept cycle
            AcceptStart();
        }

        private void ReceiveStart(ConnectionState state)
        {
            // Begin receive
            // TODO: Handle case where we receive more data than buffer capacity
            try
            {
                state.Connection.BeginReceive(state.Buffer, state.Position, state.Buffer.Length - state.Position, SocketFlags.None, new AsyncCallback(ReceiveEnd), state);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }
            catch (SocketException ex) // Occurs when there is a connection error
            {
                // Report error
                OnError?.Invoke(this, new OnErrorEventArgs()
                {
                    Message = ex.Message,
                    RemoteAddressString = state.RemoteAddressString
                });
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }
        }

        private void ReceiveEnd(IAsyncResult ar)
        {
            // Retrieve state
            var state = (ConnectionState)ar.AsyncState;

            // Complete read
            try
            {
                state.Position += state.Connection.EndReceive(ar);
            }
            catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
            {
                return; // Abort reawd
            }
            catch (SocketException ex) // Occurs when there is a connection error
            {
                // Report error
                OnError?.Invoke(this, new OnErrorEventArgs()
                {
                    Message = ex.Message,
                    RemoteAddressString = state.RemoteAddressString
                });
                return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
            }

            // If the message is complete...
            if (state.Position > 2 && state.Buffer[state.Position] == '#' && state.Buffer[state.Position - 1] == '#')
            {
                // Decode message
                var message = Encoding.ASCII.GetString(state.Buffer, 0, state.Position);

                // Process message
                try
                {
                    ProcessMessage(state.RemoteAddressString, message);
                }
                catch (ProtocolViolationException ex)
                {
                    // Report error
                    OnError?.Invoke(this, new OnErrorEventArgs()
                    {
                        Message = ex.Message,
                        RemoteAddressString = state.RemoteAddressString
                    });
                }
            }
            else
            {
                // Receive more
                ReceiveStart(state);
            }
        }
        private void ProcessMessage(String remoteAddressString, String message) // TODO: RemoteAddressString smells funny - pondering!
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
            MictrackBeacon.Events evt;
            switch (tokens1[4])
            {
                case "AUTO":
                    evt = MictrackBeacon.Events.None;
                    break;
                case "AUTOLOW":
                    evt = MictrackBeacon.Events.PowerSaveStopped;
                    break;
                case "TOWED":
                    evt = MictrackBeacon.Events.PowerSaveMoving;
                    break;
                case "CALL":
                    evt = MictrackBeacon.Events.Call;
                    break;
                case "DEF":
                    evt = MictrackBeacon.Events.Disconnect;
                    break;
                case "HT":
                    evt = MictrackBeacon.Events.HighTemperature;
                    break;
                case "BLP":
                    evt = MictrackBeacon.Events.InternalBatteryLow;
                    break;
                case "CLP":
                    evt = MictrackBeacon.Events.ExternalBatteryLow;
                    break;
                case "OS":
                    evt = MictrackBeacon.Events.GeoFenceExit;
                    break;
                case "RS":
                    evt = MictrackBeacon.Events.GeoFenceEnter;
                    break;
                case "OVERSPEED":
                    evt = MictrackBeacon.Events.SpeedLimitOver;
                    break;
                case "SAFESPEED":
                    evt = MictrackBeacon.Events.SpeedLimitUnder;
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
            MictrackBeacon.Statuses status;
            switch (tokens2[12])
            {
                case "A":
                    status = MictrackBeacon.Statuses.Valid;
                    break;
                case "V":
                    status = MictrackBeacon.Statuses.Invalid;
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
            MictrackBeacon.LatitudeIndicators latitudeIndicator;
            switch (tokens2[14])
            {
                case "N":
                    latitudeIndicator = MictrackBeacon.LatitudeIndicators.North;
                    break;
                case "S":
                    latitudeIndicator = MictrackBeacon.LatitudeIndicators.South;
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
            MictrackBeacon.LongitudeIndicators longitudeIndicator;
            switch (tokens2[16])
            {
                case "E":
                    longitudeIndicator = MictrackBeacon.LongitudeIndicators.East;
                    break;
                case "W":
                    longitudeIndicator = MictrackBeacon.LongitudeIndicators.West;
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
            var beacon = new MictrackBeacon()
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

            // Raise event
            OnBeacon?.Invoke(this, new OnBeaconEventArgs()
            {
                Beacon = beacon,
                RemoteAddressString = remoteAddressString
            });
        }
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                // Dispose managed state (managed objects)
                Listener?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}