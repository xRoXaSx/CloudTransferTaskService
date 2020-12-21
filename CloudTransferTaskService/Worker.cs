using CloudTransferTask.src.classes;
using CloudTransferTask.src.classes.helper;
using CloudTransferTaskService.classes.helper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace CloudTransferTaskService {
    public class Worker : BackgroundService {

        public static string configFilePath = Json.confPath + Path.DirectorySeparatorChar + Json.confFileName;
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger) {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                Json.SetConfPath();
                FileLogger.SetLoggingStatus();
                WriteConfig();

                switch (Program.os) {
                    case "win":
                        var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "CloudTransferTask");
                        if (ctl == null) {
                            InstallSvc(_logger, _hostApplicationLifetime);
                        }

                        break;
                    case "lin":
                        if (!Directory.Exists(Json.serviceInstallPathLnx)) {
                            Directory.CreateDirectory(Json.serviceInstallPathLnx);
                        }

                        if (!File.Exists(Json.serviceInstallPathLnx + Path.DirectorySeparatorChar + Json.serviceInstallFileNameLnx)) {
                            FileLogger.Debug("Service cannot be found! Creating...");
                            InstallSvc(_logger, _hostApplicationLifetime);
                        }

                        break;
                }

                new FileSysWatcherService().StartService();

                while (!stoppingToken.IsCancellationRequested) {
                    FileLogger.Info("Start completed");
                    await Task.Delay(-1, stoppingToken);
                }
            } catch (Exception e) {
                FileLogger.Error(e.ToString());
            }
        }


        private static void InstallSvc(ILogger<Worker> _logger, IHostApplicationLifetime _hostApplicationLifetime) {
            _logger.LogInformation("Service is not installed! Installing...");
            Program.InstallService();
            _logger.LogInformation("Stopping local service...");
            _hostApplicationLifetime.StopApplication();
        }


        private void WriteConfig() {
            if (!Directory.Exists(Json.serviceConfPath) && !string.IsNullOrEmpty(Json.serviceConfPath)) {
                try {
                    Directory.CreateDirectory(Json.serviceConfPath);
                    FileLogger.Info("Config folder has been created " + Json.serviceConfPath);
                } catch (Exception e) {
                    FileLogger.Error("Cannot create service config folder \n" + e.ToString() + "\nExiting...");
                    _hostApplicationLifetime.StopApplication();
                }
            }


            if (!File.Exists(Json.serviceConfFullPath)) {
                try {
                    var serviceConfig = new ServiceConfig();
                    var json = JsonConvert.SerializeObject(serviceConfig, Formatting.Indented);
                    try {
                        File.WriteAllText(Json.serviceConfFullPath, json);
                        FileLogger.Info("Configuration has been written to " + Json.serviceConfFullPath);
                    } catch (Exception ex) {
                        FileLogger.Error(ex.ToString());
                        _hostApplicationLifetime.StopApplication();
                    }
                } catch (Exception e) {
                    FileLogger.Error(e.ToString());
                }
            }
        }
    }
}
    