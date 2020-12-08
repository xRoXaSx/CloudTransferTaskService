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
        public bool EnableBackgroundService = false;
        public string Source = "YourDirectory";
        public string Destination = "CloudDrive:BackupLocation";
        public string Action = "copy";
        public List<string> FileType = new List<string>() { "*.png", "*.jpg" };
        public List<string> Flags = new List<string>() { 
            "-v",
            "--retries 5",
            "--transfers 6"
        };
        public Actions PreAction = new Actions();
        public Actions PostAction = new Actions();
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
