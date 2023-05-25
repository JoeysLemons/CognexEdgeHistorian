using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace CognexEdgeMonitoringService.Core
{
    public class TelnetUtils
    {
        public static TcpClient OpenConnection(string ipAddress, int port)
        {
            TcpClient client = new TcpClient();
            client.Connect(ipAddress, port);
            
            return client;
        }

        public static void CloseConnection(TcpClient client)
        {
            client.Close();
            Console.WriteLine("Telnet client closed");
        }

        public static void LoginAsAdmin(StreamWriter writer)
        {
            writer.NewLine = "\r\n";
            writer.WriteLine("admin");
            writer.Flush();
            writer.WriteLine("");
            writer.Flush();
        }

        public static void FlushStreamBuffer(TcpClient client, NetworkStream stream)
        {
            string response;
            while (stream.DataAvailable)
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine(response);
            }
        }

        public static string SendCommand(TcpClient client, string command, NetworkStream stream, StreamWriter writer)
        {
            string response;
            
            writer.WriteLine(command);
            writer.Flush();
            
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Response from server: " + response);
            

            return response;
        }

        public static string ReadResponse(TcpClient client, NetworkStream stream)
        {
            string response;
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Response from server: " + response);

            return response;
        }

        public static void WriteCommand(string command, StreamWriter writer)
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(command);
        }
    }
}