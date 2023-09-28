using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Documents;
using System.Xml;
using EdgePcConfigurationApp.Models;
using Opc.Ua;

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

        public static void UpdateCameraName(string cameraName, int cameraID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                string updateNameQuery = @"Update Cameras SET Name = @name WHERE id = @id";
                using (SqlCommand command = new SqlCommand(updateNameQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@name", cameraName);
                    command.Parameters.AddWithValue("@id", cameraID);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public static int AddCamera(string cameraName, string endpoint, string macAddress, int pcID)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkDuplicate = "SELECT id FROM Cameras WHERE Endpoint = @endpoint AND PC_id = @pcID";
                string insertCamera = "INSERT INTO Cameras (Name, Endpoint, Mac_Address, PC_id) VALUES (@cameraName, @endpoint, @MacAddress, @pc_ID); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(checkDuplicate, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    command.Parameters.AddWithValue("@pcID", pcID);
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
                            insertCommand.Parameters.AddWithValue("@MacAddress", macAddress);
                            insertCommand.Parameters.AddWithValue("@pc_ID", pcID);
                            int newCameraId = Convert.ToInt32(insertCommand.ExecuteScalar());
                            return newCameraId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method will update the endpoint of a given camera. This method also checks to make sure that there are no
        /// other cameras on the current PC with the same IP address. If there are then this method will return false indicating
        /// that the value of endpoint should not be updated
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="camId"></param>
        /// <returns>A boolean value indicating whether or not the endpoint value is valid or not. False = Invalid, True = Valid</returns>
        public static bool UpdateCameraEndpoint(string endpoint, int camId)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                object pcIDObj;
                int pcID = -1;
                object duplicateCamera = null;
                string updateEndpointQuery = "UPDATE Cameras SET Endpoint = @endpoint WHERE id = @camID;";
                string getPCIdQuery = "SELECT PC_id FROM Cameras WHERE id = @camID";
                string checkDuplicatesQuery = "SELECT id FROM Cameras WHERE Endpoint = @endpoint AND PC_id = @pcID;";
                using (SqlCommand command = new SqlCommand(getPCIdQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@camID", camId);
                    pcIDObj = command.ExecuteScalar();
                    if (pcIDObj != null)
                        pcID = (int)pcIDObj;
                }

                using (SqlCommand command = new SqlCommand(checkDuplicatesQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    command.Parameters.AddWithValue("@pcID", pcID);
                    duplicateCamera = command.ExecuteScalar();
                }
                
                //If this result is not null that means that a duplicate entry was found and the method should return false indicating this is not an available endpoint
                if (duplicateCamera != null)
                    return false;
                
                using (SqlCommand command = new SqlCommand(updateEndpointQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    command.Parameters.AddWithValue("@camID", camId);
                    command.ExecuteNonQuery();
                }

                return true;
            }
        }

        

        public static int AddTag(int jobId, string tagName, string nodeId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string checkTagExists = "SELECT id FROM MonitoredTags WHERE Name = @tagName AND Job_id = @jobId";
                string insertTag = "INSERT INTO MonitoredTags (Job_id, Name, Node_id, Monitored) VALUES (@jobId, @tagName, @nodeId, @Monitored); SELECT SCOPE_IDENTITY();";
                object existingTagId;
                using (SqlCommand command = new SqlCommand(checkTagExists, SqlConnection))
                {
                    command.Parameters.AddWithValue("@tagName", tagName);
                    command.Parameters.AddWithValue("@jobId", jobId);
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
        public static int GetCameraIdByEndpoint(string endpoint)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string selectCameraByEndpoint = "SELECT id FROM Cameras WHERE Endpoint = @endpoint";
                using (SqlCommand command = new SqlCommand(selectCameraByEndpoint, SqlConnection))
                {
                    command.Parameters.AddWithValue("@endpoint", endpoint);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read() ? reader.GetInt32(0) : -1;
                    }
                } 
            }
        }
        public static int GetCameraIdByMacAddress(string macAddress)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string selectCameraByEndpoint = "SELECT id FROM Cameras WHERE Mac_Address = @mac";
                using (SqlCommand command = new SqlCommand(selectCameraByEndpoint, SqlConnection))
                {
                    command.Parameters.AddWithValue("@mac", macAddress);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read() ? reader.GetInt32(0) : -1;
                    }
                } 
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

        public static void ResetTagMonitoredStatus(int jobId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                string resetMonitoredStatus = "UPDATE MonitoredTags SET Monitored = 0 Where job_id = @jobId;";
                using (SqlCommand command = new SqlCommand(resetMonitoredStatus, SqlConnection))
                {
                    command.Parameters.AddWithValue("@jobId", jobId);
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

        public static List<string> GetSavedTagConfiguration(int jobId)
        {
            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                // string queryString = "SELECT id FROM Cameras WHERE Endpoint = @Endpoint;";
                // int jobId = -1;
                // SqlCommand command = new SqlCommand(queryString, SqlConnection);
                // command.Parameters.AddWithValue("@Endpoint", endpoint);
                //
                // using (SqlDataReader reader = command.ExecuteReader())
                // {
                //     if (reader.Read())
                //     {
                //         jobId = reader.GetInt32(0);
                //     }
                // }

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

        /// <summary>
        /// This method will check to the DB to see if the provided GUID already exists within the database
        /// </summary>
        /// <param name="guid">String containing the PC GUID</param>
        /// <returns>True if the provided GUID already exists in the DB. False if no matching GUID is found</returns>
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
        /// <summary>
        /// Generates a GUID for the Pc and stores the computer information alongside the GUID.
        /// </summary>
        /// <returns>Returns the gererated GUID for the PC</returns>
        public static string StoreComputer()
        {
            try
            {
                string pcGuid;
                //Check to make sure that GUID is unique and does not already exist
                while (true)
                {
                    pcGuid = Guid.NewGuid().ToString();
                    if (!CheckPCExists(pcGuid))
                        break;
                }
                
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

        public static List<CameraInfo> GetpreviouslyConnectedCameras(int pcID)
        {
            try
            {
                DataTable results = new DataTable();
                List<CameraInfo> cameras = new List<CameraInfo>();
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    string queryString = @"SELECT Endpoint, Name FROM Cameras WHERE PC_id = @pcID";

                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@pcID", pcID);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(results);
                        }
                    }
            
                    foreach (DataRow row in results.Rows)
                    {
                        string name = row["Name"].ToString();
                        string endpoint = row["Endpoint"].ToString();
                        cameras.Add(new CameraInfo(endpoint, name));
                    }
                }

                return cameras;
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
                    string insertJobString = @"INSERT INTO Jobs (Job_Name, Camera_id) VALUES (@jobName, @cameraID); SELECT SCOPE_IDENTITY();";
                    string checkDuplicateJobString =
                        @"SELECT id FROM Jobs WHERE Job_Name = @jobName AND Camera_id = @cameraID;";
                    using (SqlCommand command = new SqlCommand(checkDuplicateJobString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@jobName", jobName);
                        command.Parameters.AddWithValue("@cameraID", cameraID);
                        object jobID = command.ExecuteScalar();
                        if (jobID != null)
                            return Convert.ToInt32(jobID);
                    }
                    using (SqlCommand command = new SqlCommand(insertJobString, sqlConnection))
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

        /// <summary>
        /// Checks to see if a specific camera has any records associated with it. If there are no records associated
        /// with the camera this method will return true indicating the camera is safe to be deleted from the database.
        /// Records are checked by looking for any jobs and monitored tags linked by foriegn keys to this camera.
        /// </summary>
        /// <param name="cameraID">The ID of the camera that is to be checked</param>
        /// <returns>True if camera is safe to be deleted</returns>
        public static bool CheckIfCameraCanBeDeleted(int cameraID)
        {
            bool canDelete = false;
            DataTable results = new DataTable();
            List<int> jobIDs = new List<int>();
            try
            {
                string getJobIdsQuery = @"SELECT id FROM Jobs WHERE Camera_id = @cameraID;";
                string getMonitoredTagsQuery = @"SELECT ";
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(getJobIdsQuery, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@cameraID", cameraID);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(results);
                        }

                        foreach (DataRelation row in results.Rows)
                        {
                            jobIDs.Add(Int32.Parse(row.ToString()));
                        }
                    }

                    if (jobIDs.Count == 0)
                    {
                        return true;
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return canDelete;
        }

        
        /// <summary>
        /// This method will attempt to delete a camera from the database. If the camera is successfully delete this method will
        /// return true. If the camera cannot be deleted this method will return false;
        /// A camera will not be deleted if there are any foriegn keys linked to the camera. This is intended behavior as this
        /// would result in a loss of data. If you would want to delete a camera with records the records must first be deleted.
        /// </summary>
        /// <param name="cameraID">The ID of the camera you want to delete</param>
        /// <returns>True if the camera was deleted. False if the camera could not be deleted.</returns>
        public static bool DeleteCamera(int cameraID)
        {
            try
            {
                string queryString = "DELETE FROM Cameras WHERE id = @cameraID;";
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(queryString, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@cameraID", cameraID);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException e)
            {
                Trace.WriteLine(e);
                Trace.WriteLine(e.InnerException);
                Trace.WriteLine(e.Message);
                return false;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                throw;
            }

            return true;
        }
    }
}
