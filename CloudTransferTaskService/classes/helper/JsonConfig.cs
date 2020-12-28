using System.Collections.Generic;

namespace CloudTransferTask.src.classes.helper {

    /// <summary>
    /// The whole configuration file
    /// </summary>
    class JsonConfig {

        public string RCloneProgramLocation = "TheLocationOfRClone";
        public bool Debug = false;
        public string RCloneConsoleColor = "Gray";
        public List<Jobs> Jobs = new List<Jobs>() { new Jobs() };
    }


    /// <summary>
    /// The list of jobs
    /// </summary>
    class Jobs {
        public string Name = "ExampleJobToCloud";
        public string Source = "YourDirectory";
        public string Destination = "CloudDrive:BackupLocation";
        public string Action = "copy";
        public List<string> FileType = new List<string>() { "*.png", "*.jpg" };
        public List<string> Flags = new List<string>() {
            "-v",
            "--retries 5",
            "--transfers 6"
        };
        public Service Service = new Service();
        public Actions PreAction = new Actions();
        public Actions PostAction = new Actions();
    }


    /// <summary>
    /// Settings for the background service
    /// </summary>
    class Service {
        public bool EnableBackgroundService = false;
        public bool MonitorSubdriectories = false;
        public string FileFilter = "*.*";
        public List<string> AdvancedFileFilter = new List<string>() { "*.*" };
        public List<string> IgnoreFileFilter = new List<string>() { "*.tmp", "*.temp" };
        public List<string> NotifyFilter = new List<string>() { 
            "LastWrite",
            "FileName",
            "Size",
            "DirectoryName"
        };
        public EventListener EventListeners = new EventListener();
    }


    /// <summary>
    /// Settings for the background service event listeners
    /// </summary>
    class EventListener {
        public bool TrackFileCreations = true;
        public bool TrackFileRenamings = true;
        public bool TrackFileModifications = true;
        public bool TrackFileDeletions = true;
    }


    /// <summary>
    /// Action class before rclone will be executed
    /// </summary>
    class Actions {
        public bool Enabled = false;
        public bool FailIfNotSucceeded = false;
        public string MainCommand = "Your Command Or Program";
        public int MilliSecondsUntillTimeout = -1;
        public bool ContinueAfterTimeOut = true;
        public List<string> AdditionalArguments = new List<string>() {
            "RCloneProgramLocation:<RCloneProgramLocation>",
            "Action:<Action>",
            "Source:<Source>",
            "Destination:<Destination>",
            "Flags:<Flags>"
        };
    }
}
