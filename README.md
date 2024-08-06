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

- Downloads photos from Azure Blob Storage
- Detects and encodes faces using DlibDotNet
- Recognizes known faces using pre-trained models
- Stores recognized faces and metadata in a SQL database
- Converts images to WebP format
- Extracts date taken from photo metadata

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

├── FischBowl_Sorting_Script
│   ├── Configuration.cs
│   ├── PhotoProcessor.cs
│   ├── BlobStorageService.cs
│   ├── DatabaseService.cs
│   ├── FaceRecognitionService.cs
│   ├── ImageService.cs
│   ├── Models
│   │   ├── KnownFaces.cs
│   │   └── PhotoMetaData.cs
│   └── Program.cs
└── README.md

## Contributions

Contributions to the project are welcome. I am a new developer with this being my first project and I welcome feedback on improvements.

(I should also note the contibutions of Microsoft Learn and Chat GPT for their contributions to this code as I was trying to learn new methods and practices)

## License

This project is licensed under the MIT License.


