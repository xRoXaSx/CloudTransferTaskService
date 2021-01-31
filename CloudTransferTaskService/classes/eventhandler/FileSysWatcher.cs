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
using CloudTransferTaskService.classes.helper;

namespace CloudTransferTask.src.classes.eventhandler {
    class FileSysWatcher {

        private static string binPathCacheKey = "BinPath";
        private static string threadSleepTriggerFrom = "createUniqueCacheElementFrom";
        private static int sleepTimeInMillisecondsForModificationChecker = 50;
        private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();


        /// <summary>
        /// Initialize the FileSystemWatcher(s)
        /// </summary>
        /// <param name="job">The corresponding job for which the FileSystemWatcher should be initialized</param>
        /// <returns></returns>
        public FileSystemWatcher Initialize(Jobs job) {
            FileSystemWatcher returnVal = null;
            if (Directory.Exists(job.Source) || File.Exists(job.Source)) {
                fileSystemWatcher = new FileSystemWatcher();
                var eventSubscribed = false;
                GC.KeepAlive(fileSystemWatcher);
                if (!Enum.TryParse(string.Join(",", job.Service.NotifyFilter), out NotifyFilters notifyFilters)) {
                    FileLogger.Warning("Could not parse NotifyFilter for job \"" + job.Name + "\" ! Using LastWrite, FileName, Size, DirectoryName");
                    notifyFilters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName;
                }

                try {
                    fileSystemWatcher.Path = job.Source;
                    fileSystemWatcher.EnableRaisingEvents = true;
                    fileSystemWatcher.NotifyFilter = notifyFilters;
                    fileSystemWatcher.Filters.Add(job.Service.FileFilter);
                    fileSystemWatcher.IncludeSubdirectories = job.Service.MonitorSubdriectories;
                    fileSystemWatcher.Error += FileWatcherError;
                    fileSystemWatcher.Disposed += FileWatcherDisposed;

                    if (job.Service != null && job.Service.EventListeners != null) {
                        if (job.Service.EventListeners.TrackFileCreations) {
                            fileSystemWatcher.Created += Changed;
                            eventSubscribed = true;
                        }

                        if (job.Service.EventListeners.TrackFileModifications) {
                            fileSystemWatcher.Changed += Changed;
                            eventSubscribed = true;
                        }

                        if (job.Service.EventListeners.TrackFileRenamings) {
                            fileSystemWatcher.Renamed += Changed;
                            eventSubscribed = true;
                        }

                        if (job.Service.EventListeners.TrackFileDeletions) {
                            fileSystemWatcher.Deleted += Changed;
                            eventSubscribed = true;
                        }
                    }

                    if (!eventSubscribed) {
                        fileSystemWatcher.Dispose();
                    } else {
                        returnVal = fileSystemWatcher;
                    }

                } catch (Exception e) {
                    FileLogger.Error(e.ToString());
                }
            } else {
                FileLogger.Warning("Path \"" + job.Source + "\" does not exist!");
            }

            return returnVal;
        }


        /// <summary>
        /// Triggered if FileSystemWatcher gets disposed
        /// </summary>
        /// <param name="sender">The source object</param>
        /// <param name="e">The EventArgs</param>
        private void FileWatcherDisposed(object sender, EventArgs e) {
            var fileSystemWatcher = sender as FileSystemWatcher;
            FileLogger.Debug("FileSystemWatcher for " + fileSystemWatcher.Path + " has been disposed!");
        }


        /// <summary>
        /// Triggered if FileSystemWatcher runs into an error
        /// </summary>
        /// <param name="sender">The source object</param>
        /// <param name="e">The ErrorEventArgs</param>
        private void FileWatcherError(object sender, ErrorEventArgs e) {
            Console.WriteLine(e.ToString());
            FileLogger.Error(e.ToString());
        }


