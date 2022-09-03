namespace S3_SimpleBackup.Models
{
    public class BackupJobModel
    {
        public string JobProfile { get; set; }
        public string JobName { get; set; }
        public string SourceFileFolder { get; set; }
        public string S3Destination { get; set; }
        public string JobParameters { get; set; }
        public bool JobEnabled { get; set; }
        public bool JobRunning { get; set; }


    }
}
