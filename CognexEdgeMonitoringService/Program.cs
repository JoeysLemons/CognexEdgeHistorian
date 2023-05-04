using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeMonitoringService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if DEBUG
            CognexMonitoringService cognexEdgeMonitoringService = new CognexMonitoringService();
            cognexEdgeMonitoringService.OnDebug();
#else
ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new CognexMonitoringService()
            };
            ServiceBase.Run(ServicesToRun);
#endif

        }
    }
}
