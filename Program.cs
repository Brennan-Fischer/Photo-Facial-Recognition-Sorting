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
            PhotoProcessor photoProcessor = new();
            await photoProcessor.ProcessPhotosAsync();
        }
    }
}
