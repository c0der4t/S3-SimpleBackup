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

            //Check if dev keys exist. If so load them into th dev tab
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "devinfo.txt")))
            {
                using (StreamReader devInfo = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "devinfo.txt")))
                {
                    string currLine = "";
                    int lineCounter = 0;
                    while ((currLine = devInfo.ReadLine()) != null)
                    {
                        lineCounter += 1;

                        switch (lineCounter)
                        {
                            case 1:
                                dev_edtS3Host.Text = currLine;
                                break;
                            case 2:
                                dev_edtAccessKeyID.Text = currLine;
                                break;
                            case 3:
                                dev_edtSecretAccessKey.Text = currLine;
                                break;
                            case 4:
                                dev_edtBucketName.Text = currLine;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private async void dev_btnTestConnection_Clicked(object? sender, RoutedEventArgs args)
        {
            dev_btnTestConnection.Content = "Testing Connection...";
            bool ConnectionSuccess = await s3Methods.Test_BucketConnectionAsync(dev_edtS3Host.Text, dev_edtAccessKeyID.Text, Protect.ConvertToSecureString(dev_edtSecretAccessKey.Text), dev_edtBucketName.Text);

            dev_btnTestConnection.Content = "Test Connection";

            if (ConnectionSuccess)
            {
                await MessageBox.Show(frmMain, $"Successfully listed objects in bucket: {dev_edtBucketName.Text}", "Connection Verified", MessageBox.MessageBoxButtons.Ok);
                Output.WriteToUI($"Successfully listed objects in bucket: {dev_edtBucketName.Text}", this);
            }
            else
            {
                await MessageBox.Show(frmMain, $"Failed to list objects in bucket: {dev_edtBucketName.Text}", "Connection Failed", MessageBox.MessageBoxButtons.Ok);
                Output.WriteToUI($"Failed to list objects in bucket: {dev_edtBucketName.Text}", this);
            }
        }

        private async void dev_btnUploadTestFile_Clicked(object? sender, RoutedEventArgs args)
        {
            bool UploadSuccess = false;

            OpenFileDialog dlgFiletoUpload = new OpenFileDialog { Title = "Select a file to upload as a test", AllowMultiple = false };
            string[] outPathStrings = await dlgFiletoUpload.ShowAsync(frmMain);

            if (outPathStrings != null)
            {
                UploadSuccess = await s3Methods.Test_UploadTestFile(dev_edtS3Host.Text, dev_edtAccessKeyID.Text, Protect.ConvertToSecureString(dev_edtSecretAccessKey.Text), dev_edtBucketName.Text, outPathStrings[0], this);

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

        private async void dev_btnListRemoteRoot_Clicked(object? sender, RoutedEventArgs args)
        {
            Task<bool> result = s3Methods.Test_ListBucketContentsAsync(dev_edtS3Host.Text, dev_edtAccessKeyID.Text, Protect.ConvertToSecureString(dev_edtSecretAccessKey.Text), dev_edtBucketName.Text);
        }

        private async void dev_btnTestSourceListing_Clicked(object? sender, RoutedEventArgs args)
        {

            OpenFolderDialog folderDialog = new OpenFolderDialog();
            string selectedPath = await folderDialog.ShowAsync(this);

            List <FileInformation> sourceFileInfo = new List<FileInformation>();
            sourceFileInfo =  FileInteraction.FileIndexFromPath(selectedPath,true, true);

            foreach (var item in sourceFileInfo)
            {
                Debug.WriteLine($"{item.FileName}, {item.FQPath}, {item.isDirectory}");
            }


        }

        private void btnEditjob_Clicked(object? sender, RoutedEventArgs args)
        {
            JobManager jobManagerWindow = new JobManager("edit", _Jobs[dbgJobsList.SelectedIndex]);
            jobManagerWindow.ShowDialog(this);

        }

        private void LoadJobConfiguration()
        {
            Output.WriteToUI($"Current Profile Location: {Path.Combine(Directory.GetCurrentDirectory(), "Profiles")}", this);
            try
            {
                _Jobs = new ObservableCollection<BackupJobModel>(DoInputOutput.LoadJobsFiles(Path.Combine(Directory.GetCurrentDirectory(), "Profiles"), this));
            }
            catch (System.Exception e)
            {
                Output.WriteToUI($"An error occured while loading profiles: {e.ToString()}", this);
            }

            dbgJobsList.AutoGenerateColumns = true;
            dbgJobsList.Items = _Jobs;
        }

    }
}
