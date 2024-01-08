using System;
using System.IO;
using System.Xml;

namespace EdgePcConfigurationApp.Helpers;

public class FirstTimeSetupUtils
{
    private static string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.xml");

    public static int RegisterComputer()
    {
        //Checks to see whether the PC has been assigned a GUID and if it has it then checks to make sure that GUID is saved in the database
        //If either of these conditions are not met the application will generate and assign a GUID to the PC
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath); 
        XmlNode root = doc.DocumentElement;
        XmlNode pcSettings = root.SelectSingleNode("PCSettings");
        string GUID =  pcSettings.SelectSingleNode("ComputerGUID").InnerText;
        bool hasGUID = !string.IsNullOrEmpty(GUID);
        //check to make sure that the GUID has been stored in the database
        if (hasGUID)
        {
            //if no record is found add computer and replace old GUID to ensure no faulty data
            if (!DatabaseUtils.CheckPCExists(GUID))
            {
                GUID = DatabaseUtils.StoreComputer();
                pcSettings.SelectSingleNode("ComputerGUID").InnerText = GUID;
            }
        }
        else
        {
            //No guid exists in config file create guid and store in database and locally on PC
            GUID = DatabaseUtils.StoreComputer();
            pcSettings.SelectSingleNode("ComputerGUID").InnerText = GUID;
        }

        doc.Save(xmlFilePath);
        int pcID = DatabaseUtils.GetPCIdFromGUID(GUID);
        if (pcID == -1) throw new Exception("No PC with a matching GUID was found in the database");
        return pcID;
    }
}