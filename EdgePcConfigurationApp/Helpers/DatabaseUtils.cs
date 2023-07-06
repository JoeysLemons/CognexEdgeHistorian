using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using Npgsql;
using System.Windows.Documents;

namespace EdgePcConfigurationApp.Helpers
{
    public class DatabaseUtils
    {
        public static string ConnectionString { get; set; }
        public static void CreateCamerasTable()
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string createCamerasTable = @"
                CREATE TABLE IF NOT EXISTS cameras (
                    id INTEGER PRIMARY KEY,
                    camera_name TEXT,
                    camera_endpoint TEXT
                )"
            ;

                using (NpgsqlCommand command = new NpgsqlCommand(createCamerasTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void CreateTagsTable()
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string createTagsTable = @"
                CREATE TABLE IF NOT EXISTS tags (
                    id INTEGER PRIMARY KEY,
                    camera_id INTEGER,
                    tag_name TEXT,
                    FOREIGN KEY (camera_id) REFERENCES cameras (id)
                )";

                using (NpgsqlCommand command = new NpgsqlCommand(createTagsTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void CreateTagValuesTable()
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
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

                using (NpgsqlCommand command = new NpgsqlCommand(createTagValuesTable, SqlConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static int AddCamera(string cameraName, string endpoint)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkDuplicate = "CALL check_camera_exists(@endpoint);";
                string insertCamera = "INSERT INTO Cameras (Name, Endpoint) VALUES (@cameraName, @endpoint); SELECT SCOPE_IDENTITY();";
                using (NpgsqlCommand command = new NpgsqlCommand(checkDuplicate, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    object existingCameraId = command.ExecuteScalar();
                    if (existingCameraId != null)
                    {
                        return Convert.ToInt32(existingCameraId);
                    }
                    else
                    {
                        using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertCamera, SqlConnection))
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



        public static int AddTag(int cameraId, string tagName, string nodeId)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkTagExists = "CALL check_tag_exists(@tagName);";
                string insertTag = "CALL add_tag(@cameraId, @tagName, @nodeId)";
                object existingTagId;
                using (NpgsqlCommand command = new NpgsqlCommand(checkTagExists, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagName", tagName);
                    existingTagId = command.ExecuteScalar();
                }
                if (existingTagId != null)
                {
                    return Convert.ToInt32(existingTagId);
                }
                using (NpgsqlCommand command = new NpgsqlCommand(insertTag, SqlConnection))
                {
                    command.Parameters.AddWithValue("@cameraId", cameraId);
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
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string insertTagValue = "CALL store_tag_value(@tagId, @value, @timestamp)";
                using (NpgsqlCommand command = new NpgsqlCommand(insertTagValue, SqlConnection))
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
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable camera = new DataTable();

                string selectCameraByEndpoint = "SELECT * FROM cameras WHERE camera_endpoint = @endpoint";
                using (NpgsqlCommand command = new NpgsqlCommand(selectCameraByEndpoint, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                    {
                        adapter.Fill(camera);
                    }
                }
                return camera;
            }
        }

        public static void UpdateTagMonitoredStatus(string tagName, int cameraId, int monitored)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string updateMonitoredStatus = "CALL updat";
                using (NpgsqlCommand command = new NpgsqlCommand(updateMonitoredStatus, SqlConnection))
                {
                    command.Parameters.AddWithValue("@monitored", monitored);
                    command.Parameters.AddWithValue("@CameraId", cameraId);
                    command.Parameters.AddWithValue("@tagName", tagName);
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected.ToString()}");
                }
            }
        }

        public static void ResetTagMonitoredStatus(int cameraId)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string resetMonitoredStatus = "CALL reset_tag_monitored_status(@CameraId)";
                using (NpgsqlCommand command = new NpgsqlCommand(resetMonitoredStatus, SqlConnection))
                {
                    command.Parameters.AddWithValue("@CameraId", cameraId);
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected.ToString()}");
                }
            }
        }

        public static DataTable GetTagValues(int tagId)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable values = new DataTable();

                string selectTagValues = "SELECT value, timestamp FROM tag_values WHERE tag_id = @tagId ORDER BY timestamp";
                using (NpgsqlCommand command = new NpgsqlCommand(selectTagValues, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagId", tagId);
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                    {
                        adapter.Fill(values);
                    }
                }

                return values;
            }
        }

        public static DataTable GetCamerasAndTags()
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable cameraTagInfo = new DataTable();

                string selectCameraInfo = @"
                    SELECT cameras.id, cameras.camera_name, cameras.camera_endpoint, tags.id, tags.tag_name
                    FROM cameras
                    LEFT JOIN tags ON cameras.id = tags.camera_id";
                using (NpgsqlCommand command = new NpgsqlCommand(selectCameraInfo, SqlConnection))
                {
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                    {
                        adapter.Fill(cameraTagInfo);
                    }
                }

                return cameraTagInfo;
            }
        }
        public static bool CameraExists(string cameraName)
        {
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                DataTable cameraEndpoints = new DataTable();
                string getCameras = @"SELECT Endpoint FROM Cameras";
                using (NpgsqlCommand command = new NpgsqlCommand(getCameras, SqlConnection))
                {
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
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
            using (NpgsqlConnection SqlConnection = new NpgsqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string queryString = "SELECT id FROM Cameras WHERE Endpoint = @Endpoint;";
                int cameraId = -1;
                NpgsqlCommand command = new NpgsqlCommand(queryString, SqlConnection);
                command.Parameters.AddWithValue("@Endpoint", endpoint);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        cameraId = reader.GetInt32(0);
                    }
                }

                DataTable tags = new DataTable();
                string getTagNames = @"SELECT Name FROM MonitoredTags WHERE Camera_id = @Camera_id AND Monitored = 1";
                using (NpgsqlCommand getTagCommand = new NpgsqlCommand(getTagNames, SqlConnection))
                {
                    getTagCommand.Parameters.AddWithValue("@Camera_id", cameraId);
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(getTagCommand))
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
    }
}
