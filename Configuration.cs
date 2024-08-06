using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public static class Configuration
    {
        public static readonly string ShapePredictorPath = "C:\\MSSA-CCAD14\\FischBowl Sorting Script\\shape_predictor_68_face_landmarks.dat";
        public static readonly string FaceRecognitionModelPath = "C:\\MSSA-CCAD14\\FischBowl Sorting Script\\dlib_face_recognition_resnet_model_v1.dat";
        public static readonly string PhotosContainerName = "photos";
        public static readonly string ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
        public static readonly string SqlConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
    }
}
