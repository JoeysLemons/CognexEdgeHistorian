using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace EdgePcConfigurationApp.Helpers;

public class NetworkUtils
{
    /// <summary>
    /// Searches all network interfaces for the provided IP address and then queries its MAC address
    /// </summary>
    /// <param name="endpoint">IP address of the device you want to get the MAC address of</param>
    /// <returns>The MAC address of the provided IP address. Returns an empty string if no device with a matching IP address is found</returns>
    public static string GetMacAddress(string endpoint)
    {
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var networkInterface in networkInterfaces)
        {
            var ipProperties = networkInterface.GetIPProperties();
            foreach (var ipAddress in ipProperties.UnicastAddresses)
            {
                if (ipAddress.Address.ToString() == endpoint)
                {
                    // This network interface matches the server's IP address.
                    // You can retrieve the MAC address from this interface.
                    PhysicalAddress macAddress = networkInterface.GetPhysicalAddress();
                    string macAddressString = BitConverter.ToString(macAddress.GetAddressBytes());
                    Trace.WriteLine($"MAC Address: {macAddressString}");
                    return macAddressString;
                }
            }
        }
        return String.Empty;
    }
}