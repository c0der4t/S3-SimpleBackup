namespace S3_SimpleBackup.Models
{
    public class FileInformation
    {
        public bool isDirectory { get; set; }
        public string ObjectName { get; set; }
        public string FQPath { get; set; }
        public double FileSize { get; set; }
        public string FileHash { get; set; }

        public string ToS3Path(string NTFSPath)
        {
            return NTFSPath.Replace("\\","/");
        }

    }
}
