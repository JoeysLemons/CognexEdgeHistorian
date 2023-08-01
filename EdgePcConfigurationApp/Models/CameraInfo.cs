using System.DirectoryServices;

namespace EdgePcConfigurationApp.Models;

public class CameraInfo
{
    public string IpAddress { get; set; }
    public string Region { get; set; }
    public string Location { get; set; }
    public string ProductionLine { get; set; }
    public string Name { get; set; }

    public CameraInfo(string ipAddress, string name)
    {
        IpAddress = ipAddress;
        Name = name;
    }
}

