﻿using Newtonsoft.Json;
using Shadowsocks.Controller;
using Shadowsocks.Util;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Shadowsocks.Model
{
    public static class Global
    {
        private const string ConfigFile = @"gui-config.json";
        private const string ConfigFileBackup = @"gui-config.json.backup";

        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        public static bool OSSupportsLocalIPv6 = false;

        public static string LocalHost => OSSupportsLocalIPv6 ? $@"[{IPAddress.IPv6Loopback}]" : $@"{IPAddress.Loopback}";

        public static string AnyHost => OSSupportsLocalIPv6 ? $@"[{IPAddress.IPv6Any}]" : $@"{IPAddress.Any}";

        public static Configuration GuiConfig;

        public static MainController Controller;

        public static MenuViewController ViewController;

        public static Configuration LoadFile(string filename)
        {
            Configuration config;
            try
            {
                if (File.Exists(filename))
                {
                    var configContent = File.ReadAllText(filename);
                    config = Load(configContent);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            config = new Configuration();
            config.FixConfiguration();
            return config;
        }

        public static Configuration Load()
        {
            return LoadFile(ConfigFile);
        }

        private static Configuration Load(string configStr)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<Configuration>(configStr);
                config.FixConfiguration();
                return config;
            }
            catch
            {
                return null;
            }
        }

        public static void LoadConfig()
        {
            GuiConfig = Load();
        }

        public static void SaveConfig()
        {
            if (GuiConfig.Index >= GuiConfig.Configs.Count)
            {
                GuiConfig.Index = GuiConfig.Configs.Count - 1;
            }
            else if (GuiConfig.Index < 0)
            {
                GuiConfig.Index = 0;
            }

            try
            {
                var jsonString = JsonConvert.SerializeObject(GuiConfig, Formatting.Indented);
                Lock.EnterWriteLock();
                try
                {
                    Utils.WriteAllTextAsync(ConfigFile, jsonString);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                if (File.Exists(ConfigFileBackup))
                {
                    var dt = File.GetLastWriteTimeUtc(ConfigFileBackup);
                    var now = DateTime.Now;
                    if ((now - dt).TotalHours > 4)
                    {
                        File.Copy(ConfigFile, ConfigFileBackup, true);
                    }
                }
                else
                {
                    File.Copy(ConfigFile, ConfigFileBackup, true);
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}