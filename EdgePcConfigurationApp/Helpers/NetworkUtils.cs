using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Org.BouncyCastle.Utilities.Net;
using IPAddress = System.Net.IPAddress;

namespace EdgePcConfigurationApp.Helpers;

public class NetworkUtils
{
    /// <summary>
    /// Searches all network interfaces for the provided IP address and then queries its MAC address
    /// </summary>
    /// <param name="endpoint">IP address of the device you want to get the MAC address of</param>
    /// <returns>The MAC address of the provided IP address. Returns an empty string if no device with a matching IP address is found</returns>

    public static string GetMacAddress(string ipAddress)
    {
        try
        {
            var arp = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = $"-a {ipAddress}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = arp })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string[] lines = output.Split('\n');
                if (lines.Length >= 4)
                {
                    string[] parts = lines[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        return parts[1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return null;
    }

    public static bool PingHost(string ipAddress)
    {
        using (Ping ping = new Ping())
        {
            try
            {
                PingReply reply = ping.Send(ipAddress);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException e)
            {
                Trace.WriteLine(e);
                return false;
            }
        }
    }

}