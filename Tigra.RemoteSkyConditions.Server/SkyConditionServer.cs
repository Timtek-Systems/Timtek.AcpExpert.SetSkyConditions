// This file is part of the Tigra.RemoteSkyConditions.Server project
// 
// File: SkyConditionServer.cs  Created: 2017-05-24@03:59
// Last modified: 2017-05-24@06:59

using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;

namespace Tigra.RemoteSkyConditions.Server
    {
    [ProgId("TA.SkyCondition.Server")]
    [Guid("18f11d1e-9f6e-47f7-bf36-8731dd3cb290")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class SkyConditionServer
        {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        private bool available;
        private NamedPipeServerStream pipe;
        private int readsPending;
        private int skyCondition = 1;

        public SkyConditionServer()
            {
            CreateNamedPipeServerAsync();
            }

        public bool Available
            {
            get
                {
                Log.Debug().Message($"Get Available: {available}").Write();
                return available;
                }
            private set
                {
                Log.Debug().Message($"Set Available: {value}");
                available = value;
                }
            }

        public int SkyCondition
            {
            get
                {
                Log.Debug().Message("Get SkyCondition");
                Available = false;
                var result = skyCondition; // Prevent race condition with ReadPipeDataAsync()
                Log.Debug().Message($"Get SkyCondition: {result}");
#pragma warning disable 4014         // Async method not awaited.
                ReadPipeDataAsync(); // Prime the pipe reader to read the next data
#pragma warning restore 4014
                return result;
                }
            private set
                {
                Log.Info().Message($"Set SkyCondition: {value}").Write();
                skyCondition = value;
                Available = true;
                }
            }

        private Task CreateNamedPipeServerAsync()
            {
            pipe = new NamedPipeServerStream("tigraSkyQuality", PipeDirection.In);
            return ReadPipeDataAsync();
            }

        private async Task ReadPipeDataAsync()
            {
            // Only allow 1 read operation to be pending at any time. Checks must be thread-safe.
            if (Interlocked.CompareExchange(ref readsPending, 1, 0) > 0)
                return;

            var reader = new StreamReader(pipe);
            while (true)
                {
                try
                    {
                    var newSkyConition = await reader.ReadLineAsync().ConfigureAwait(false);
                    Log.Info()
                        .Message($"Received: {newSkyConition}")
                        .Property("received", newSkyConition)
                        .Write();
                    var ordinal = int.Parse(newSkyConition);
                    if (ordinal < 0 || ordinal > 3)
                        throw new ArgumentOutOfRangeException(
                            $"Sky condition ordinal value {ordinal} is outside of the allowed range [0..3]");
                    SkyCondition = ordinal;
                    Available = true;
                    }
                catch (FormatException ex)
                    {
                    Log.Error()
                        .Message($"Unable to parse received value as an integer: {ex.Message}")
                        .Property("exception", ex)
                        .Write();
                    }
                catch (ArgumentOutOfRangeException ex)
                    {
                    Log.Error()
                        .Message(ex.Message)
                        .Property("exception", ex)
                        .Write();
                    }
                finally
                    {
                    // Unblock future read operations
                    Interlocked.Exchange(ref readsPending, 0);
                    }
                }
            }
        }
    }