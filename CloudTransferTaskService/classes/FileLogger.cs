

using CloudTransferTaskService;
using System;
using System.IO;

namespace CloudTransferTask.src.classes {
    class FileLogger {

        private static readonly string logExtension = ".log";

        /// <summary>
        /// Log information
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Debug(string text) {
            if (Program.logLevel.ToLower() == "debug") {
               WriteToFile("DEBUG:\t" + text);
            }
        }

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Info(string text) {
            if (Program.logLevel.ToLower() == "info" || Program.logLevel.ToLower() == "debug") {
                WriteToFile("INFO:\t" + text);
            }
        }


        /// <summary>
        /// Log warnings
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Warning(string text) {
            if (Program.logLevel.ToLower() == "info" || Program.logLevel.ToLower() == "debug") {
                WriteToFile("WARNING:\t" + text);
            }
        }


        /// <summary>
        /// Log notice
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Notice(string text) {
            if (Program.logLevel.ToLower() == "info") {
                WriteToFile("NOTICE:\t" + text);
            }
        }


        /// <summary>
        /// Log errors
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Error(string text) {
            if (Program.logLevel.ToLower() == "info" || Program.logLevel.ToLower() == "debug") {
                WriteToFile("ERROR:\t" + text);
            }
        }


        /// <summary>
        /// Write highlighted to console
        /// </summary>
        /// <param name="logString">Non-highlighted text</param>
        public static void WriteToFile(string logString) {
            try {
                if (!string.IsNullOrEmpty(Program.logLocation)) {
                    if (Json.loggingEnabled) {
                        // System.IO.IOException: 'The process cannot access the file .../CloudTransferTaskService.log' because it is being used by another process.'
                        try {
                            if (!Program.logLocation.EndsWith(Path.DirectorySeparatorChar)) {
                                Program.logLocation += Path.DirectorySeparatorChar;
                            }

                            try {
                                if (!Directory.Exists(Program.logLocation)) {
                                    Directory.CreateDirectory(Program.logLocation);
                                }
                            } catch {

                            }

                            File.AppendAllText(Program.logLocation + DateTime.Now.ToString("yyyy-MM-dd") + logExtension, "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + logString + "\n");
                        } catch { }
                    }
                }
            } catch { }
        }


        /// <summary>
        /// Check if logging is enabled and set status 
        /// </summary>
        public static void SetLoggingStatus() {
            var serviceConf = Json.GetServiceConfiguration(Json.serviceConfFullPath);
            Json.loggingEnabled = !string.IsNullOrEmpty(serviceConf.LogLocation) && serviceConf.LogLocation != new CloudTransferTaskService.classes.helper.ServiceConfig().LogLocation && 
                serviceConf.LogLocation.ToLower() != "disable";
            Program.logLocation = serviceConf.LogLocation;
            Program.logLevel = serviceConf.LogLevel;
        }
    }
}
