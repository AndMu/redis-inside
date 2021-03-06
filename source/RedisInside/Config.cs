﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Wikiled.Common.Extensions;

namespace RedisInside
{
    internal class Config : IConfig
    {
        private static readonly Random random = new Random();

        private static readonly ConcurrentDictionary<int, byte> usedPorts = new ConcurrentDictionary<int, byte>();

        private string location;

        public Config()
        {
            do
            {
                SelectedPort = random.Next(8000, 55535 + 1);
                var isOpen = NetworkExtensions.ScanPort(SelectedPort).Result;
                if (isOpen)
                {
                    usedPorts.AddOrUpdate(SelectedPort, i => byte.MinValue, (i, b) => byte.MinValue);
                }

                Logger?.Invoke($"Trying port {SelectedPort}({isOpen})");
            }
            while (usedPorts.ContainsKey(SelectedPort));
            usedPorts.AddOrUpdate(SelectedPort, i => byte.MinValue, (i, b) => byte.MinValue);
            Logger = message => Debug.WriteLine(message);
        }

        public Action<string> Logger { get; private set; }

        public int SelectedPort { get; private set; }

        public bool CheckStatus { get; private set; } = true;

        public string Location
        {
            get => location ?? Path.GetTempPath();
            private set => location = value;
        }

        public bool IsWithPersistence { get; private set; }

        public bool Kill { get; private set; }

        public string PersistenceFile { get; private set; }

        public string Persistence => !IsWithPersistence ? "persistence-available no" : $"dbfilename {PersistenceFile}";

        public IConfig WithPersistence(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Guid.NewGuid().ToString("N") + ".db";
            }

            PersistenceFile = fileName;
            IsWithPersistence = true;
            return this;
        }

        public IConfig KillAll()
        {
            Kill = true;
            return this;
        }

        public IConfig DisableCheck()
        {
            CheckStatus = false;
            return this;
        }

        public IConfig WithLocation(string directory = null, bool randomName = false)
        {
            if (!string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(directory, "RedisInside", randomName ? DateTime.UtcNow.Ticks.ToString() : "Server");
                directory.EnsureDirectoryExistence();
            }

            Location = directory;
            return this;
        }

        public IConfig LogTo(Action<string> logFunction)
        {
            Logger = logFunction;
            return this;
        }

        public IConfig Port(int portNumber)
        {
            SelectedPort = portNumber;
            return this;
        }
    }
}
