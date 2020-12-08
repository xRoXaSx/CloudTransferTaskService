using CloudTransferTask.src.classes.helper;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace CloudTransferTask.src.classes.eventhandler {
    class FileSysWatcher {

        private static string binPathCacheKey = "BinPath";
        private static string threadSleepTriggerFrom = "createUniqueCacheElementFrom";

        public void Initialize(string watchPath) {
            if (Directory.Exists(watchPath)) {
                var fileWatcher = new FileSystemWatcher();
                try {
                    fileWatcher.Path = watchPath;
                    fileWatcher.EnableRaisingEvents = true;
                    //fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName;
                    fileWatcher.Filter = "*.*";
                    fileWatcher.Changed += new FileSystemEventHandler((sender, e) => Changed(sender, e, "changed"));
                    fileWatcher.Created += new FileSystemEventHandler((sender, e) => Changed(sender, e, "created"));
                    fileWatcher.Renamed += new RenamedEventHandler((sender, e) => Changed(sender, e, "renamed"));
                    fileWatcher.Deleted += new FileSystemEventHandler((sender, e) => Changed(sender, e, "deleted"));
                } catch (Exception e) {
                    FileLogger.Error(e.ToString());
                }
            } else {
                FileLogger.Warning("Path \"" + watchPath + "\" does not exist!");
            }
        }


        /// <summary>
        /// Gets called if a source folder changes
        /// </summary>
        /// <param name="sender">The source object</param>
        /// <param name="e">The FileSystemEventArgs</param>
        private void Changed(object sender, FileSystemEventArgs e, string action) {
            var fileSystemWatcher = sender as FileSystemWatcher;
            FileLogger.Debug("Cache currently contains " + MemoryCache.Default.GetCount() + " elements");
            var keyValuePairs = MemoryCache.Default.Where(x => x.Value is IList && x.Value.GetType().IsGenericType && ((IList<string>)x.Value).Contains(fileSystemWatcher.Path)).ToList();
            if (keyValuePairs.Count > 0) {
                foreach (var keyValuePair in keyValuePairs) {
                    var jobList = Json.GetJobListFromSourceDir(keyValuePair.Key, fileSystemWatcher.Path);
                    jobList = jobList.Where(x => x.EnableBackgroundService).Distinct().ToList();
                    if (jobList.Count > 0) {
                        var serviceConfig = Json.GetServiceConfiguration(Json.serviceConfFullPath);

                        if (!MemoryCache.Default.Contains(binPathCacheKey)) {
                            SetCache(binPathCacheKey, serviceConfig.CloudTransferTaskPath);
                        }

                        if (!MemoryCache.Default.Contains(threadSleepTriggerFrom)) {
                            SetCache(threadSleepTriggerFrom, serviceConfig.ThreadSleepTriggerFrom);
                        }

                        if (File.Exists(MemoryCache.Default[binPathCacheKey] as string)) {

                            // === Create SHA256 for the cache 
                            var uniqueCacheElement = string.IsNullOrEmpty(MemoryCache.Default[threadSleepTriggerFrom] as string) ? "file" : MemoryCache.Default[threadSleepTriggerFrom] as string;
                            var hashedCacheElement = "";
                            switch (uniqueCacheElement.ToLower()) {
                                case "file":
                                    hashedCacheElement = HashSha256(e.FullPath);
                                    break;
                                case "directory":
                                    hashedCacheElement = HashSha256(fileSystemWatcher.Path);
                                    break;
                                default:
                                    hashedCacheElement = HashSha256(fileSystemWatcher.Path);
                                    break;
                            }

                            //var hashedCacheElement = HashSha256(e.FullPath); // fileSystemWatcher.Path => Source directory
                            if (!MemoryCache.Default.Contains("ThreadSleepBeforeRCloneInMs")) {
                                SetCache("ThreadSleepBeforeRCloneInMs", serviceConfig.ThreadSleepBeforeRCloneInMs.ToString());
                            }

                            // === If cache does not contain the hashvalue add it and run task
                            if (!MemoryCache.Default.Contains(hashedCacheElement)) {
                                SetCache(hashedCacheElement, DateTime.Now.ToString(), true);
                                FileLogger.Debug("Added cacheElement to cache...");
                                foreach (var job in jobList) {
                                    FileLogger.Debug("Starting new thread...");
                                    var thread = new Thread(() => RunCloudTransferTask(hashedCacheElement, job));
                                    thread.Start();
                                }
                            } else {
                                var timeSpan = MemoryCache.Default[hashedCacheElement].ToString();
                                if (DateTime.TryParse(timeSpan, out DateTime paresedTimeSpan)) {
                                    if (int.TryParse(serviceConfig.ThreadSleepBeforeRCloneInMs.ToString(), out int threadSleepBeforeRCloneInMs)) {
                                        if (paresedTimeSpan.AddMilliseconds(threadSleepBeforeRCloneInMs) < DateTime.Now) {
                                            MemoryCache.Default.Remove(hashedCacheElement);
                                            foreach (var job in jobList) {
                                                FileLogger.Debug("Starting new thread...");
                                                var thread = new Thread(() => RunCloudTransferTask(hashedCacheElement, job));
                                                thread.Start();
                                            }
                                        } else {
                                            FileLogger.Debug("Still in cooldown...");
                                        }
                                    }
                                }
                            }
                        } else {
                            FileLogger.Error("The CloudTransferTaskPath \"" + MemoryCache.Default[binPathCacheKey] + "\" does not exist! Please make sure that the service executable does exist.");
                        }

                    } else {
                        FileLogger.Debug("Removing listener for " + fileSystemWatcher.Path);
                        fileSystemWatcher.Changed -= new FileSystemEventHandler((sender, e) => Changed(sender, e, "changed"));
                        fileSystemWatcher.Created -= new FileSystemEventHandler((sender, e) => Changed(sender, e, "created"));
                        fileSystemWatcher.Renamed -= new RenamedEventHandler((sender, e) => Changed(sender, e, "renamed"));
                        fileSystemWatcher.Deleted -= new FileSystemEventHandler((sender, e) => Changed(sender, e, "deleted"));
                    }
                }
            }
        }


        /// <summary>
        /// Run CloudTransferTask
        /// </summary>
        /// <param name="cacheElement">The cache element that should be removed from the cache afterwards</param>
        private async static void RunCloudTransferTask(string cacheElement, Jobs job) {
            try {
                await Task.Run(async () => {
                    if (Directory.Exists(job.Source)) {

                        // === Delay Task for 10 seconds (per default or given via config) to prevent too many changes at once
                        var delay = 10000;
                        if (MemoryCache.Default.Contains("ThreadSleepBeforeRCloneInMs")) {
                            int.TryParse(MemoryCache.Default["ThreadSleepBeforeRCloneInMs"].ToString(), out delay);
                        }

                        FileLogger.Info("Waiting " + FormatTimeFromMs(delay) + " seconds before running CloudTransferTask");
                        await Task.Delay(delay);
                        
                        try {
                            StartCloudTransferProcess(job.Name);
                        } catch (Exception ex) {
                            FileLogger.Error(ex.ToString());
                        }

                        MemoryCache.Default.Remove(cacheElement);
                    } else {
                        FileLogger.Warning("Directory \"" + job.Source + "\" does not exist! Skipping...");
                    }
                });
            } catch (Exception e) {
                FileLogger.Error("Exception caught: " + e.ToString() + "\n");
            }
        }


        private static void StartCloudTransferProcess(string cloudTransferTaskName) {
            try {
                if (!MemoryCache.Default.Contains(binPathCacheKey)) {
                    SetCache(binPathCacheKey, Json.GetServiceConfiguration(Json.serviceConfFullPath).CloudTransferTaskPath);
                }

                ProcessStartInfo startInfo = new ProcessStartInfo() {
                    FileName = MemoryCache.Default[binPathCacheKey].ToString(),
                    UseShellExecute = false,
                    Arguments = cloudTransferTaskName,
                };

                Process proc = new Process() { StartInfo = startInfo };
                FileLogger.Debug("Startinfo: " + startInfo.FileName + ", Args: " + string.Join(",", startInfo.Arguments));
                FileLogger.Info("Starting process...");

                proc.Start();
                proc.WaitForExit();
            } catch (Exception e) {
                FileLogger.Error(e.ToString());
            }
        }


        /// <summary>
        /// Set / append a cache entry
        /// </summary>
        /// <param name="cacheKey">The key of the cache element</param>
        /// <param name="cacheValue">The value of the cache element</param>
        public static void SetCache(string cacheKey, object cacheValue, bool isChangedFile = false) {
            using (ExecutionContext.SuppressFlow()) {
                
                var cache = MemoryCache.Default;
                string content = cache[cacheKey] as string;

                if (string.IsNullOrEmpty(content)) {
                    CacheItemPolicy policy = new CacheItemPolicy();
                    policy.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(1));

                    //List<string> list = new List<string>();
                    //list.Add(cacheValue);
                    //policy.ChangeMonitors.Add(new HostFileChangeMonitor(list));
                    //content = File.ReadAllText(cacheValue);
                    
                    cache.Set(cacheKey, cacheValue, policy);

                    // === Debugging
                    foreach (var a in cache) {
                        Console.WriteLine("CACHED INFO: " + a.Key + " > " + a.Value);
                    }

                    Console.WriteLine();
                }
            }
        }


        private static string FormatTimeFromMs(int time) {
            var returnVal = "";
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(time);
            returnVal = string.Join(" ", string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds)
                .Split(':').SkipWhile(s => Regex.Match(s, @"^00\w").Success).ToArray());
            return returnVal;
        }


        /// <summary>
        /// Hash the given string via SHA256
        /// </summary>
        /// <param name="value">The string to hash</param>
        /// <returns></returns>
        public static string HashSha256(string value) {
            using (SHA256 hash = SHA256.Create()) {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2")));
            }
        }
    }
}