        /// <summary>
        /// Gets called if a source folder changes
        /// </summary>
        /// <param name="sender">The source object</param>
        /// <param name="e">The FileSystemEventArgs</param>
        private void Changed(object sender, FileSystemEventArgs e) {
            new Thread(() => { 
                try {
                    string cacheElementFileChangedPrefix = "changed_";
                    var fileSystemWatcher = sender as FileSystemWatcher;
                    FileLogger.Debug("Cache currently contains " + MemoryCache.Default.GetCount() + " elements");
                    var keyValuePairs = MemoryCache.Default.Where(x => x.Value is IList && x.Value.GetType().IsGenericType && ((IList<string>)x.Value).Contains(fileSystemWatcher.Path)).ToList();
                    if (keyValuePairs.Count > 0) {
                        foreach (var keyValuePair in keyValuePairs) {
                            var jobList = Json.GetJobListFromSourceDir(keyValuePair.Key, fileSystemWatcher.Path);
                            jobList = jobList.Where(x => (x.Service != null && x.Service.EnableBackgroundService)).Distinct().ToList();
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

                                    if (!MemoryCache.Default.Contains("ThreadSleepBeforeRCloneInMs")) {
                                        SetCache("ThreadSleepBeforeRCloneInMs", serviceConfig.ThreadSleepBeforeRCloneInMs.ToString());
                                    }

                                    Task.Delay(sleepTimeInMillisecondsForModificationChecker + 15).Wait();

                                    var fileHasBeenChanged = false;
                                    if (MemoryCache.Default.Contains(cacheElementFileChangedPrefix + hashedCacheElement)) {
                                        bool.TryParse(MemoryCache.Default[cacheElementFileChangedPrefix + hashedCacheElement].ToString(), out fileHasBeenChanged);
                                    }

                                    // === If cache does not contain the hashvalue add it and run task
                                    // === First modification of the file will return true
                                    if (!MemoryCache.Default.Contains(hashedCacheElement) || fileHasBeenChanged) {
                                        SetCache(hashedCacheElement, DateTime.Now.ToString(), true);
                                        FileLogger.Debug("Added cacheElement to cache...");
                                        StartWaiterThreadFromJobList(jobList, e, hashedCacheElement);
                                    } else {
                                        var timeSpan = MemoryCache.Default[hashedCacheElement].ToString();
                                        if (DateTime.TryParse(timeSpan, out DateTime paresedTimeSpan)) {
                                            if (int.TryParse(serviceConfig.ThreadSleepBeforeRCloneInMs.ToString(), out int threadSleepBeforeRCloneInMs)) {
                                                if (paresedTimeSpan.AddMilliseconds(threadSleepBeforeRCloneInMs) < DateTime.Now) {
                                                    StartWaiterThreadFromJobList(jobList, e, hashedCacheElement);
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
                                fileSystemWatcher.Dispose();
                            }
                        }
                    }
                } catch (Exception ex) {
                    FileLogger.Error("Exception caught: " + ex.ToString());
                }
            }).Start();
        }


        /// <summary>
        /// Start a new thread wich will monitor a file / directory (if it has been modified).
        /// </summary>
        /// <param name="waiterThreadHelper"></param>
        private void StartWaiterThread(WaiterThreadHelper waiterThreadHelper) {
            var runningStatePrefix = "running_";
            var thread = new Thread(() => {
                if (!MemoryCache.Default.Contains(runningStatePrefix + waiterThreadHelper.HashedCacheElement)) {
                    SetCache(runningStatePrefix + waiterThreadHelper.HashedCacheElement, true);
                }

                Task.Run(async () => {
                    var initialDateTime = DateTime.Now;
                    var delayTaskAfterLastEvent = GetTimeInSecondsFromString(waiterThreadHelper.Job.Service.DelayTaskAfterLastEvent);
                    var fileChanged = false;

                    // === Fallback if int cannot be parsed (1 min)
                    if (delayTaskAfterLastEvent == -2) {
                        delayTaskAfterLastEvent = 60;
                    } else {
                        delayTaskAfterLastEvent += 2;
                    }

                    if (waiterThreadHelper.e.ChangeType != WatcherChangeTypes.Deleted) {
                        var initialLastWriteTime = new FileInfo(waiterThreadHelper.e.FullPath).LastWriteTime;
                        
                        while (initialDateTime.AddMilliseconds(delayTaskAfterLastEvent * 1000) > DateTime.Now && !fileChanged) {
                            var lastWriteTime = new FileInfo(waiterThreadHelper.e.FullPath).LastWriteTime;

                            // === Check if file has been modified
                            if (initialLastWriteTime == lastWriteTime) {
                                await Task.Delay(sleepTimeInMillisecondsForModificationChecker);
                            } else {
                                fileChanged = true;
                                break;
                            }
                        }

                    } else {
                        await Task.Delay(delayTaskAfterLastEvent * 1000);
                    }
                    
                    if (fileChanged) {
                        FileLogger.Debug("File has been modified while waiting for \"DelayTaskAfterLastEvent\" (" + waiterThreadHelper.Job.Service.DelayTaskAfterLastEvent + ") ");
                    } else {
                        if (MemoryCache.Default.Contains(runningStatePrefix + waiterThreadHelper.HashedCacheElement)) {
                            MemoryCache.Default.Remove(runningStatePrefix + waiterThreadHelper.HashedCacheElement);
                            RunCloudTransferTask(waiterThreadHelper.HashedCacheElement, waiterThreadHelper.Job);
                        }
                    }
                });
            });

            thread.Start();
        }


        /// <summary>
        /// Start waiter thread from a List<job>
        /// </summary>
        /// <param name="jobList">The list of jobs to start the waiter thread from</param>
        /// <param name="e">The FileSystemEventArgs</param>
        /// <param name="hashedCacheElement">The HashedCacheElement (hashed file or directory name)</param>
        private void StartWaiterThreadFromJobList(List<Jobs>jobList, FileSystemEventArgs e, string hashedCacheElement) {
            foreach (var job in jobList) {
                if (FileDoesNotMatchFilter(e.Name, job.Service.AdvancedFileFilter)) {
                    if (FileDoesNotMatchFilter(e.Name, job.Service.IgnoreFileFilter, true)) {
                        if (StringIsEqualToFalse(job.Service.DelayTaskAfterLastEvent)) {
                            FileLogger.Debug("Starting new thread...");
                            var thread = new Thread(() => RunCloudTransferTask(hashedCacheElement, job));
                            thread.Start();
                        } else {
                            FileLogger.Debug("Starting new thread to wait for last event...");
                            var waiterThreadHelper = new WaiterThreadHelper() {
                                HashedCacheElement = hashedCacheElement,
                                e = e,
                                Job = job
                            };

                            StartWaiterThread(waiterThreadHelper);
                        }
                    } else {
                        FileLogger.Debug("File matched IgnoreFileFilter, ignoring it!");
                    }
                }
            }
        }

        /// <summary>
        /// Parse time from string (Eg: 5m)
        /// </summary>
        /// <param name="time">The time that should be parsed</param>
        /// <returns></returns>
        private int GetTimeInSecondsFromString(string time) {
            var matches = Regex.Matches(time, @"((\d+)([dhms]))+?");
            var returnVal = -2;
            foreach (Match match in matches) {
                if (match.Success) {
                    var parsedInt = 0;
                    int.TryParse(match.Groups[2].ToString(), out parsedInt);

                    switch (match.Groups[3].ToString()) {
                        case "d":
                            returnVal += parsedInt * 86400;
                            break;

                        case "h":
                            returnVal += parsedInt * 3600;
                            break;

                        case "m":
                            returnVal += parsedInt * 60;
                            break;

                        case "s":
                            returnVal += parsedInt;
                            break;
                    }
                }
            }

            FileLogger.Debug("Time from " + time + " in seconds: " + returnVal);
            return returnVal;
        }


        /// <summary>
        /// If string should be considered as a false boolean
        /// </summary>
        /// <param name="delayTaskAfterLastEvent">The string to check</param>
        /// <returns></returns>
        private bool StringIsEqualToFalse(string delayTaskAfterLastEvent) {
            bool returnVal = true;
            if (!string.IsNullOrEmpty(delayTaskAfterLastEvent) && delayTaskAfterLastEvent != "false" && delayTaskAfterLastEvent != "-1" && delayTaskAfterLastEvent != "null") {
                returnVal = false;
            }

            return returnVal;
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

                        
                        if (delay > 0) {
                            FileLogger.Info("Waiting " + FormatTimeFromMs(delay) + " seconds before running CloudTransferTask");
                            await Task.Delay(delay);
                        }
                        
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
                Console.WriteLine("Exception caught: " + e.ToString());
                FileLogger.Error("Exception caught: " + e.ToString());
            }
        }


        /// <summary>
        /// Start CloudTransferTask
        /// </summary>
        /// <param name="cloudTransferTaskName"></param>
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
                    CacheItemPolicy policy = new CacheItemPolicy {
                        Priority = CacheItemPriority.NotRemovable
                    };
                  
                    cache.Set(cacheKey, cacheValue, policy);
                }
            }
        }


        /// <summary>
        /// Fromat string from milliseconds 
        /// </summary>
        /// <param name="time">The milliseconds</param>
        /// <returns></returns>
        private static string FormatTimeFromMs(int time) {
            var returnVal = "";
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(time);
            returnVal = string.Join(" ", string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds)
                .Split(':').SkipWhile(s => Regex.Match(s, @"^00\w").Success).ToArray());
            return returnVal;
        }


        /// <summary>
        /// Check if a string matches any wildcard
        /// </summary>
        /// <param name="input">The string to check</param>
        /// <param name="mask">The mask which should match the input</param>
        /// <returns></returns>
        private bool FileDoesNotMatchFilter(string input, List<string> mask, bool invertFileMatch = false) {
            var returnVal = false;
            if (!string.IsNullOrEmpty(input) && (mask.All(x => !string.IsNullOrEmpty(x)))) {
                List<Regex> masks = mask.Select(x => new Regex(x.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."))).ToList();
                returnVal = masks.Any(x => x.IsMatch(input));

                if (invertFileMatch) {
                    returnVal = !returnVal;
                }
            } else {
                returnVal = true;
            }

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