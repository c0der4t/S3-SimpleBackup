namespace S3_SimpleBackup.Models
{
    public static class AppConfig
    {
        public static bool ShowDevWindow { get; set; }
        public static bool WritetoLogFile { get; set; }
        public static bool RequireLogin { get; set; }

        public static string LogFileStorageLocation { get; set; }
        public static string LogFilePrefix { get; set; }
        public static string ProfileStorageLocation { get; set; }

    }
}
