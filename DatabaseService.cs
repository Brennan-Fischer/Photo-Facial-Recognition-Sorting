using DlibDotNet;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureKnownFacesTableExists()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KnownFaces' AND xtype='U')
                CREATE TABLE KnownFaces (
                    Name NVARCHAR(100) PRIMARY KEY,
                    Encodings VARBINARY(MAX)
                )";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Dictionary<string, List<Matrix<float>>>> LoadKnownFacesFromDatabase()
        {
            Dictionary<string, List<Matrix<float>>> knownFaces = new Dictionary<string, List<Matrix<float>>>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT Name, Encodings FROM KnownFaces";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string name = reader.GetString(0);
                            byte[] encodingsData = (byte[])reader.GetValue(1);
                            List<Matrix<float>> encodings = FaceRecognitionService.DeserializeEncodings(encodingsData);
                            knownFaces[name] = encodings;
                        }
                    }
                }
            }

            return knownFaces;
        }

        public async Task SaveKnownFaces(string name, List<Matrix<float>> encodings)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                IF EXISTS (SELECT * FROM KnownFaces WHERE Name = @Name)
                    UPDATE KnownFaces SET Encodings = @Encodings WHERE Name = @Name
                ELSE
                    INSERT INTO KnownFaces (Name, Encodings) VALUES (@Name, @Encodings)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Encodings", FaceRecognitionService.SerializeEncodings(encodings));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task SavePhotoMetaData(PhotoMetaData photoMetaData)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "INSERT INTO PhotoMetaData (PeopleIdentified, PhotoName, BlobUrl, DateTaken) VALUES (@PeopleIdentified, @PhotoName, @BlobUrl, @DateTaken)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PeopleIdentified", photoMetaData.PeopleIdentified);
                    cmd.Parameters.AddWithValue("@PhotoName", photoMetaData.PhotoName);
                    cmd.Parameters.AddWithValue("@BlobUrl", photoMetaData.BlobUrl);
                    cmd.Parameters.AddWithValue("@DateTaken", (object)photoMetaData.DateTaken ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            Console.WriteLine($"Saved metadata for {photoMetaData.PhotoName} to database.");
        }
    }

}
