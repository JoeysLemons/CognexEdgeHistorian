using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Xml;
using CognexEdgeMonitoringService.Models;

namespace CognexEdgeMonitoringService.Core
{
    public class DatabaseUtils
    {
        public static string ConnectionString { get; set; }

        public static bool TestDBConnection()
        {
            bool connected = false;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {

                    sqlConnection.Open();
                    connected = true;
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return connected;
        }
        public static void CreateCamerasTable()
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string createCamerasTable = @"
                CREATE TABLE IF NOT EXISTS cameras (
                    id INTEGER PRIMARY KEY,
                    camera_name TEXT,
                    camera_endpoint TEXT
                )"
            ;

                using (SqlCommand command = new SqlCommand(createCamerasTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void CreateTagsTable()
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string createTagsTable = @"
                CREATE TABLE IF NOT EXISTS tags (
                    id INTEGER PRIMARY KEY,
                    camera_id INTEGER,
                    tag_name TEXT,
                    FOREIGN KEY (camera_id) REFERENCES cameras (id)
                )";

                using (SqlCommand command = new SqlCommand(createTagsTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }


        public static int AddCamera(string cameraName, string endpoint, int pcID)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkDuplicate = "SELECT id FROM Cameras WHERE Endpoint = @endpoint";
                string insertCamera = "INSERT INTO Cameras (Name, Endpoint, PC_id) VALUES (@cameraName, @endpoint, @pc_ID); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(checkDuplicate, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    object existingCameraId = command.ExecuteScalar();
                    if (existingCameraId != null)
                    {
                        return Convert.ToInt32(existingCameraId);
                    }
                    else
                    {
                        using (SqlCommand insertCommand = new SqlCommand(insertCamera, SqlConnection))
                        {
                            insertCommand.Parameters.AddWithValue("@cameraName", cameraName);
                            insertCommand.Parameters.AddWithValue("@endpoint", endpoint);
                            insertCommand.Parameters.AddWithValue("@pc_ID", pcID);
                            int newCameraId = Convert.ToInt32(insertCommand.ExecuteScalar());
                            return newCameraId;
                        }
                    }
                }
            }
        }

        

        public static int AddTag(int jobId, string tagName, string nodeId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkTagExists = "SELECT id FROM MonitoredTags WHERE Name = @tagName";
                string insertTag = "INSERT INTO MonitoredTags (Job_id, Name, Node_id, Monitored) VALUES (@jobId, @tagName, @nodeId, @Monitored); SELECT SCOPE_IDENTITY();";
                object existingTagId;
                using (SqlCommand command = new SqlCommand(checkTagExists, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagName", tagName);
                    existingTagId = command.ExecuteScalar();
                }
                if (existingTagId != null)
                {
                    return Convert.ToInt32(existingTagId);
                }
                using (SqlCommand command = new SqlCommand(insertTag, SqlConnection))
                {
                    command.Parameters.AddWithValue("@jobId", jobId);
                    command.Parameters.AddWithValue("@tagName", tagName);
                    command.Parameters.AddWithValue("@nodeId", nodeId);
                    command.Parameters.AddWithValue("@Monitored", 1);
                    int newTagId = Convert.ToInt32(command.ExecuteScalar());
                    return newTagId;
                }
            }
        }

        public static void StoreTagValue(int tagId, string value, string timestamp)    //! Need to add support for storing an image as a blob later on down the line
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string insertTagValue = "INSERT INTO MonitoredTagValues (tag_id, value, timestamp) VALUES (@tagId, @value, @timestamp)";
                using (SqlCommand command = new SqlCommand(insertTagValue, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagId", tagId);
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static DataTable GetCameraByEndpoint(string endpoint)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable camera = new DataTable();

                string selectCameraByEndpoint = "SELECT * FROM cameras WHERE Endpoint = @endpoint";
                using (SqlCommand command = new SqlCommand(selectCameraByEndpoint, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(camera);
                    }
                }
                return camera;
            }
        }

        public static void UpdateTagMonitoredStatus(string tagName, int jobId, int monitored)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string updateMonitoredStatus = "UPDATE MonitoredTags SET Monitored = @monitored WHERE Name = @tagName AND job_id = @jobId;";
                using (SqlCommand command = new SqlCommand(updateMonitoredStatus, SqlConnection))
                {
                    command.Parameters.AddWithValue("@monitored", monitored);
                    command.Parameters.AddWithValue("@jobId", jobId);
                    command.Parameters.AddWithValue("@tagName", tagName);
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected.ToString()}");
                }
            }
        }

        public static void ResetTagMonitoredStatus(int cameraId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string resetMonitoredStatus = "UPDATE MonitoredTags SET Monitored = 0 Where job_id = @jobId;";
                using (SqlCommand command = new SqlCommand(resetMonitoredStatus, SqlConnection))
                {
                    command.Parameters.AddWithValue("@jobId", cameraId);
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected.ToString()}");
                }
            }
        }

        public static DataTable GetTagValues(int tagId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable values = new DataTable();

                string selectTagValues = "SELECT value, timestamp FROM MonitoredTagValues WHERE tag_id = @tagId ORDER BY timestamp";
                using (SqlCommand command = new SqlCommand(selectTagValues, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagId", tagId);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(values);
                    }
                }

                return values;
            }
        }

        
        public static bool CameraExists(string cameraName)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable cameraEndpoints = new DataTable();
                string getCameras = @"SELECT Endpoint FROM Cameras";
                using (SqlCommand command = new SqlCommand(getCameras, SqlConnection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(cameraEndpoints);
                    }
                }
                // Check if cameraName exists in the DataTable
                foreach (DataRow row in cameraEndpoints.Rows)
                {
                    if (row["Endpoint"].ToString() == cameraName)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static List<string> GetSavedTagConfiguration(string endpoint)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string queryString = "SELECT id FROM Cameras WHERE Endpoint = @Endpoint;";
                int jobId = -1;
                SqlCommand command = new SqlCommand(queryString, SqlConnection);
                command.Parameters.AddWithValue("@Endpoint", endpoint);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        jobId = reader.GetInt32(0);
                    }
                }

                DataTable tags = new DataTable();
                string getTagNames = @"SELECT Name FROM MonitoredTags WHERE job_id = @job_id AND Monitored = 1";
                using (SqlCommand getTagCommand = new SqlCommand(getTagNames, SqlConnection))
                {
                    getTagCommand.Parameters.AddWithValue("@job_id", jobId);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(getTagCommand))
                    {
                        adapter.Fill(tags);
                    }
                }

                //Add all tag names that matched previous query criteria to a list
                List<string> tagNames = new List<string>();
                foreach (DataRow row in tags.Rows)
                {
                    tagNames.Add(row["Name"].ToString());
                }

                return tagNames;
            }
        }

        public static bool CheckPCExists(string guid)
        {
            //Check to see if a pc with a matching GUID exists
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT * FROM Computers WHERE GUID = @guid";
                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@guid", guid);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public static string StoreComputer()
        {
            try
            {
                string pcGuid = Guid.NewGuid().ToString();
                string pcName = Environment.MachineName;
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO Computers (Name, GUID) VALUES (@name, @guid)";
                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", pcName);
                        command.Parameters.AddWithValue("@guid", pcGuid);
                        command.ExecuteNonQuery();
                    }
                }

                return pcGuid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static int StoreManufacturingArea(string name)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO ManufacturingAreas (Name) VALUES (@name); SELECT SCOPE_IDENTITY()";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        object manufacturingAreaID = command.ExecuteScalar();
                        if (manufacturingAreaID != null && manufacturingAreaID != DBNull.Value)
                            return Convert.ToInt32(manufacturingAreaID);
                        else return -1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static string GetCameraName(string ipAddress)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT Name FROM Cameras WHERE Endpoint = @ipAddress";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@ipAddress", ipAddress);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read() ? reader.GetString(0) : string.Empty;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Handle the exception appropriately, e.g., log it or rethrow it.
                throw;
            }
        }

        public static int GetCameraID(string ipAddress)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT id FROM Cameras WHERE Endpoint = @ipAddress";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@ipAddress", ipAddress);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read() ? reader.GetInt32(0) : -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Handle the exception appropriately, e.g., log it or rethrow it.
                throw;
            }
        }

        public static List<string> GetCamerasOnPC(int id)
        {
            List<string> ipAddresses = new List<string>();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT Endpoint FROM Cameras WHERE PC_id = @pc_id";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@pc_id", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string ipAddress = reader["Endpoint"].ToString();
                                ipAddresses.Add(ipAddress);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Handle the exception appropriately, e.g., log it or rethrow it.
                throw;
            }

            return ipAddresses;
        }

        public static List<Tag> GetMonitoredTags(int jobID)
        {
            try
            {
                List<Tag> tags = new List<Tag>();
                DataTable tagsTable = new DataTable();
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string query = @"SELECT Name, Node_id, id FROM MonitoredTags WHERE Job_id = @jobID AND Monitored = 1";
                    using (SqlCommand command = new SqlCommand(query, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@jobID", jobID);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(tagsTable);
                        }

                        foreach (DataRow row in tagsTable.Rows)
                        {
                            string name = (string)row["Name"];
                            int id = (int)row["id"];
                            string nodeId = (string)row["Node_id"];
                            Tag tag = new Tag(name, nodeId);
                            tag.ID = id;
                            tags.Add(tag);
                        }
                    }
                }

                return tags;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static int StoreGeoLocation(string name)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO Locations (Name) VALUES (@name); SELECT SCOPE_IDENTITY();";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        object locationID = command.ExecuteScalar();
                        if (locationID != null && locationID != DBNull.Value)
                            return Convert.ToInt32(locationID);
                        else return -1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public static void LinkPCToLocation(int computerId, int geoLocationId, int manufacturingAreaId)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO PcLocation (Geo_Location_id, PC_id, Manufacturing_Area_id) VALUES (@geoLocationID, @PC_ID, @ManufacturingAreaID)";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@geoLocationID", geoLocationId);
                        command.Parameters.AddWithValue("@PC_ID", computerId);
                        command.Parameters.AddWithValue("@manufacturingAreaID", manufacturingAreaId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static int GetPCIdFromGUID(string guid)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT id FROM COMPUTERS WHERE GUID = @guid";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@guid", guid);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read() ? reader.GetInt32(0) : -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        /// <summary>
        /// This method will return the job ID associated with a specified job name. If no job is found a value of -1 is returned
        /// </summary>
        /// <param name="jobName">The name of the job you would like the ID of</param>
        /// <returns>The ID column for the job you specified. If no job is found -1 is returned</returns>
        public static int GetJobIdFromName(string jobName)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT id FROM Jobs WHERE Job_Name = @jobName";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@jobName", jobName);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read() ? reader.GetInt32(0) : -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static int StoreJob(string jobName, int cameraID)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO Jobs (Job_Name, Camera_id) VALUES (@jobName, @cameraID); SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@jobName", jobName);
                        command.Parameters.AddWithValue("@cameraID", cameraID);
                        object jobID = command.ExecuteScalar();
                        if (jobID != null && jobID != DBNull.Value)
                            return Convert.ToInt32(jobID);
                        else return -1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
