using System.Collections.Generic;
using System.IO;

namespace CloudTransferTaskService.classes.helper {

    /// <summary>
    /// ServiceConfig class which contains all configurations for the service
    /// </summary>
    class ServiceConfig {
        public string CloudTransferTaskPath = "Path" + Path.DirectorySeparatorChar + "To" + Path.DirectorySeparatorChar + ".exe/.dll";
        public string LogLocation = "Path" + Path.DirectorySeparatorChar + "for" + Path.DirectorySeparatorChar + "YourLog.log";
        public string LogLevel = "Info";
        public string ThreadSleepTriggerFrom = "File";
        public int ThreadSleepBeforeRCloneInMs = -1;
    }
}
