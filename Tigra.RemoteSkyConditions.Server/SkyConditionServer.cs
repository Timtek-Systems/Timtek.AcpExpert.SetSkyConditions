// This file is part of the Tigra.RemoteSkyConditions.Server project
// 
// Copyright © 2016-2017 Tigra Astronomy., all rights reserved.
// 
// File: SkyConditionServer.cs  Last modified: 2017-10-22@19:19 by Tim Long

using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;
using NLog.Fluent;

namespace Tigra.RemoteSkyConditions.Server
    {
    [ProgId("TA.SkyCondition.Server")]
    [Guid("18f11d1e-9f6e-47f7-bf36-8731dd3cb290")]
    //[ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class SkyConditionServer : IDisposable
        {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        private bool available;
        private Thread server;
        private int skyCondition = 1;
        private bool terminatePipeServer;

        public SkyConditionServer()
            {
            RegisterUnhandledExceptionHandler();
            CreateNamedPipeServer();
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
                var result = skyCondition; // Prevent race condition with ReadPipeDataAsync()
                Log.Debug().Message($"Get SkyCondition: {result}");
                return result;
                }
            private set
                {
                Log.Info().Message($"Set SkyCondition: {value}").Write();
                skyCondition = value;
                }
            }

        private static NamedPipeServerStream CreateServerPipe()
            {
            const string pipeName = "tigraSkyQuality";
            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte);
            Log.Info($@"Created server pipe on \\.\pipe\{pipeName}");
            return pipe;
            }

        private void CreateNamedPipeServer()
            {
            Log.Info().Message("Starting pipe server thread").Write();
            server = new Thread(PipeServerThread);
            server.CurrentCulture = CultureInfo.InvariantCulture;
            server.Name = "Tigra Sky Quality Pipe Server";
            server.IsBackground = true; // background threads do not keep the process alive
            server.Start();
            }

        private void PipeServerThread()
            {
            Log.Debug().Message("Pipe server thread running").Write();
            Available = true;
            while (!terminatePipeServer)
                {
                using (var pipeStream = CreateServerPipe())
                    {
                    WaitForClientConnection(pipeStream);
                    ReadPipeMessages(pipeStream);
                    }
                Log.Info("Destroyed server pipe");
                }
            Available = false;
            Log.Warn("Pipe server thread exiting");
            }

        private void ProcessReceivedData(string receivedData)
            {
            var ordinal = 0;
            try
                {
                ordinal = int.Parse(receivedData);
                if (ordinal < 0 || ordinal > 3)
                    {
                    Log.Error($"Sky condition ordinal value {ordinal} is outside of the allowed range [0..3]");
                    return;
                    }
                SkyCondition = ordinal;
                Available = true;
                }
            catch (FormatException ex)
                {
                Log.Error()
                    .Message($"Unable to parse Sky Condition '{receivedData}' into an integer")
                    .Write();
                }
            catch (ArgumentOutOfRangeException ex)
                {
                Log.Error()
                    .Message($"Sky condition ordinal value {ordinal} is outside the allowed range of [0..3]")
                    .Write();
                }
            }

        private void ReadPipeMessages(NamedPipeServerStream pipe)
            {
            using (var reader = new StreamReader(pipe))
                {
                while (pipe.IsConnected && !reader.EndOfStream)
                    try
                        {
                        var receivedData = reader.ReadLine();
                        Log.Info().Message($"Received: {receivedData}");
                        if (string.IsNullOrEmpty(receivedData))
                            continue;
                        ProcessReceivedData(receivedData);
                        }
                    catch (Exception ex)
                        {
                        Log.Error()
                            .Message($"Error reading pipe stream: {ex.Message}")
                            .Write();
                        }
                }
            Log.Warn().Message("Client disconnected").Write();
            }

        private void WaitForClientConnection(NamedPipeServerStream pipe)
            {
            Log.Info().Message("Waiting for client connection").Write();
            pipe.WaitForConnection();
            Log.Warn().Message("Client connected").Write();
            }

        private void RegisterUnhandledExceptionHandler()
            {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                var exception = args.ExceptionObject as Exception;
                var exceptionMessage = exception?.Message ?? "Unknown exception";
                Log.Error()
                    .Message($"Unhandled exception: {exceptionMessage}")
                    .Property("exception", exception)
                    .Write();
                };
            }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
            {
            if (!disposedValue)
                {
                if (disposing)
                    terminatePipeServer = true;

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
                }
            }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SkyConditionServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
            {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
            }
        #endregion
        }
    }