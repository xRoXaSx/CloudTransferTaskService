using CloudTransferTask.src.classes.helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudTransferTaskService.classes.helper {
    class WaiterThreadHelper {
        public Jobs Job { get; set; }
        public FileSystemEventArgs e { get; set; }
        public string HashedCacheElement { get; set; }
    }
}
