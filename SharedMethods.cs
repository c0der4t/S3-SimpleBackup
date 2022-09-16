using Amazon.Util.Internal;
using Avalonia.Controls;
using S3_SimpleBackup.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Reflection.Metadata.BlobBuilder;

namespace SharedMethods
{
    public partial class Output
    {
        public static void WriteToUI(string LinetoWrite, Window OutputWindow, string NameofOutputComponent = "edtLogOutput")
        {
            OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text = OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text + $"\n- {LinetoWrite}";
        }


    }

    public partial class DoInputOutput
    {
        public static List<BackupJobModel> LoadJobsFiles(string ProfileStorageLocation, Window parentWindow, string ProfiletoLoad = "*")
        {
            List<BackupJobModel> _Jobs = new List<BackupJobModel>();
            _Jobs.Clear();

            if (!Directory.Exists(ProfileStorageLocation))
            {
                throw new DirectoryNotFoundException("Profile Storage Location Does not Exist. Check Your Settings");
            }
            else
            {
                foreach (var currentJobFile in Directory.GetFiles(ProfileStorageLocation))
                {
                    //Read each line
                    //Each line should be one job
                    //Send the job line to the job parser method
                    //Expect a BackupJobModel back
                    string profileName = Path.GetFileNameWithoutExtension(currentJobFile);
                    Output.WriteToUI($"Loading profile: {profileName}", parentWindow);

                    foreach (string currLine in System.IO.File.ReadLines(currentJobFile))
                    {
                        BackupJobModel processedJobItem = ParseJobItem(profileName, currLine, parentWindow);

                        if (processedJobItem != null)
                        {
                            _Jobs.Add(processedJobItem);
                        }
                    }


                }

            }

            return _Jobs;
        }

        public static BackupJobModel ParseJobItem(string ProfileName, string rawJobItem, Window parentWindow)
        {
            //Check if first char is a #
            //comments start with #
            //If comment, ignore the line (do not process)
            if (rawJobItem[0] != '#')
            {
                BackupJobModel jobItem = new BackupJobModel();
                jobItem.JobProfile = ProfileName;

                int lengthCurrLine = rawJobItem.Length;
                int colNumber = 0;

                string jobName = "";
                string sourceFileFolder = "";
                string s3Destination = "";


                while (lengthCurrLine > 0)
                {
                    colNumber += 1;
                    int indexOfDelimeter = rawJobItem.IndexOf(',') > -1 ? rawJobItem.IndexOf(',') : lengthCurrLine;
                    string currColValue = rawJobItem.Substring(0, indexOfDelimeter);

                    rawJobItem = rawJobItem.IndexOf(',') > -1 ? rawJobItem.Substring(currColValue.Length + 1) : "";
                    lengthCurrLine = rawJobItem.Length;

                    switch (colNumber)
                    {
                        case 1:
                            jobItem.JobName = currColValue;
                            break;
                        case 2:
                            jobItem.SourceFileFolder = currColValue;
                            break;
                        case 3:
                            jobItem.S3Destination = currColValue;
                            break;
                        case 4:
                            jobItem.JobParameters = currColValue;
                            break;
                        case 5:
                            jobItem.JobEnabled = currColValue.ToLower() == "true" ? true : false;
                            break;
                        default:
                            break;
                    }
                }

                Output.WriteToUI($"Loaded job: {jobName}", parentWindow);
                return jobItem;
            }
            else
            {
                return null;
            }
        }

        public static void CreateNewProfile(string ProfileStorageLocation, string ProfileName)
        {
            //Redundant on purpose: Create profile file
            if (!File.Exists(Path.Combine(ProfileStorageLocation, ProfileName)))
            {
                File.Create(Path.Combine(ProfileStorageLocation, ProfileName));

                //Add default comments to the file
                using (StreamWriter newProfileFile = new StreamWriter(Path.Combine(ProfileStorageLocation, ProfileName)))
                {
                    newProfileFile.WriteLine("#This is a Simple S3 profile file");
                    newProfileFile.WriteLine("#Fields starting with # are considered comments");
                    newProfileFile.WriteLine("#Field Format:");
                    newProfileFile.WriteLine("#JobName,SourceFileFolder,S3Destination,JobParameterString,isJobEnabled");
                }

            }

        }

        public static bool CreateNewJob(string ProfileStorageLocation, BackupJobModel jobInfo)
        {
            //Check if profile exists
            if (!Directory.Exists(ProfileStorageLocation))
            {
                Directory.CreateDirectory(ProfileStorageLocation);
            }

            CreateNewProfile(ProfileStorageLocation, jobInfo.JobProfile);

            using (StreamWriter jobFile = new StreamWriter(Path.Combine(ProfileStorageLocation, jobInfo.JobProfile), append: true))
            {
                jobFile.WriteLine($"{jobInfo.JobName},{jobInfo.SourceFileFolder},{jobInfo.S3Destination},{jobInfo.JobParameters},{(jobInfo.JobEnabled ? "true" : "false")}");
            }


            return false;
        }

    }

    public partial class FileInteraction
    {

        public static List<FileInformation> FileIndexFromPath(string pathToSourceObject, bool isDirectory = false, bool RecursiveIndex = false)
        {
            //If directory, and recurse, loop method
            //From list of objects in directory, GetFileInformation for files

            List<FileInformation> listFileInformation = new List<FileInformation>();

            if (RecursiveIndex)
            {
                // TODO: Add check for restricted file access.
               

                foreach (var subDirectory in Directory.GetDirectories(pathToSourceObject))
                {
                    FileInformation objectInfo = new FileInformation();
                    objectInfo.isDirectory = true;
                    objectInfo.FileName = subDirectory.Substring(subDirectory.LastIndexOf('\\') + 1);
                    objectInfo.FQPath = subDirectory;
                    listFileInformation.Add(objectInfo);

                    listFileInformation.AddRange(FileIndexFromPath(subDirectory, true, RecursiveIndex));
                }


                foreach (var singleFile in Directory.GetFiles(pathToSourceObject))
                {
                    listFileInformation.Add(GetFileInformation(singleFile));
                }

                return listFileInformation;

            }
            else
            {
                foreach (var subDirectory in Directory.GetDirectories(pathToSourceObject))
                {
                    FileInformation objectInfo = new FileInformation();
                    objectInfo.isDirectory = true;
                    objectInfo.FileName = subDirectory.Substring(subDirectory.LastIndexOf('\\') + 1);
                    objectInfo.FQPath = subDirectory;
                    listFileInformation.Add(objectInfo);
                }

                foreach (var singleFile in Directory.GetFiles(pathToSourceObject))
                {
                    listFileInformation.Add(GetFileInformation(singleFile));
                }

                return listFileInformation;
            }
        }

        private static FileInformation GetFileInformation(string pathToFile)
        {
            FileInformation objectInfo = new FileInformation();

            if (File.Exists(pathToFile))
            {
              
                objectInfo.isDirectory = false;
                objectInfo.FileName = pathToFile.Substring(pathToFile.LastIndexOf('\\') + 1 );
                objectInfo.FQPath = pathToFile;
            }

            return objectInfo;
        }



    }
}
