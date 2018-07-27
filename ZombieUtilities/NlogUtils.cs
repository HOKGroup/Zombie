using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace ZombieUtilities
{
    public static class NlogUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        public static void CreateConfiguration(Uri endpoint = null)
        {
            var config = new LoggingConfiguration();

            // (Konrad) File Log Setup
            var fileTarget = new FileTarget
            {
                Name = "default",
                FileName = @"${basedir}/logs/Debug.log",
                Layout = @"${longdate}|${level:uppercase=true}|${logger}|${message}",
                KeepFileOpen = false,
                ArchiveFileName = @"${basedir}/logs/Debug_${shortdate}.{##}.log",
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 30
            };
            config.AddTarget("logfile", fileTarget);

            var rule1 = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule1);

            if (endpoint != null)
            {
                // (Konrad) Web Service Log Setup
                var wsTarget = new WebServiceTarget
                {
                    Encoding = Encoding.UTF8,
                    Protocol = WebServiceProtocol.HttpPost,
                    Name = "ws",
                    Url = endpoint
                };
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${message}",
                    Name = "message",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${longdate}",
                    Name = "createdAt",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${assembly-version}",
                    Name = "version",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${level}",
                    Name = "level",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${machinename}",
                    Name = "machine",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${exception}",
                    Name = "exception",
                    Type = typeof(string)
                });
                wsTarget.Parameters.Add(new MethodCallParameter
                {
                    Layout = "${callsite} at line: ${callsite-linenumber}",
                    Name = "source",
                    Type = typeof(string)
                });

                config.AddTarget("ws", wsTarget);
                var rule = new LoggingRule("*", LogLevel.Trace, wsTarget);
                config.LoggingRules.Add(rule);
            }

            LogManager.Configuration = config;
        }
    }
}
