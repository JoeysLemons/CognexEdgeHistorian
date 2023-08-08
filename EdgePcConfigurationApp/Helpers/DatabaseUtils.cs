using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Windows.Documents;
using System.Xml;

namespace EdgePcConfigurationApp.Helpers
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

        public static void CreateTagValuesTable()
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string createTagValuesTable = @"
                CREATE TABLE IF NOT EXISTS tag_values (
                    id INTEGER PRIMARY KEY,
                    tag_id INTEGER,
                    value TEXT,
                    timestamp TEXT,
                    image BLOB,
                    FOREIGN KEY (tag_id) REFERENCES tags (id)
                )";

                using (SqlCommand command = new SqlCommand(createTagValuesTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static int AddCamera(string cameraName, string endpoint)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkDuplicate = "SELECT id FROM Cameras WHERE Endpoint = @endpoint";
                string insertCamera = "INSERT INTO Cameras (Name, Endpoint) VALUES (@cameraName, @endpoint); SELECT SCOPE_IDENTITY();";
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
                string insertTagValue = "INSERT INTO tag_values (tag_id, value, timestamp) VALUES (@tagId, @value, @timestamp)";
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

                string selectCameraByEndpoint = "SELECT * FROM cameras WHERE camera_endpoint = @endpoint";
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

                string selectTagValues = "SELECT value, timestamp FROM tag_values WHERE tag_id = @tagId ORDER BY timestamp";
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

        public static void StoreManufacturingArea(string name)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO ManufacturingAreas (Name) VALUES (@name)";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
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

        public static void StoreGeoLocation(string name)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"INSERT INTO Locations (Name) VALUES (@name)";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
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
    }
}
