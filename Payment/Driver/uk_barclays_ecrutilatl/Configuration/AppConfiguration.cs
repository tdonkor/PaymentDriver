﻿using Acrelec.Library.Logger;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Acrelec.Mockingbird.Payment.Configuration
{
    public class AppConfiguration
    {
        private class ConfigurationEntry
        {
            public string Key { get; set; }

            public string Section { get; set; }

            public string Value { get; set; }
        }

        static AppConfiguration()
        {
            Instance = new AppConfiguration();
        }

        private IList<ConfigurationEntry> _entries;

        private AppConfiguration(string iniPath = null)
        {
            var executingAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var path = new FileInfo(iniPath ?? executingAssemblyName + ".ini").FullName;
            ParseFile(path);
        }

        private void ParseFile(string path)
        {
            _entries = new List<ConfigurationEntry>();

            if (!File.Exists(path))
            {
                Log.Info($"Configuration file {path} not found. Using default values.");
                return;
            }

            var section = string.Empty;
            foreach (var line in File.ReadAllLines(path))
            {
                var sectionMatch = Regex.Match(line, @"^\[(\w+)\]\s*$");
                if (sectionMatch.Success)
                {
                    section = sectionMatch.Groups[1].Value;
                    continue;
                }

                var entryMatch = Regex.Match(line, @"^([^;]+?)=([\s\S]+?)$");
                if (entryMatch.Success)
                {
                    _entries.Add(new ConfigurationEntry()
                    {
                        Section = section,
                        Key = entryMatch.Groups[1].Value,
                        Value = entryMatch.Groups[2].Value
                    });
                }
            }
        }

        public string OutPath
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key =="OUT_PATH")?.Value ?? @"out\";
            }
        }

        public int SettlementTriggerPort
        {
            get
            {
                var entry = _entries.FirstOrDefault(_ => _.Key == "SETTLEMENT_LISTENER_PORT")?.Value;
                return int.TryParse(entry, out var result) ? result : 9845;
            }
        }

        public int HeartbeatInterval
        {
            get
            {
                var entry = _entries.FirstOrDefault(_ => _.Key == "HEARTBEAT_INTERVAL")?.Value;
                return int.TryParse(entry, out var result) ? result : 300;
            }
        }

        public static AppConfiguration Instance { get; }
    }
}
