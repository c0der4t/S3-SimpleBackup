using Avalonia.Controls;
using S3_SimpleBackup.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
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
                    Output.WriteToUI($"Loading profile: {profileName}",parentWindow);

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
                            jobName = currColValue;
                            break;
                        case 2:
                            sourceFileFolder = currColValue;
                            break;
                        case 3:
                            s3Destination = currColValue;
                            break;
                        default:
                            break;
                    }
                }

                jobItem.JobName = jobName;
                jobItem.SourceFileFolder = sourceFileFolder;
                jobItem.S3Destination = s3Destination;
                Output.WriteToUI($"Loaded job: {jobName}", parentWindow);
                return jobItem;
            }
            else
            {
                return null;
            }
        }


    }


}
