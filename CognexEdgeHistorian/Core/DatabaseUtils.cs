using CognexEdgeHistorian.MVVM.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace CognexEdgeHistorian.Core
{
    public class DatabaseUtils
    {
        public static SQLiteConnection SqlConnection { get; set; }
        public static string ConnectionString { get; set; }

        public static void OpenSQLConnection()
        {
            SqlConnection = new SQLiteConnection(ConnectionString);
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

            using (SQLiteCommand command = new SQLiteCommand(createCamerasTable, SqlConnection))
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

            using (SQLiteCommand command = new SQLiteCommand(createTagsTable, SqlConnection))
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

            using (SQLiteCommand command = new SQLiteCommand(createTagValuesTable, SqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public static void AddCamera(string cameraName, string endpoint)
        {
            string insertCamera = "INSERT INTO cameras (camera_name, camera_endpoint) VALUES (@cameraName, @endpoint)";
            using (SQLiteCommand command = new SQLiteCommand(insertCamera, SqlConnection))
            {
                command.Parameters.AddWithValue("@cameraName", cameraName);
                command.Parameters.AddWithValue("@endpoint", endpoint);
                command.ExecuteNonQuery();
            }
        }

        public static void AddTag(int cameraId, string tagName)
        {
            string insertTag = "INSERT INTO tags (camera_id, tag_name) VALUES (@cameraId, @tagName)";
            using (SQLiteCommand command = new SQLiteCommand(insertTag, SqlConnection))
            {
                command.Parameters.AddWithValue("@cameraId", cameraId);
                command.Parameters.AddWithValue("@tagName", tagName);
                command.ExecuteNonQuery();
            }
        }

        public static void StoreTagValue(int tagId, string value, string timestamp)    //! Need to add support for storing an image as a blob later on down the line
        {
            string insertTagValue = "INSERT INTO tag_values (tag_id, value, timestamp) VALUES (@tagId, @value, @timestamp)";
            using (SQLiteCommand command = new SQLiteCommand(insertTagValue, SqlConnection))
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
            using (SQLiteCommand command = new SQLiteCommand(selectCameraByEndpoint, SqlConnection))
            {
                command.Parameters.AddWithValue("@endpoint", endpoint);
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
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
            using (SQLiteCommand command = new SQLiteCommand(selectTagValues, SqlConnection))
            {
                command.Parameters.AddWithValue("@tagId", tagId);
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
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
            using (SQLiteCommand command = new SQLiteCommand(selectCameraInfo, SqlConnection))
            {
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                {
                    adapter.Fill(cameraTagInfo);
                }
            }

            return cameraTagInfo;
        }

    }
}
