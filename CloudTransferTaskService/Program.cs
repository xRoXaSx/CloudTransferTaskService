using CloudTransferTask.src.classes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace CloudTransferTaskService {
    public class Program {

        public static readonly string os = Json.DetectOS();
        public static string logLocation = "";
        internal static string logLevel = "";

        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            switch (os) {
                case "win":
                    return Host.CreateDefaultBuilder(args).UseWindowsService().ConfigureServices((hostContext, services) => {
                        services.AddHostedService<Worker>();
                    });

                default:
                    return Host.CreateDefaultBuilder(args).UseSystemd().ConfigureServices((hostContext, services) => {
                        services.AddHostedService<Worker>();
                    });

            }
        }


        public static bool InstallService() {
            var serviceDescription = "\"Service initially installed from user " + Environment.UserName + ". This service watches all source dirs for CloudTransferTask to execute the corresponding actions\"";
            var binaryPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var returnVal = false;
            try {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                switch (os) {
                    case "win":
                        startInfo.FileName = "cmd.exe";
                        break;
                    case "lin":
                        //if (Directory.Exists("/etc/systemd/system")) {
                        if (!Directory.Exists(Json.serviceConfPathLnx)) {
                            FileLogger.Notice("Cannot find directory \"" + Json.serviceConfPathLnx + "\"! Creating it...");
                            
                            try {
                                Directory.CreateDirectory(Json.serviceConfPathLnx);
                            } catch {
                                FileLogger.Error("Cannot create the directory \"" + Json.serviceConfPathLnx + "\"!");
                                Environment.Exit(1);
                            }
                        }

                        File.WriteAllText(Json.serviceInstallFullPathLnx,
                            "[Unit]" + Environment.NewLine + "Description=" + serviceDescription + Environment.NewLine + Environment.NewLine + 
                            "[Service]" + Environment.NewLine + "Type=notify" + Environment.NewLine + "ExecStart=" + binaryPath + Environment.NewLine + Environment.NewLine +
                            "[Install]" + Environment.NewLine + "WantedBy=default.target" + Environment.NewLine
                        );

                        startInfo.FileName = "/bin/bash";
                        break;
                }

                startInfo.RedirectStandardInput = true;
                startInfo.UseShellExecute = false;

                process.StartInfo = startInfo;
                process.Start();

                using (StreamWriter sw = process.StandardInput) {
                    if (sw.BaseStream.CanWrite) {
                        switch (os) {
                            case "win":
                                var serviceName = "CloudTransferTask";
                                sw.WriteLine("sc.exe create " + serviceName + " BinPath= \"" + binaryPath + "\" type= own start= auto ");
                                sw.WriteLine("sc.exe description " + serviceName + " " + serviceDescription);
                                break;
                            case "lin":
                                break;
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine("ERROR: " + e.ToString());
            }
            
            return returnVal;           
        }
    }
}
