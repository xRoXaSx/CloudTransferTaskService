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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).UseWindowsService().ConfigureServices((hostContext, services) => {
                services.AddHostedService<Worker>();
            });


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
                        if (Directory.Exists("/etc/systemd/system")) {
                            File.WriteAllText("/etc/systemd/system/cloudtransfertask.service",
                                "[Unit]" + Environment.NewLine + "Description=" + serviceDescription + Environment.NewLine + Environment.NewLine + 
                                "[Service]" + Environment.NewLine + "Type=notify" + Environment.NewLine + "ExecStart=" + binaryPath + Environment.NewLine + Environment.NewLine +
                                "[Install]" + Environment.NewLine + "WantedBy=multi-user.target" + Environment.NewLine
                            );
                        } else {
                            Console.WriteLine("ERROR: Cannot find directory \"/etc/systemd/system\"!");
                        }
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
