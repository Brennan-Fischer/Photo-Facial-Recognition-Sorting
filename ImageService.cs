using ImageMagick;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    public class ImageService
    {
        public static string ConvertHeicToPng(string heicFilePath)
        {
            string pngFilePath = Path.GetTempFileName();
            using (var image = new MagickImage(heicFilePath))
            {
                image.Write(pngFilePath, MagickFormat.Png);
            }
            return pngFilePath;
        }

        public void ConvertImageToWebP(Stream inputStream, Stream outputStream)
        {
            inputStream.Position = 0;
            using (var image = new MagickImage(inputStream))
            {
                image.Write(outputStream, MagickFormat.WebP);
            }
        }

        public DateTime? ExtractDateTaken(Stream imageStream)
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

        public bool IsUnsupportedFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".docx";
        }
    }

}
