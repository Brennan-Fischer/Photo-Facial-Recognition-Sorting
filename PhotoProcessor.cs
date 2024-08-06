using DlibDotNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public class PhotoProcessor
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly DatabaseService _databaseService;
        private readonly FaceRecognitionService _faceRecognitionService;
        private readonly ImageService _imageService;

        public PhotoProcessor()
        {
            _blobStorageService = new BlobStorageService(Configuration.ConnectionString, Configuration.PhotosContainerName);
            _databaseService = new DatabaseService(Configuration.SqlConnectionString);
            _faceRecognitionService = new FaceRecognitionService(Configuration.ShapePredictorPath, Configuration.FaceRecognitionModelPath);
            _imageService = new ImageService();
        }

        public async Task ProcessPhotosAsync()
        {
            Console.WriteLine("Starting the photo processing script...");

            await _databaseService.EnsureKnownFacesTableExists();

            int currentMaxPhotoNumber = await _blobStorageService.GetCurrentMaxPhotoNumber();

            Dictionary<string, List<Matrix<float>>> knownFaces = await _databaseService.LoadKnownFacesFromDatabase();
            Dictionary<string, List<Matrix<float>>> newEncodings = new Dictionary<string, List<Matrix<float>>>();

            await ProcessTrainingPhotos(knownFaces, newEncodings);

            foreach (var kvp in newEncodings)
            {
                await _databaseService.SaveKnownFaces(kvp.Key, kvp.Value);
            }

            currentMaxPhotoNumber = await ProcessUnprocessedPhotos(knownFaces, currentMaxPhotoNumber);

            Console.WriteLine("Photo processing completed successfully.");
        }

        private async Task ProcessTrainingPhotos(Dictionary<string, List<Matrix<float>>> knownFaces, Dictionary<string, List<Matrix<float>>> newEncodings)
        {
            await foreach (var blobItem in _blobStorageService.GetBlobsAsync("TrainingPhotos/"))
            {
                string name = _blobStorageService.GetNameFromBlobItem(blobItem);

                if (knownFaces.ContainsKey(name))
                {
                    Console.WriteLine($"Face for {name} already encoded, skipping...");
                    continue;
                }

                if (!newEncodings.ContainsKey(name))
                {
                    newEncodings[name] = new List<Matrix<float>>();
                }

                using (var stream = await _blobStorageService.DownloadBlobAsync(blobItem.Name))
                {
                    List<Matrix<float>> encodings = _faceRecognitionService.EncodeFaces(stream, blobItem.Name);
                    if (encodings.Count > 0)
                    {
                        newEncodings[name].AddRange(encodings);
                        Console.WriteLine($"Encoded face for {name}");
                    }
                    else
                    {
                        Console.WriteLine($"No faces found in image {blobItem.Name}, skipping...");
                    }
                }
            }
        }

        private async Task<int> ProcessUnprocessedPhotos(Dictionary<string, List<Matrix<float>>> knownFaces, int currentMaxPhotoNumber)
        {
            await foreach (var blobItem in _blobStorageService.GetBlobsAsync("Unprocessed/"))
            {
                if (_imageService.IsUnsupportedFileType(blobItem.Name))
                {
                    Console.WriteLine($"Skipping file {blobItem.Name} as it is an unsupported file type.");
                    continue;
                }

                using (var stream = await _blobStorageService.DownloadBlobAsync(blobItem.Name))
                {
                    List<Matrix<float>> unknownEncodings = _faceRecognitionService.EncodeFaces(stream, blobItem.Name);
                    if (unknownEncodings.Count == 0)
                    {
                        Console.WriteLine($"No faces found in image {blobItem.Name}, skipping...");
                        continue;
                    }

                    HashSet<string> matchedNames = _faceRecognitionService.IdentifyFaces(unknownEncodings, knownFaces);
                    Console.WriteLine($"{blobItem.Name} contains {string.Join(" and ", matchedNames)}");

                    DateTime? dateTaken = _imageService.ExtractDateTaken(stream);
                    Console.WriteLine($"Date taken for {blobItem.Name}: {dateTaken?.ToString() ?? "Unknown"}");

                    string newBlobName = $"photo_{++currentMaxPhotoNumber}.webp";
                    string newBlobUrl = await _blobStorageService.UploadConvertedBlobAsync(stream, newBlobName);
                    var photoMetaData = new PhotoMetaData
                    {
                        PeopleIdentified = string.Join(", ", matchedNames),
                        PhotoName = newBlobName,
                        BlobUrl = newBlobUrl,
                        DateTaken = dateTaken
                    };
                    await _databaseService.SavePhotoMetaData(photoMetaData);

                    await _blobStorageService.DeleteBlobAsync(blobItem.Name);
                    Console.WriteLine($"Moved {blobItem.Name} to Processed/{newBlobName}");
                }
            }
            return currentMaxPhotoNumber;
        }
    }
}
