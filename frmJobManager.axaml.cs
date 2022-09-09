using Avalonia.Controls;
using S3_SimpleBackup.Models;

namespace S3_SimpleBackup
{
    public partial class JobManager : Window
    {
        private string FormMode { get; set; }
        private BackupJobModel JobInfo { get; set; }

        public JobManager()
        {
            InitializeComponent();
        }

        public JobManager(string formMode, BackupJobModel jobInfo) : base()
        {
            InitializeComponent();

            switch (formMode)
            {
                case "edit":
                    FormMode = formMode;
                    JobInfo = jobInfo;
                    edtJobName.Text = JobInfo.JobName;
                    edtSource.Text = JobInfo.SourceFileFolder;
                    edtDestination.Text = JobInfo.S3Destination;
                    chkbxJobEnabled.IsChecked = JobInfo.JobEnabled;
                    break;
                case "add":
                    JobInfo = new BackupJobModel();
                    edtJobName.Text = "";
                    edtSource.Text = "";
                    edtDestination.Text = "";
                    chkbxJobEnabled.IsChecked = false;
                    break;
                default:
                    break;
            }

        }

        public void frmJobManager_Activated(object sender, System.EventArgs e)
        {
            
        }
    }
}
