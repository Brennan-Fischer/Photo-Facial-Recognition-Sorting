using Azure.Storage.Blobs;
using DlibDotNet;
using DlibDotNet.Dnn;
using DlibDotNet.Extensions;
using ImageMagick;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    class Program
    {
        private static readonly string ShapePredictorPath = "C:\\MSSA-CCAD14\\FischBowl Sorting Script\\shape_predictor_68_face_landmarks.dat";
        private static readonly string FaceRecognitionModelPath = "C:\\MSSA-CCAD14\\FischBowl Sorting Script\\dlib_face_recognition_resnet_model_v1.dat";
        private static readonly string PhotosContainerName = "photos";
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
        private static readonly string SqlConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting the photo processing script...");
            // Load the models
            ShapePredictor shapePredictor = ShapePredictor.Deserialize(ShapePredictorPath);
            LossMetric faceRecognitionModel = LossMetric.Deserialize(FaceRecognitionModelPath);

            BlobServiceClient blobServiceClient = new BlobServiceClient(ConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(PhotosContainerName);

            // Get the current max photo number in the "Processed" directory
            int currentMaxPhotoNumber = await GetCurrentMaxPhotoNumber(containerClient);

            // Encode known faces from TrainingPhotos
            Dictionary<string, List<Matrix<float>>> knownFaces = new Dictionary<string, List<Matrix<float>>>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: "TrainingPhotos/"))
            {
                string[] pathSegments = blobItem.Name.Split('/');
                if (pathSegments.Length < 2) continue;
                string name = pathSegments[1];

                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                using (var stream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(stream);
                    stream.Position = 0;
                    List<Matrix<float>> encodings = EncodeFaces(stream, shapePredictor, faceRecognitionModel, blobItem.Name);
                    if (encodings.Count > 0)
                    {
                        if (!knownFaces.ContainsKey(name))
                        {
                            knownFaces[name] = new List<Matrix<float>>();
                        }
                        knownFaces[name].AddRange(encodings);
                        Console.WriteLine($"Encoded faces for {name}");
                    }
                    else
                    {
                        Console.WriteLine($"No faces found in image {blobItem.Name}, skipping...");
                    }
                }
            }

            // Process test images from Unprocess and move them to Processed
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: "Unprocessed/"))
            {
                string extension = Path.GetExtension(blobItem.Name).ToLower();
                if (extension == ".docx")
                {
                    Console.WriteLine($"Skipping file {blobItem.Name} as it is a .docx file.");
                    continue; // Skip .docx files
                }

                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                using (var stream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(stream);
                    stream.Position = 0;
                    List<Matrix<float>> unknownEncodings = EncodeFaces(stream, shapePredictor, faceRecognitionModel, blobItem.Name);
                    if (unknownEncodings.Count == 0)
                    {
                        Console.WriteLine($"No faces found in image {blobItem.Name}, skipping...");
                        continue;
                    }

                    List<string> matchedNames = new List<string>();
                    foreach (var unknownEncoding in unknownEncodings)
                    {
                        string matchedName = IdentifyFace(unknownEncoding, knownFaces);
                        if (matchedName != null)
                        {
                            matchedNames.Add(matchedName);
                        }
                    }
                    Console.WriteLine($"{blobItem.Name} contains {string.Join(" and ", matchedNames)}");

                    // Extract date taken from photo metadata
                    DateTime? dateTaken = ExtractDateTaken(stream);
                    Console.WriteLine($"Date taken for {blobItem.Name}: {dateTaken?.ToString() ?? "Unknown"}");

                    // Save metadata to SQL Database
                    string newBlobName = $"photo_{++currentMaxPhotoNumber}.jpg";
                    string newBlobUrl = containerClient.Uri + "/Processed/" + newBlobName;
                    await SavePhotoMetaData(string.Join(", ", matchedNames), newBlobName, newBlobUrl, dateTaken);

                    // Move the processed blob to the Processed directory with a new name
                    BlobClient newBlobClient = containerClient.GetBlobClient("Processed/" + newBlobName);
                    await newBlobClient.StartCopyFromUriAsync(blobClient.Uri);
                    await blobClient.DeleteAsync();
                    Console.WriteLine($"Moved {blobItem.Name} to Processed/{newBlobName}");
                }
            }

            // Dispose of models
            shapePredictor.Dispose();
            faceRecognitionModel.Dispose();
            Console.WriteLine("Photo processing completed successfully.");
        }

        static async Task<int> GetCurrentMaxPhotoNumber(BlobContainerClient containerClient)
        {
            int maxNumber = 0;
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: "Processed/photo_"))
            {
                string fileName = Path.GetFileNameWithoutExtension(blobItem.Name);
                if (fileName.StartsWith("photo_"))
                {
                    if (int.TryParse(fileName.Substring(6), out int number))
                    {
                        if (number > maxNumber)
                        {
                            maxNumber = number;
                        }
                    }
                }
            }
            return maxNumber;
        }

        static async Task SavePhotoMetaData(string peopleIdentified, string photoName, string newBlobUrl, DateTime? dateTaken)
        {
            using (SqlConnection conn = new SqlConnection(SqlConnectionString))
            {
                await conn.OpenAsync();
                string query = "INSERT INTO PhotoMetaData (PeopleIdentified, PhotoName, BlobUrl, DateTaken) VALUES (@PeopleIdentified, @PhotoName, @BlobUrl, @DateTaken)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PeopleIdentified", peopleIdentified);
                    cmd.Parameters.AddWithValue("@PhotoName", photoName);
                    cmd.Parameters.AddWithValue("@BlobUrl", newBlobUrl);
                    cmd.Parameters.AddWithValue("@DateTaken", (object)dateTaken ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            Console.WriteLine($"Saved metadata for {photoName} to database.");
        }

        static List<Matrix<float>> EncodeFaces(Stream imageStream, ShapePredictor shapePredictor, LossMetric faceRecognitionModel, string blobName)
        {
            string tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFilePath))
            {
                imageStream.CopyTo(fileStream);
            }

            Array2D<RgbPixel> img;
            string extension = Path.GetExtension(blobName).ToLower();

            // Handle different image types
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                    img = Dlib.LoadImage<RgbPixel>(tempFilePath);
                    break;
                case ".heic":
                case ".heif":
                    string convertedFilePath = ConvertHeicToPng(tempFilePath);
                    img = Dlib.LoadImage<RgbPixel>(convertedFilePath);
                    File.Delete(convertedFilePath);
                    break;
                default:
                    throw new NotSupportedException($"Image type {extension} is not supported.");
            }

            File.Delete(tempFilePath);

            Rectangle[] faces = Dlib.GetFrontalFaceDetector().Operator(img);
            if (faces.Length == 0)
                return new List<Matrix<float>>(); // No faces found

            List<Matrix<float>> encodings = new List<Matrix<float>>();
            foreach (var face in faces)
            {
                FullObjectDetection shape = shapePredictor.Detect(img, face);
                ChipDetails chipDetails = Dlib.GetFaceChipDetails(shape, 150, 0.25);
                Array2D<RgbPixel> faceChip = Dlib.ExtractImageChip<RgbPixel>(img, chipDetails);

                using (Matrix<RgbPixel> matrix = new Matrix<RgbPixel>(faceChip))
                {
                    OutputLabels<Matrix<float>> faceDescriptor = faceRecognitionModel.Operator(matrix);
                    encodings.Add(faceDescriptor[0]);
                }
            }

            return encodings;
        }

        static string ConvertHeicToPng(string heicFilePath)
        {
            string pngFilePath = Path.GetTempFileName();
            using (var image = new MagickImage(heicFilePath))
            {
                image.Write(pngFilePath, MagickFormat.Png);
            }
            return pngFilePath;
        }

        static DateTime? ExtractDateTaken(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                var directories = ImageMetadataReader.ReadMetadata(imageStream);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
                {
                    return dateTaken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract date taken: {ex.Message}");
            }
            return null;
        }

        static string IdentifyFace(Matrix<float> unknownEncoding, Dictionary<string, List<Matrix<float>>> knownFaces)
        {
            const double Tolerance = 0.5; // Reduced tolerance for better accuracy
            foreach (KeyValuePair<string, List<Matrix<float>>> kvp in knownFaces)
            {
                foreach (var knownEncoding in kvp.Value)
                {
                    double distance = ComputeEuclideanDistance(unknownEncoding, knownEncoding);
                    if (distance < Tolerance)
                    {
                        return kvp.Key;
                    }
                }
            }
            return null;
        }

        static double ComputeEuclideanDistance(Matrix<float> encoding1, Matrix<float> encoding2)
        {
            double sum = 0.0;
            for (int i = 0; i < encoding1.Size; i++)
            {
                double diff = encoding1[i] - encoding2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
