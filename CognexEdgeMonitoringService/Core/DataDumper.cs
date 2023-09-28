using System;
using System.Diagnostics;
using System.IO;
using Org.BouncyCastle.Asn1.Cmp;

namespace CognexEdgeMonitoringService.Core
{
    public class DataDumper
    {
        private static string filePath = @"C:\Users\jverstraete\Desktop\DataDumps\Run1.csv";

        
        /// <summary>
        /// Takes in a string containing data in a csv format and writes it to a file
        /// </summary>
        /// <param name="data">a string containing data in a csv format</param>
        public static void DumpData(string data)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(data);
            }
        }
    }
}