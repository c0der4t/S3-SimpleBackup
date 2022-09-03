using Avalonia.Controls;
using Avalonia.Interactivity;
using MsgBox;
using System.Diagnostics;
using System.IO;

namespace S3_SimpleBackup
{
    public partial class MainWindow : Window
    {
        S3Methods s3Methods = new S3Methods();

        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "devinfo.txt"));

            //Check if dev keys exist. If so load them into th dev tab
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(),"devinfo.txt")))
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
            bool ConnectionSuccess = await s3Methods.Test_ListBucketContentsAsync(dev_edtS3Host.Text, dev_edtAccessKeyID.Text, dev_edtSecretAccessKey.Text, dev_edtBucketName.Text);

            dev_btnTestConnection.Content = "Test Connection";

            if (ConnectionSuccess)
            {
                await MessageBox.Show(frmMain, $"Successfully listed objects in bucket: {dev_edtBucketName.Text}", "Connection Verified", MessageBox.MessageBoxButtons.Ok);
            }
            else
            {
                await MessageBox.Show(frmMain, $"Failed to list objects in bucket: {dev_edtBucketName.Text}", "Connection Failed", MessageBox.MessageBoxButtons.Ok);
            }
        }

        private async void dev_btnUploadTestFile_Clicked(object? sender, RoutedEventArgs args)
        {
            await MessageBox.Show(frmMain, $"Successfully listed objects in bucket: {dev_edtBucketName.Text}", "Connection Verified", MessageBox.MessageBoxButtons.Ok);

        }

        private async void dev_btnTestMsgBox_Clicked(object? sender, RoutedEventArgs args)
        {
            await MessageBox.Show(this, "This is a custom messagebox test\nThis is a second line", "Test MessageBox", MessageBox.MessageBoxButtons.Ok);

        }

    }
}
