using DlibDotNet.Dnn;
using DlibDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public class FaceRecognitionService
    {
        private readonly ShapePredictor _shapePredictor;
        private readonly LossMetric _faceRecognitionModel;

        public FaceRecognitionService(string shapePredictorPath, string faceRecognitionModelPath)
        {
            _shapePredictor = ShapePredictor.Deserialize(shapePredictorPath);
            _faceRecognitionModel = LossMetric.Deserialize(faceRecognitionModelPath);
        }

        public List<Matrix<float>> EncodeFaces(Stream imageStream, string blobName)
        {
            string tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFilePath))
            {
                imageStream.CopyTo(fileStream);
            }

            Array2D<RgbPixel> img = LoadImage(tempFilePath, Path.GetExtension(blobName).ToLower());
            File.Delete(tempFilePath);

            return DetectAndEncodeFaces(img);
        }

        private Array2D<RgbPixel> LoadImage(string filePath, string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" => Dlib.LoadImage<RgbPixel>(filePath),
                ".heic" or ".heif" => Dlib.LoadImage<RgbPixel>(ImageService.ConvertHeicToPng(filePath)),
                _ => throw new NotSupportedException($"Image type {extension} is not supported."),
            };
        }

        private List<Matrix<float>> DetectAndEncodeFaces(Array2D<RgbPixel> img)
        {
            Rectangle[] faces = Dlib.GetFrontalFaceDetector().Operator(img);
            if (faces.Length == 0) return new List<Matrix<float>>();

            List<Matrix<float>> encodings = new List<Matrix<float>>();
            foreach (var face in faces)
            {
                FullObjectDetection shape = _shapePredictor.Detect(img, face);
                ChipDetails chipDetails = Dlib.GetFaceChipDetails(shape, 150, 0.25);
                Array2D<RgbPixel> faceChip = Dlib.ExtractImageChip<RgbPixel>(img, chipDetails);

                using (Matrix<RgbPixel> matrix = new Matrix<RgbPixel>(faceChip))
                {
                    OutputLabels<Matrix<float>> faceDescriptor = _faceRecognitionModel.Operator(matrix);
                    encodings.Add(faceDescriptor[0]);
                }
            }
            return encodings;
        }

        public HashSet<string> IdentifyFaces(List<Matrix<float>> unknownEncodings, Dictionary<string, List<Matrix<float>>> knownFaces)
        {
            const double Tolerance = 0.5;
            HashSet<string> matchedNames = new HashSet<string>();

            foreach (var unknownEncoding in unknownEncodings)
            {
                foreach (KeyValuePair<string, List<Matrix<float>>> kvp in knownFaces)
                {
                    foreach (var knownEncoding in kvp.Value)
                    {
                        if (ComputeEuclideanDistance(unknownEncoding, knownEncoding) < Tolerance)
                        {
                            matchedNames.Add(kvp.Key);
                        }
                    }
                }
            }

            return matchedNames;
        }

        private double ComputeEuclideanDistance(Matrix<float> encoding1, Matrix<float> encoding2)
        {
            double sum = 0.0;
            for (int i = 0; i < encoding1.Size; i++)
            {
                double diff = encoding1[i] - encoding2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        public static byte[] SerializeEncodings(List<Matrix<float>> encodings)
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(encodings.Count);
                    foreach (var encoding in encodings)
                    {
                        for (int i = 0; i < encoding.Size; i++)
                        {
                            bw.Write(encoding[i]);
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        public static List<Matrix<float>> DeserializeEncodings(byte[] data)
        {
            List<Matrix<float>> encodings = new List<Matrix<float>>();
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    int count = br.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        float[] encodingArray = new float[128]; // Adjust this size if necessary
                        for (int i = 0; i < encodingArray.Length; i++)
                        {
                            encodingArray[i] = br.ReadSingle();
                        }
                        Matrix<float> encoding = new Matrix<float>(1, 128);
                        for (int i = 0; i < 128; i++)
                        {
                            encoding[0, i] = encodingArray[i];
                        }
                        encodings.Add(encoding);
                    }
                }
            }
            return encodings;
        }

        public void Dispose()
        {
            _shapePredictor.Dispose();
            _faceRecognitionModel.Dispose();
        }
    }

}
