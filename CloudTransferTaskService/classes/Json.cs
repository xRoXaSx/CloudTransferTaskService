using CloudTransferTask.src.classes.helper;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using CloudTransferTaskService;
using CloudTransferTaskService.classes.helper;
using CloudTransferTask.src.classes.eventhandler;
using System.Security.Principal;

namespace CloudTransferTask.src.classes {

    class Json {

        // === CloudTransferTask config.json locations
        public static string confPath = "";
        public static string confFullPath = "";
        public static string confFileName = "config.json";
        public static string folderNameCap = "CloudTransferTasks";
        public static string folderNameNoCap = "cloudtransfertasks";
        public static string confPathLnx = Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar;
        private static string confPathOsx = "~" + Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar + "Application Support";
        public static string confPathWin = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar;

        // === CloudTransferTaskService config.json locations
        public static string serviceConfPath = "";
        public static string serviceConfFullPath = "";
        public static string serviceInstallFullPathLnx = "";
        public static string serviceConfFileName = "config.json";
        public static string serviceFolderNameCap = "CloudTransferTasks";
        public static string serviceFolderNameNoCap = "cloudtransfertasks";
        public static string serviceInstallFileNameLnx = serviceFolderNameNoCap + ".service";
        public static string serviceConfPathLnx = Path.DirectorySeparatorChar + "etc" + Path.DirectorySeparatorChar + serviceFolderNameNoCap;
        public static string serviceInstallPathLnx = Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".local" +
            Path.DirectorySeparatorChar + "share" + Path.DirectorySeparatorChar + "systemd" + Path.DirectorySeparatorChar + "user";
        private static string serviceConfPathOsx = "~" + Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar + "Application Support";
        public static string serviceConfPathWin = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        public static bool loggingEnabled = false;

        /// <summary>
        /// Detect which OS this machine is running
        /// </summary>
        /// <returns></returns>
        public static string DetectOS() {
            string os = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                os = "win";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                os = "lin";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                os = "osx";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
                os = "fbd";
            }

            return os;
        }


        /// <summary>
        /// Set the config path field to read from it later.
        /// </summary>
        public static void SetConfPath() {
            switch (Program.os) {
                case "win":
                    using (var identity = WindowsIdentity.GetCurrent()) {
                        if (identity.IsSystem) {

                            confPathWin = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar +
                                    "User" + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;
                        }
                    }

                    //if (Environment.UserName == Environment.MachineName + "$") {
                    //        confPathWin = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar +
                    //            "User" + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;
                    //    }

                    confPathWin += folderNameCap;
                    confPath = confPathWin;

                    serviceConfPathWin += Path.DirectorySeparatorChar + serviceFolderNameCap;
                    serviceConfPath = serviceConfPathWin;
                    break;
                case "lin":
                case "fbd":
                    confPathLnx += folderNameNoCap;
                    confPath = confPathLnx;

                    //serviceConfPathLnx += Path.DirectorySeparatorChar + serviceFolderNameNoCap;
                    serviceConfPath = serviceConfPathLnx;
                    serviceInstallFullPathLnx = serviceInstallPathLnx + Path.DirectorySeparatorChar + serviceInstallFileNameLnx;
                    break;
                case "osx":
                    confPathOsx += folderNameCap;
                    confPath = confPathOsx;

                    serviceConfPathOsx += Path.DirectorySeparatorChar + serviceFolderNameNoCap;
                    serviceConfPath = serviceConfPathOsx;
                break;
            }

            confFullPath = confPath + Path.DirectorySeparatorChar + confFileName;
            serviceConfFullPath = serviceConfPath + Path.DirectorySeparatorChar + serviceConfFileName;
        }


        /// <summary>
        /// Get the service configuration
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <returns></returns>
        public static ServiceConfig GetServiceConfiguration(string configFilePath) {
            var serviceConfig = new ServiceConfig();
            if (File.Exists(configFilePath)) {
                string json = File.ReadAllText(configFilePath);
                var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                serviceConfig = JsonConvert.DeserializeObject<ServiceConfig>(json, serializerSettings);
            }

            return serviceConfig;
        }


        /// <summary>
        /// Get a list of Jobs matching the source directory from the config file
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <param name="sourceDir">The name of the job</param>
        /// <returns></returns>
        public static List<Jobs> GetJobListFromSourceDir(string configFilePath, string sourceDir) {
            var jobs = new List<Jobs>();
            if (File.Exists(configFilePath)) {
                string json = File.ReadAllText(configFilePath);
                var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
                jobs = deserializedJson.Jobs.Where(x => x.Source == sourceDir).ToList();
            }

            return jobs;
        }


        /// <summary>
        /// Get a list of Jobs matching the source directory from the config file
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <param name="enableCaching">Optional, enable caching for the config path & source dirs</param>
        /// <returns></returns>
        public static List<Jobs> GetJobListFromEnabledService(string configFilePath, bool enableCaching = false) {
            var jobs = new List<Jobs>();
            if (File.Exists(configFilePath)) {
                string json = File.ReadAllText(configFilePath);
                var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
                jobs = deserializedJson.Jobs.Where(x => x.Service != null && x.Service.EnableBackgroundService).ToList();
                if (jobs.Count > 0) {
                    FileLogger.Info("Found " + jobs.Count + " task(s) with enabled option \"EnableBackgroundService\"");
                    if (enableCaching) {
                        FileSysWatcher.SetCache(configFilePath, jobs.Select(x => x.Source).ToList());
                    }
                } else {
                    FileLogger.Info("No task with enabled option \"EnableBackgroundService\"");
                }
            }
            return jobs;
        }
    }
}
