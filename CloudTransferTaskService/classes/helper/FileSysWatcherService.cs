using CloudTransferTask.src.classes.eventhandler;
using CloudTransferTaskService;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace CloudTransferTask.src.classes.helper {

    class FileSysWatcherService {

        public void StartService() {
            FileLogger.Info("Service started...");
            var jobs = new List<Jobs>();
            var confDirs = GetConfigPathOfAllUsers();
            foreach (var confDir in confDirs) {
                var usersConfigFile = confDir + Path.DirectorySeparatorChar + Json.confFileName;
                if (Directory.Exists(confDir) && File.Exists(usersConfigFile)) {
                    FileLogger.Debug("Configdir and config do exist!");
                    var jobList = Json.GetJobListFromEnabledService(usersConfigFile, true);
                    if (jobList.Count > 0) {
                        FileSysWatcher.SetCache("","");
                        FileLogger.Debug("Service enabled jobs: ");
                        foreach (var job in jobList) {
                            FileLogger.Debug("   -> " + job.Name);
                            new FileSysWatcher().Initialize(job.Source);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Return the configFullPath of all user
        /// </summary>
        /// <param name="userName">The user for which the config should be get</param>
        /// <returns></returns>
        public static List<string> GetConfigPathOfAllUsers() {
            var returnVal = new List<string>();
            switch (Program.os) {
                case "win":
                    var search = new ManagementObjectSearcher(new SelectQuery("Select * from Win32_UserAccount WHERE Disabled=False"));
                    foreach (ManagementObject env in search.Get()) {
                        var currentUserName = env["Name"].ToString();
                        var configPathOfUser = Regex.Replace(Json.confPathWin, @"(Users\\)(.*)(\\AppData)", m => m.Groups[1] + currentUserName + m.Groups[3]);
                        returnVal.Add(configPathOfUser);
                        FileSysWatcher.SetCache(currentUserName, configPathOfUser);
                    }

                    break;
                case "lin":
                case "fbd":
                    break;
                case "mos":
                    break;

            }

            return returnVal;
        }



        /// <summary>
        /// Return the configFullPath of a single user
        /// </summary>
        /// <param name="userName">The user for which the config should be get</param>
        /// <returns></returns>
        public static string GetConfigPathOfUser(string userName) {
            var returnVal = "";
            switch (Program.os) {
                case "win":
                    returnVal = Regex.Replace(Json.confPathWin, @"(Users\\)(.*)(\\AppData)", m => m.Groups[1] + userName + m.Groups[3]);

                break;
                case "lin":
                case "fbd":
                break;
                case "mos":
                break;

            }

            return returnVal;
        }
    }
}
