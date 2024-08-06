# Photo Facial Recognition Sorting Script (Previously "Fischbowl Sorting Script")

This project is a photo processing script designed to process and organize photos by detecting faces, recognizing individuals, and storing metadata. The script uses Azure Blob Storage, SQL Server, and various image processing libraries.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Setup](#setup)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

## Features


1. **Face Detection**: Utilizing advanced image processing techniques, the script detects faces within photos, allowing for the identification and cataloging of individuals present in the images.

2. **Face Recognition**: The script employs pre-trained models to recognize and distinguish between different individuals. This feature enables the automatic tagging and grouping of photos based on the identified persons.

3. **Metadata Storage**: The script captures and stores detailed metadata for each photo, including information about the detected faces and other relevant attributes. This metadata is then securely stored in a SQL Server database, providing a robust and reliable means of managing and querying photo information.

4. **Integration with Azure Blob Storage**: To handle large volumes of photos efficiently, the script integrates with Azure Blob Storage. This cloud-based storage solution offers scalable, durable, and secure storage for the photos, ensuring they are easily accessible for processing and retrieval.

5. **Image Conversion**: The script also includes functionality to convert images to the WebP format, which is known for its superior compression and quality characteristics. This conversion helps in reducing storage costs and improving load times when photos are accessed.

6. **Date Extraction**: Using metadata extraction libraries, the script can determine the date a photo was taken. This information is valuable for organizing photos chronologically and for various other time-based queries.

This app was designed to utilize these features to assist families or groups easyily organize large libraries of photos by people present in the photo. 


## Requirements

- .NET 6.0 or higher
- Azure Storage account with Blob Container
- SQL Server instance
- Pre-trained Dlib models:
  - `shape_predictor_68_face_landmarks.dat`
  - `dlib_face_recognition_resnet_model_v1.dat`
- Environment variables:
  - `AZURE_BLOB_CONNECTION_STRING`
  - `SQL_CONNECTION_STRING`

## Setup

1. **Clone the repository:**

   ```sh
   git clone https://github.com/yourusername/fischbowl-sorting-script.git
   cd fischbowl-sorting-script

2. Install the necessary packages

    dotnet restore
   
3.Set up environment variables:

Ensure you have the following environment variables set in your system:

AZURE_BLOB_CONNECTION_STRING: Your Azure Blob Storage connection string.
SQL_CONNECTION_STRING: Your SQL Server connection string.

4.Place pre-trained models:

I utilized the dlib face recognition models @ 
https://github.com/janlle/dlib-face-recognition/tree/master/data/data_dlib

Download the following models and place them in the specified directory:

shape_predictor_68_face_landmarks.dat
dlib_face_recognition_resnet_model_v1.dat
Update the paths in Configuration.cs if necessary.

## Usage

1. Run the script either in IDE or dotnet run
2. The script will start processing photos, detecting faces, recognizing individuals, and saving metadata to the SQL database.

## Project Structure

1. FischBowl_Sorting_Script
 a.Configuration.cs
 b.  PhotoProcessor.cs
 c. BlobStorageService.cs
 d. DatabaseService.cs
 e. FaceRecognitionService.cs
 f. ImageService.cs
 h. Models
   (1) KnownFaces.cs
   (2) PhotoMetaData.cs
 j. Program.cs
 2. README.md

## Contributions

Contributions to the project are welcome. I am a new developer with this being my first project and I welcome feedback on improvements.

(I should also note the contibutions of Microsoft Learn and Chat GPT for their contributions to this code as I was trying to learn new methods and practices)

## License

This project is licensed under the MIT License.


