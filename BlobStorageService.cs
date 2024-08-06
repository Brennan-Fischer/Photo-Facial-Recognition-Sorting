using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(string connectionString, string containerName)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async IAsyncEnumerable<BlobItem> GetBlobsAsync(string prefix)
        {
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
            {
                yield return blobItem;
            }
        }

        public string GetNameFromBlobItem(BlobItem blobItem)
        {
            string[] pathSegments = blobItem.Name.Split('/');
            return pathSegments.Length < 2 ? null : pathSegments[1];
        }

        public async Task<Stream> DownloadBlobAsync(string blobName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public async Task<int> GetCurrentMaxPhotoNumber()
        {
            int maxNumber = 0;
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: "Processed/photo_"))
            {
                string fileName = Path.GetFileNameWithoutExtension(blobItem.Name);
                if (fileName.StartsWith("photo_") && int.TryParse(fileName.Substring(6), out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }
            return maxNumber;
        }

        public async Task<string> UploadConvertedBlobAsync(Stream inputStream, string newBlobName)
        {
            BlobClient newBlobClient = _containerClient.GetBlobClient("Processed/" + newBlobName);
            using (var convertedStream = new MemoryStream())
            {
                new ImageService().ConvertImageToWebP(inputStream, convertedStream);
                convertedStream.Position = 0;
                await newBlobClient.UploadAsync(convertedStream, true);
            }
            return _containerClient.Uri + "/Processed/" + newBlobName;
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteAsync();
        }
    }

}
