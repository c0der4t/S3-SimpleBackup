namespace S3_SimpleBackup.Models
{
    public static class S3Config
    {
        public static string Host { get; set; }
        public static string AccessKey { get; set; }
        public static string SecretKey { get; set; }

        public static bool EncryptSecretKey { get; set; }

    }
}
