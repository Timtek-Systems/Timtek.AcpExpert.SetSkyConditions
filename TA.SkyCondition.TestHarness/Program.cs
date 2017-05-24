// This file is part of the Tigra.RemoteSkyConditions.Server project
// 
// File: Program.cs  Created: 2017-05-24@15:06
// Last modified: 2017-05-24@15:19

using System;
using NLog;
using Tigra.RemoteSkyConditions.Server;

namespace TA.SkyCondition.TestHarness
    {
    internal class Program
        {
        private static void Main(string[] args)
            {
            var log = LogManager.GetCurrentClassLogger();
            var server = new SkyConditionServer();
            log.Warn("Server running - press ENTER to exit");
            Console.ReadLine();
            }
        }
    }