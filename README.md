# CloudTransferTaskService
## About CloudTransferTaskService 🎉
CloudTransferTaskService is an additional C# based service for [CloudTransferTask](https://github.com/xRoXaSx/CloudTransferTask) (rclone).   
This service allows you to set listeners to your source directories for CloudTransferTask rclone actions.  
That means if you create, change, rename, move or delete files in the source path the service will pick up that change.  
It'll look for that specific source directory in the configuration files of all local users (if the files exist).  
If a job contains that directory and `EnableBackgroundService` is set to `true` the job will be run automatically.  
No more manual copying / syncing files 🎉

## Requirements & Installation
### Requirements:
- **OS**: Windows (Unix like operating systems will come in the future!)
- **rclone**: Newest version [get it from here](https://rclone.org/)
- **CloudTransferTask**: Newest version [get it from here](https://github.com/xRoXaSx/CloudTransferTask)
- **Runtime**: .NET Core 3.1
- **Time to set it up** ☕

### Installation
1. Open an elevated terminal window and run CloudTransferTaskService.exe (`Path\To\CloudTransferTaskService.exe`)

### Check out the wiki for more and detailed information! 😊
