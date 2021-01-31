using CloudTransferTask.src.classes.eventhandler;
using CloudTransferTaskService;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace CloudTransferTask.src.classes.helper {

    class FileSysWatcherService {

        private static string enumeratePathToDetermineAllUserNames = Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
        private static List<FileSystemWatcher> fileSystemWatchers_;

        public void StartService() {
            var fileSystemWatchers = new List<FileSystemWatcher>();
            FileLogger.Info("Service started...");
            var jobs = new List<Jobs>();
            var confDirs = GetConfigPathOfAllUsers();
            foreach (var confDir in confDirs) {
                var usersConfigFile = confDir + Path.DirectorySeparatorChar + Json.confFileName;
                if (Directory.Exists(confDir) && File.Exists(usersConfigFile)) {
                    FileLogger.Debug("Configdir and config do exist!");
                    var jobList = Json.GetJobListFromEnabledService(usersConfigFile, true);
                    if (jobList.Count > 0) {
                        //FileSysWatcher.SetCache("","");
                        FileLogger.Debug("Service enabled jobs: ");
                        foreach (var job in jobList) {
                            FileLogger.Debug("   -> " + job.Name);
                            fileSystemWatchers.Add(new FileSysWatcher().Initialize(job));
                        }
                    } else {
                        FileLogger.Info("No service enabled jobs! Exiting...");
                        System.Environment.ExitCode = 1;
                    }
                }
            }

            fileSystemWatchers_ = fileSystemWatchers;
            FileLogger.Debug("Added " + fileSystemWatchers_.Count + " FileSystemWatchers!");
        }


        /// <summary>
        /// Return the configFullPath of all users
        /// </summary> 
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
                // === Determine users via home directory
                //foreach (var homeDir in Directory.GetDirectories(enumeratePathToDetermineAllUserNames)) {
                //    var homeDir_ = new DirectoryInfo(homeDir).Name;
                //    var confPathLnx = Json.confPathLnx.Replace(System.Environment.UserName, homeDir_);
                //    returnVal.Add(confPathLnx);
                    FileLogger.Debug("Service user: " + System.Environment.UserName);
                    FileLogger.Debug("confPathLnx: " + Json.confPathLnx);
                    
                    returnVal.Add(Json.confPathLnx);
                    FileSysWatcher.SetCache(System.Environment.UserName, Json.confPathLnx);
                    //}

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
