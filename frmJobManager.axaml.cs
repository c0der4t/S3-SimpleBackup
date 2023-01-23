using Avalonia.Controls;
using Avalonia.Interactivity;
using S3_SimpleBackup.Models;
using SharedMethods;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace S3_SimpleBackup
{
    public partial class JobManager : Window
    {
        private bool InEditMode { get; set; }
        private string OldJobName;
        private BackupJobModel JobInfo { get; set; }

        public JobManager()
        {
            InitializeComponent();
        }

        public JobManager(string formMode, BackupJobModel jobInfo, List<string> AvailableProfiles) : base()
        {
            InitializeComponent();

            AvailableProfiles.Add("Create New Profile");

            switch (formMode)
            {
                case "edit":
                    InEditMode = true;
                    cmbxProfileSelector.Items = AvailableProfiles;
                    cmbxProfileSelector.SelectedItem = jobInfo.JobProfile;
                    OldJobName = jobInfo.JobName;
                    edtJobName.Text = jobInfo.JobName;
                    edtSource.Text = jobInfo.SourceFileFolder;
                    edtDestination.Text = jobInfo.S3Destination;
                    edtS3Bucket.Text = jobInfo.S3BucketName;
                    chckbxRunwithProfile.IsChecked = jobInfo.RunWithProfile;
                    rbtnSyncUp.IsChecked = jobInfo.JobParameters.Contains("/su") ? true : false;
                    chckbxRecursiveSync.IsChecked = jobInfo.JobParameters.Contains("/r") ? true : false;

                    break;
                case "new":
                    InEditMode = false;
                    cmbxProfileSelector.Items = AvailableProfiles;
                    edtJobName.Text = "";
                    edtSource.Text = "";
                    edtDestination.Text = "";
                    chckbxRunwithProfile.IsChecked = false;
                    rbtnSyncUp.IsChecked = true;
                    break;
                default:
                    break;
            }

        }

        

        private async void btnSaveJob_ClickedAsync(object? sender, RoutedEventArgs args)
        {

            BackupJobModel jobInfo = new BackupJobModel();

            if (cmbxProfileSelector.SelectedItem.ToString() != "Create New Profile")
            {
                jobInfo.JobProfile = cmbxProfileSelector.SelectedItem.ToString();
            }
            else
            {
                jobInfo.JobProfile = edtNewProfileName.Text;
            }

            jobInfo.JobName = edtJobName.Text;
            jobInfo.SourceFileFolder = edtSource.Text;
            jobInfo.S3Destination = edtDestination.Text;
            jobInfo.S3BucketName = edtS3Bucket.Text;
            jobInfo.JobParameters = rbtnSyncUp.IsChecked ?? false ? "/su" : "";
            jobInfo.JobParameters = jobInfo.JobParameters + (chckbxRecursiveSync.IsChecked ?? false ? " /r" : "");
            jobInfo.RunWithProfile = chckbxRunwithProfile.IsChecked ?? false;



            if (InEditMode)
            {
                //We are editing a job, so we will update it
                //Not in Edit mode so we will create a new job
                if (await DoInputOutput.UpdateJobAsync(OldJobName, jobInfo, this))
                {
                    this.Close();
                }
            }
            else
            {
                //Not in Edit mode so we will create a new job
                if (await DoInputOutput.CreateNewJobAsync(jobInfo, this))
                {
                    this.Close();
                }
            }

        }

        private void btnCancel_Clicked(object? sender, RoutedEventArgs args)
        {
            this.Close();
        }

        private void cmbxProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbxProfileSelector.SelectedItem == "Create New Profile")
            {
                edtNewProfileName.IsVisible = true;
            }
            else
            {
                edtNewProfileName.IsVisible = false;
            }
        }
    }
}
