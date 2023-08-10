using System.Xml;

namespace EdgePcConfigurationApp.Helpers;

public class AppConfigUtils
{
    private static string xmlFilePath = "../../../AppSettings.xml";

    public static string GetDBConnectionString()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath); 
        XmlNode root = doc.DocumentElement;
        XmlNode dbSettings = root.SelectSingleNode("Database");
        return dbSettings.SelectSingleNode("ConnectionString").InnerText;
    }
    
    public static string GetGeoLocation()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath); 
        XmlNode root = doc.DocumentElement;
        XmlNode pcSettings = root.SelectSingleNode("PCSettings");
        return pcSettings.SelectSingleNode("GeographicLocation").InnerText;
    }
    public static string GetManufacturingArea()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath); 
        XmlNode root = doc.DocumentElement;
        XmlNode pcSettings = root.SelectSingleNode("PCSettings");
        return pcSettings.SelectSingleNode("ManufacturingArea").InnerText;
    }
    public static string GetComputerGUID()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath); 
        XmlNode root = doc.DocumentElement;
        XmlNode pcSettings = root.SelectSingleNode("PCSettings");
        return pcSettings.SelectSingleNode("ComputerGUID").InnerText;
    }
}