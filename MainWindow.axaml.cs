using Avalonia.Controls;
using Avalonia.Interactivity;
using MsgBox;
using System.IO;
using SharedMethods;
using System.Collections.Generic;
using S3_SimpleBackup.Models;
using System.Collections.ObjectModel;
using static System.Reflection.Metadata.BlobBuilder;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using DataProtection;

namespace S3_SimpleBackup
{
    public partial class MainWindow : Window
    {
        S3Methods s3Methods = new S3Methods();
        ObservableCollection<BackupJobModel> _Jobs;

        public MainWindow()
        {
            InitializeComponent();
            LoadJobConfiguration();

            tabDev.IsVisible = AppConfig.ShowDevWindow;

            if (DoInputOutput.LoadS3Config())
            {
                edtS3Host.Text = S3Config.Host;
                edtAccessKeyID.Text = S3Config.AccessKey;
                edtSecretAccessKey.Text = S3Config.SecretKey;
                chckbxRequireLogin.IsChecked = S3Config.EncryptSecretKey;
            }

        }

        private async void btnTestConnection_Clicked(object? sender, RoutedEventArgs args)
        {
            dev_btnTestConnection.Content = "Testing Connection...";
            string ConnectionSuccess = await s3Methods.Test_BucketConnectionAsync(edtS3Host.Text, edtAccessKeyID.Text, Protect.ConvertToSecureString(edtSecretAccessKey.Text), dev_edtBucketName.Text, this);

            dev_btnTestConnection.Content = "Test Connection";

            if (!ConnectionSuccess.Contains("Error"))
            {
                await MessageBox.Show(frmMain, $"Successfully listed objects in bucket: {dev_edtBucketName.Text}", "Connection Verified", MessageBox.MessageBoxButtons.Ok);
            }
            else
            {
                await MessageBox.Show(frmMain, ConnectionSuccess, "Connection Failed", MessageBox.MessageBoxButtons.Ok);
            }
        }

        private async void btnUploadTestFile_Clicked(object? sender, RoutedEventArgs args)
        {
            bool UploadSuccess = false;

            OpenFileDialog dlgFiletoUpload = new OpenFileDialog { Title = "Select a file to upload as a test", AllowMultiple = false };
            string[] outPathStrings = await dlgFiletoUpload.ShowAsync(frmMain);

            if (outPathStrings != null)
            {
                UploadSuccess = await s3Methods.Test_UploadTestFile(edtS3Host.Text, edtAccessKeyID.Text, Protect.ConvertToSecureString(edtSecretAccessKey.Text), dev_edtBucketName.Text, outPathStrings[0], this);

            }

            if (UploadSuccess)
            {
                await MessageBox.Show(frmMain, $"Successfully uploaded file to bucket:\n {dev_edtBucketName.Text}", "File Uploaded", MessageBox.MessageBoxButtons.Ok);
                Output.WriteToUI($"[TEST] Successfully uploaded file to bucket: {dev_edtBucketName.Text}", this);
            }
            else
            {
                await MessageBox.Show(frmMain, $"Failed to upload file to bucket:\n {dev_edtBucketName.Text}", "Upload Failed", MessageBox.MessageBoxButtons.Ok);
                Output.WriteToUI($"[TEST] Failed to upload file to bucket: {dev_edtBucketName.Text}", this);
            }



        }

        private async void btnEmptyBucket_Clicked(object? sender, RoutedEventArgs args)
        {
            var confirmDelete =  await MessageBox.Show(this, $"This will delete everything in bucket {dev_edtBucketName.Text}\nWould you like to proceed?\n\nTHIS CANNOT BE UNDONE!", "Important Warning!", MessageBox.MessageBoxButtons.YesNo);
            if (confirmDelete == MessageBox.MessageBoxResult.Yes)
            {
                Output.WriteToUI($"First confirm received to empty bucket {dev_edtBucketName.Text}", this);

                var secondConfirmDelete = await MessageBox.Show(this, $"Please confirm again that you'd like to empty bucket:\n{dev_edtBucketName.Text}\n\nThis is the final warning!", "Final Warning!", MessageBox.MessageBoxButtons.YesNo);
                if (secondConfirmDelete == MessageBox.MessageBoxResult.Yes)
                {
                    Output.WriteToUI($"Second confirm received to empty bucket {dev_edtBucketName.Text}", this);
                    
                   s3Methods.DeleteAllObjectsinBucket(edtS3Host.Text, edtAccessKeyID.Text, Protect.ConvertToSecureString(edtSecretAccessKey.Text), dev_edtBucketName.Text,this);

                }
            }
        }

        private async void dev_btnTestMsgBox_Clicked(object? sender, RoutedEventArgs args)
        {
            await MessageBox.Show(this, "This is a custom messagebox test\nThis is a second line", "Test MessageBox", MessageBox.MessageBoxButtons.Ok);

        }

