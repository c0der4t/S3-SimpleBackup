using Amazon.Util.Internal;
using Avalonia.Controls;
using MsgBox;
using S3_SimpleBackup.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            string DateTimeStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:ff");
            LinetoWrite = $"\n{DateTimeStamp}\t{LinetoWrite}";

            string CurrText = OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text == null ? "" : OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text;

            OutputWindow.FindControl<TextBox>(NameofOutputComponent).Text = StringSizeinMB(CurrText) < 1 ? CurrText + LinetoWrite : LinetoWrite;

            if (CurrText != null)
            {
                OutputWindow.FindControl<TextBox>(NameofOutputComponent).CaretIndex = CurrText.Length + 1;
            }

            CurrText = null;
            LinetoWrite = null;

        }

        public static double StringSizeinMB(string StringtoMeasure)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(StringtoMeasure);
            double sizeInMB = (double)bytes.Length / (1024 * 1024);
            return sizeInMB;

        }
    }

    public partial class DoInputOutput
    {
        public static List<BackupJobModel> LoadJobsFiles(Window parentWindow, string ProfiletoLoad = "*")
        {
            List<BackupJobModel> _Jobs = new List<BackupJobModel>();
            _Jobs.Clear();

            if (!Directory.Exists(AppConfig.ProfileStorageLocation))
            {
                throw new DirectoryNotFoundException("Profile Storage Location Does not Exist. Check Your Settings");
            }
            else
            {
                foreach (var currentProfileFile in Directory.GetFiles(AppConfig.ProfileStorageLocation))
                {
                    //Read each line
                    //Each line should be one job
                    //Send the job line to the job parser method
                    //Expect a BackupJobModel back
                    string profileName = Path.GetFileNameWithoutExtension(currentProfileFile);
                    Output.WriteToUI($"Loading profile: {profileName}", parentWindow);

                    foreach (string currLine in System.IO.File.ReadLines(currentProfileFile))
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

                string[] columns = rawJobItem.Split(',');

                jobItem.JobProfile = ProfileName;
                jobItem.JobName = columns[0];
                jobItem.SourceFileFolder = columns[1];
                jobItem.S3Destination = columns[2];
                jobItem.S3BucketName = columns[3];
                jobItem.JobParameters = columns[4];
                jobItem.RunWithProfile = columns[5].ToLower() == "true" ? true : false;

                return jobItem;
            }
            else
            {
                return null;
            }
        }

        public static void CreateProfileIfNotExists(string ProfileName)
        {
            //Redundant on purpose: Create profile file
            if (!File.Exists(Path.Combine(AppConfig.ProfileStorageLocation, ProfileName)))
            {

                //Add default comments to the file
                using (StreamWriter newProfileFile = new StreamWriter(Path.Combine(AppConfig.ProfileStorageLocation, ProfileName)))
                {
                    newProfileFile.WriteLine("#This is a Simple S3 profile file");
                    newProfileFile.WriteLine("#Fields starting with # are considered comments");
                    newProfileFile.WriteLine("#Field Format:");
                    newProfileFile.WriteLine("#JobName,LocalPath,S3Path,BucketName,JobParameterString,doRunWithProfile");
                }

            }

        }

        public static async Task<bool> CreateNewJobAsync(BackupJobModel jobInfo, Window parentWindow)
        {
            try
            {

                CreateProfileIfNotExists(jobInfo.JobProfile);

                using (StreamWriter jobFile = new StreamWriter(Path.Combine(AppConfig.ProfileStorageLocation, jobInfo.JobProfile), append: true))
                {
                    jobFile.WriteLine($"{jobInfo.JobName},{jobInfo.SourceFileFolder},{jobInfo.S3Destination},{jobInfo.S3BucketName},{jobInfo.JobParameters},{(jobInfo.RunWithProfile ? "true" : "false")}");
                }

                return true;

            }
            catch (Exception newjobEx)
            {
                await MessageBox.Show(parentWindow, $"An error while creating/updating job:\n{newjobEx.Message}", "Job Change Failed", MessageBox.MessageBoxButtons.Ok);
                return false;
            }

        }

        public static async Task<bool> UpdateJobAsync(string OldJobName, BackupJobModel jobInfo, Window parentWindow)
        {
            try
            {
                List<BackupJobModel> newJobsforProfile = new List<BackupJobModel>();
                CreateProfileIfNotExists(jobInfo.JobProfile);

                //Load the profile file, and find the job to update
                foreach (string currLine in System.IO.File.ReadLines(Path.Combine(AppConfig.ProfileStorageLocation, jobInfo.JobProfile)))
                {
                    BackupJobModel processedJobItem = ParseJobItem(jobInfo.JobProfile, currLine, parentWindow);

                    if (processedJobItem != null)
                    {
                        newJobsforProfile.Add(processedJobItem.JobName != OldJobName ? processedJobItem : jobInfo);
                    }
                }

                File.Delete(Path.Combine(AppConfig.ProfileStorageLocation, jobInfo.JobProfile));

                foreach (BackupJobModel singleJob in newJobsforProfile)
                {
                    CreateNewJobAsync(singleJob, parentWindow);
                }

                return true;
            }
            catch (Exception newjobEx)
            {
                await MessageBox.Show(parentWindow, $"An error while creating/updating job:\n{newjobEx.Message}", "Job Change Failed", MessageBox.MessageBoxButtons.Ok);
                return false;
            }


        }

        public static async Task<bool> DeleteJobAsync(BackupJobModel jobInfo, Window parentWindow)
        {
            try
            {
                List<BackupJobModel> newJobsforProfile = new List<BackupJobModel>();
                CreateProfileIfNotExists(jobInfo.JobProfile);

                //Load the profile file, and find the job to update
                foreach (string currLine in System.IO.File.ReadLines(Path.Combine(AppConfig.ProfileStorageLocation, jobInfo.JobProfile)))
                {
                    BackupJobModel processedJobItem = ParseJobItem(jobInfo.JobProfile, currLine, parentWindow);

                    if (processedJobItem != null)
                    {
                        if (processedJobItem.JobName != jobInfo.JobName)
                        {
                            newJobsforProfile.Add(processedJobItem);
                        }
                    }
                }

                File.Delete(Path.Combine(AppConfig.ProfileStorageLocation, jobInfo.JobProfile));

                foreach (BackupJobModel singleJob in newJobsforProfile)
                {
                    CreateNewJobAsync(singleJob, parentWindow);
                }

                Output.WriteToUI($"Successfully Deleted Job {jobInfo.JobName}", parentWindow);
                return true;
            }
            catch (Exception newjobEx)
            {
                await MessageBox.Show(parentWindow, $"An error while deleting job:\n{newjobEx.Message}", "Job Deletion Failed", MessageBox.MessageBoxButtons.Ok);
                Output.WriteToUI($"An error while deleting job:\n{newjobEx.Message}", parentWindow);
                return false;
            }


        }

        public static void LoadSettings()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "settings.ini")))
            {
                using (StreamReader settingsFile = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "settings.ini")))
                {
                    string currLine = "";
                    int lineCounter = 0;

                    while ((currLine = settingsFile.ReadLine()) != null)
                    {
                        lineCounter += 1;

                        switch (lineCounter)
                        {
                            case 1:
                                AppConfig.ShowDevWindow = currLine.ToLower() == "true" ? true : false;
                                break;
                            case 2:
                                AppConfig.WritetoLogFile = currLine.ToLower() == "true" ? true : false;
                                break;
                            case 3:
                                AppConfig.RequireLogin = currLine.ToLower() == "true" ? true : false;
                                break;
                            case 4:
                                AppConfig.LogFileStorageLocation = currLine.Contains("%appdir%") ? currLine.Replace("%appdir%", Directory.GetCurrentDirectory()).Replace('/', '\\') : currLine.Replace('/', '\\');
                                break;
                            case 5:
                                AppConfig.LogFilePrefix = currLine;
                                break;
                            case 6:
                                AppConfig.ProfileStorageLocation = currLine.Contains("%appdir%") ? currLine.Replace("%appdir%", Directory.GetCurrentDirectory()).Replace('/', '\\') : currLine.Replace('/', '\\');
                                break;
                            default:
                                break;
                        }
                    }
                }

            }
            else
            {
                AppConfig.ShowDevWindow = false;
                AppConfig.WritetoLogFile = false;
                AppConfig.RequireLogin = false;

                AppConfig.ProfileStorageLocation = "";
                AppConfig.ProfileStorageLocation = "";
                AppConfig.ProfileStorageLocation = "";
            }
        }

        public static bool WriteSettings()
        {
            try
            {
                using (StreamWriter settingFile = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "settings.ini")))
                {
                    settingFile.WriteLine(AppConfig.ShowDevWindow ? "true" : "false");
                    settingFile.WriteLine(AppConfig.WritetoLogFile ? "true" : "false");
                    settingFile.WriteLine(AppConfig.RequireLogin ? "true" : "false");

                    settingFile.WriteLine(AppConfig.LogFileStorageLocation);
                    settingFile.WriteLine(AppConfig.LogFilePrefix);
                    settingFile.WriteLine(AppConfig.ProfileStorageLocation);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return false;
        }

        public static bool LoadS3Config(Window parentWindow = null)
        {
            try
            {
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "s3.ini")))
                {
                    string[] S3Info = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "s3.ini"));

                    S3Config.EncryptSecretKey = S3Info[3].ToLower() == "true";

                    S3Config.Host = S3Info[0];
                    S3Config.AccessKey = S3Info[1];
                    S3Config.SecretKey = S3Info[2];


                    Output.WriteToUI("Successfully loaded S3 access configuration.", parentWindow);
                    return true;
                }
                else
                {
                    Output.WriteToUI("Unable to find s3.ini file. S3 access configuration not loaded", parentWindow);
                    return false;
                }

            }
            catch (Exception settingException)
            {
                return false;
            }

        }

        public static bool WriteS3Config(Window parentWindow = null)
        {
            try
            {
                File.Move(Path.Combine(Directory.GetCurrentDirectory(), "s3.ini"), Path.Combine(Directory.GetCurrentDirectory(), "s3.bak"));

                using (StreamWriter s3ConfigFile = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "s3.ini")))
                {
                    s3ConfigFile.WriteLine(S3Config.Host);
                    s3ConfigFile.WriteLine(S3Config.AccessKey);
                    s3ConfigFile.WriteLine(S3Config.SecretKey);
                    s3ConfigFile.WriteLine(S3Config.EncryptSecretKey ? "true" : "false");
                }

                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "s3.bak"));

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }


    public partial class FileInteraction
    {

        public static List<FileInformation> FileIndexFromPath(string pathToSourceObject, bool isDirectory = false, bool RecursiveIndex = false, Window parentWindow = null)
        {
            try
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
                        objectInfo.ObjectName = subDirectory.Substring(subDirectory.LastIndexOf('\\') + 1);
                        objectInfo.FQPath = subDirectory;
                        objectInfo.FileSize = 0;
                        listFileInformation.Add(objectInfo);

                        listFileInformation.AddRange(FileIndexFromPath(subDirectory, true, RecursiveIndex));
                    }


                    foreach (var singleFile in Directory.GetFiles(pathToSourceObject))
                    {
                        Output.WriteToUI($"Indexed {singleFile}", parentWindow);
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
                        objectInfo.ObjectName = subDirectory.Substring(subDirectory.LastIndexOf('\\') + 1);
                        objectInfo.FQPath = subDirectory;
                        listFileInformation.Add(objectInfo);
                    }

                    foreach (var singleFile in Directory.GetFiles(pathToSourceObject))
                    {
                        listFileInformation.Add(GetFileInformation(singleFile));
                    }

                    listFileInformation.Sort((x, y) => x.FileSize.CompareTo(y.FileSize));
                    return listFileInformation;
                }
            }
            catch (Exception e)
            { 
                throw new Exception($"An error during file indexing:{e.Message}");
            }
        }

        private static FileInformation GetFileInformation(string pathToFile)
        {
            FileInformation objectInfo = new FileInformation();

            if (File.Exists(pathToFile))
            {

                objectInfo.isDirectory = false;
                objectInfo.ObjectName = pathToFile.Substring(pathToFile.LastIndexOf('\\') + 1);
                objectInfo.FQPath = pathToFile;
                objectInfo.FileSize = new FileInfo(pathToFile).Length;
                objectInfo.FileHash = DataProtection.Hash.CalculateSHA256Hash_FromFilePath(pathToFile);
            }

            return objectInfo;
        }



    }
}
