using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Windows.Documents;

namespace EdgePcConfigurationApp.Helpers
{
    public class DatabaseUtils
    {
        public static SqlConnection SqlConnection { get; set; }

        public static void OpenSQLConnection(string connectionString)
        {
            SqlConnection = new SqlConnection(connectionString);
            SqlConnection.Open();
        }
        public static void CloseSQLConnection()
        {
            SqlConnection.Close();
        }
        public static void CreateCamerasTable()
        {
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

        public static void CreateTagsTable()
        {
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

        public static void CreateTagValuesTable()
        {
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

        public static int AddCamera(string cameraName, string endpoint)
        {
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



        public static int AddTag(int cameraId, string tagName, string nodeId)
        {
            string insertTag = "INSERT INTO MonitoredTags (Camera_id, Name, Node_id) VALUES (@cameraId, @tagName, @nodeId); SELECT SCOPE_IDENTITY();";
            using (SqlCommand command = new SqlCommand(insertTag, SqlConnection))
            {
                command.Parameters.AddWithValue("@cameraId", cameraId);
                command.Parameters.AddWithValue("@tagName", tagName);
                command.Parameters.AddWithValue("@nodeId", nodeId);
                int newTagId = Convert.ToInt32(command.ExecuteScalar());
                return newTagId;
            }
        }

        public static void StoreTagValue(int tagId, string value, string timestamp)    //! Need to add support for storing an image as a blob later on down the line
        {
            string insertTagValue = "INSERT INTO tag_values (tag_id, value, timestamp) VALUES (@tagId, @value, @timestamp)";
            using (SqlCommand command = new SqlCommand(insertTagValue, SqlConnection))
            {
                command.Parameters.AddWithValue("@tagId", tagId);
                command.Parameters.AddWithValue("@value", value);
                command.Parameters.AddWithValue("@timestamp", timestamp);
                command.ExecuteNonQuery();
            }
        }
        public static DataTable GetCameraByEndpoint(string endpoint)
        {
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

        public static DataTable GetTagValues(int tagId)
        {
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

        public static DataTable GetCamerasAndTags()
        {
            DataTable cameraTagInfo = new DataTable();

            string selectCameraInfo = @"
                    SELECT cameras.id, cameras.camera_name, cameras.camera_endpoint, tags.id, tags.tag_name
                    FROM cameras
                    LEFT JOIN tags ON cameras.id = tags.camera_id";
            using (SqlCommand command = new SqlCommand(selectCameraInfo, SqlConnection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(cameraTagInfo);
                }
            }

            return cameraTagInfo;
        }
        public static bool CameraExists(string cameraName)
        {
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

        public static List<string> GetSavedTagConfiguration(string endpoint)
        {
            string queryString = "SELECT id FROM Cameras WHERE Endpoint = @Endpoint;";
            int cameraId = -1;
            SqlCommand command = new SqlCommand(queryString, SqlConnection);
            command.Parameters.AddWithValue("@Endpoint", endpoint);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    cameraId = reader.GetInt32(0);
                }
            }

            DataTable tags = new DataTable();
            string getTagNames = @"SELECT Name FROM MonitoredTags WHERE Camera_id = @Camera_id";
            using (SqlCommand getTagCommand = new SqlCommand(getTagNames, SqlConnection))
            {
                getTagCommand.Parameters.AddWithValue("@Camera_id", cameraId);
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
}