        private async void dev_btnTestEncrytion_Clicked(object? sender, RoutedEventArgs args)
        {
            string unEncryptedString = "This is a test string";
            await MessageBox.Show(this, $"String to encrypt \n{unEncryptedString}", "", MessageBox.MessageBoxButtons.Ok);
            await MessageBox.Show(this, $"Encrypted String \n{UnProtect.ConvertToInsecureString(Protect.EncryptString(unEncryptedString))}", "", MessageBox.MessageBoxButtons.Ok);

        }

        private void chckbxEnableEmptyBucketTool_CheckChanged(object sender, RoutedEventArgs e)
        {
            edtEmptyBucketTarget.IsEnabled = chckbxEnableEmptyBucketTool.IsChecked ?? false;
            btnEmptyBucket.IsEnabled = chckbxEnableEmptyBucketTool.IsChecked ?? false;
        }

        private async void dev_btnTestSourceListing_Clicked(object? sender, RoutedEventArgs args)
        {

            OpenFolderDialog folderDialog = new OpenFolderDialog();
            string selectedPath = await folderDialog.ShowAsync(this);

            List<FileInformation> sourceFileInfo = new List<FileInformation>();
            sourceFileInfo = FileInteraction.FileIndexFromPath(selectedPath, true, true);

            foreach (var item in sourceFileInfo)
            {
                Debug.WriteLine($"{item.ObjectName}, {item.FQPath}, {item.isDirectory}");
            }


        }

        private async void btnNewjob_Clicked(object? sender, RoutedEventArgs args)
        {

            List<string> profiles = new List<string>();
            foreach (var job in _Jobs)
            {
                if (!profiles.Contains(job.JobProfile))
                {
                    profiles.Add(job.JobProfile);
                }
            }
            
            JobManager jobManagerWindow = new JobManager("new", null, profiles);
            await jobManagerWindow.ShowDialog(this);

            _Jobs.Clear();
            dbgJobsList.Items = null;
            LoadJobConfiguration();
        }

        private async void btnEditjob_Clicked(object? sender, RoutedEventArgs args)
        {
            if (dbgJobsList.SelectedIndex > -1)
            {
                List<string> profiles = new List<string>();
                foreach (var job in _Jobs)
                {
                    if (!profiles.Contains(job.JobProfile))
                    {
                        profiles.Add(job.JobProfile);
                    }
                }
                JobManager jobManagerWindow = new JobManager("edit", _Jobs[dbgJobsList.SelectedIndex], profiles);
                await jobManagerWindow.ShowDialog(this);

                _Jobs.Clear();
                dbgJobsList.Items = null;
                LoadJobConfiguration();
            }
        }

        private async void btnDeleteJob_Clicked(object? sender, RoutedEventArgs args)
        {
            BackupJobModel selectedJob = dbgJobsList.SelectedItem as BackupJobModel;
            var confirmDelete = await MessageBox.Show(this, $"This will delete job {selectedJob.JobName}\nWould you like to proceed?\n\nTHIS CANNOT BE UNDONE!", "Important Warning!", MessageBox.MessageBoxButtons.YesNo);

            if (confirmDelete == MessageBox.MessageBoxResult.Yes)
            {
                await DoInputOutput.DeleteJobAsync(_Jobs[dbgJobsList.SelectedIndex], this);

                _Jobs.Clear();
                dbgJobsList.Items = null;
                LoadJobConfiguration();

            }
            
        }


        private async void btnRunjob_Clicked(object? sender, RoutedEventArgs args)
        {
            BackupJobModel Src = dbgJobsList.SelectedItem as BackupJobModel;
            s3Methods.UploadToS3(edtS3Host.Text, edtAccessKeyID.Text, Protect.ConvertToSecureString(edtSecretAccessKey.Text), Src.SourceFileFolder, Src.S3BucketName,Src.JobName,Src.JobParameters.Contains("/r"), this);
       
        }

        private async void btnRunAllJob_Clicked(object? sender, RoutedEventArgs args)
        {
            foreach (var item in dbgJobsList.Items)
            {
                BackupJobModel Src = item as BackupJobModel;
                s3Methods.UploadToS3(edtS3Host.Text, edtAccessKeyID.Text, Protect.ConvertToSecureString(edtSecretAccessKey.Text), Src.SourceFileFolder, Src.S3BucketName, Src.JobName, Src.JobParameters.Contains("/r"), this);
            }
        }

        private void LoadJobConfiguration()
        {
            #region Load Settings

            DoInputOutput.LoadSettings();
            Debug.WriteLine(AppConfig.ProfileStorageLocation);

            #endregion


            #region Load Jobs
            

            Output.WriteToUI($"Current Profile Location: {AppConfig.ProfileStorageLocation}", this);
            try
            {
                _Jobs = new ObservableCollection<BackupJobModel>(DoInputOutput.LoadJobsFiles(this));
            }
            catch (System.Exception e)
            {
                Output.WriteToUI($"An error occurred while loading profiles: {e.ToString()}", this);
            }

            dbgJobsList.AutoGenerateColumns = true;
            dbgJobsList.Items = _Jobs;

            #endregion


        }

    }
}
