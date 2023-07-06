using System.DirectoryServices;

namespace EdgePcConfigurationApp.Models;

public class CameraInfo
{
    public string IpAddress { get; set; }
    public string Region { get; set; }
    public string Location { get; set; }
    public string ProductionLine { get; set; }
    public string Name { get; set; }

    public CameraInfo(string ipAddress, string name, string region, string location, string productionLine)
    {
        IpAddress = ipAddress;
        Name = name;
        Region = region;
        Location = location;
        ProductionLine = productionLine;
    }
}

