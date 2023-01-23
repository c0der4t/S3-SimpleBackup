namespace S3_SimpleBackup.Models
{
    public class BackupJobModel
    {
        public string JobProfile { get; set; }
        public string JobName { get; set; }
        public string SourceFileFolder { get; set; }
        public string S3Destination { get; set; }
        public string S3BucketName { get; set; }
        public string JobParameters { get; set; }
        public bool RunWithProfile { get; set; }
        public bool JobRunning { get; set; }

        // newProfileFile.WriteLine("#JobName,LocalPath,S3Path,BucketName,JobParameterString,doRunWithProfile");

    }
}
