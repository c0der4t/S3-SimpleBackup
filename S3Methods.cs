using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using SharedMethods;
using DataProtection;

namespace S3_SimpleBackup
{
    public class S3Methods
    {

        public IAmazonS3 GenerateS3Client(string s3Host, string s3AccessKey, SecureString s3SecureKey)
        {
            var config = new AmazonS3Config { ServiceURL = s3Host };
            var s3Credentials = new BasicAWSCredentials(s3AccessKey, UnProtect.ConvertToInsecureString(s3SecureKey));
            return new AmazonS3Client(s3Credentials, config);
        }

        /// <summary>
        /// Attempts to list the contents of a given bucket
        /// Once connection opens, success is assumed
        /// </summary>
        /// <param name="s3Host">An S3 host/REST URL. Must include https://</param>
        /// <param name="s3AccessKey">The access / user key for S3 host</param>
        /// <param name="s3SecureKey">The secret key for the S3 host</param>
        /// <param name="bucketToTarget">The full name of the bucket to target</param>
        /// <returns></returns>
        public async Task<bool> Test_BucketConnectionAsync(string s3Host, string s3AccessKey, SecureString s3SecureKey, string bucketToTarget)
        {
            try
            {
                IAmazonS3 _s3Client = GenerateS3Client(s3Host, s3AccessKey, s3SecureKey);

                var request = new ListObjectsV2Request
                {
                    BucketName = bucketToTarget,
                    MaxKeys = 1,
                };

                var response = new ListObjectsV2Response();

                response = await _s3Client.ListObjectsV2Async(request);
                return true;




            }
            catch (AmazonS3Exception ex)
            {
                Debug.WriteLine($"Error encountered on server. Message:'{ex.Message}' getting list of objects.");
                return false;
            }
        }


        public async Task<bool> Test_ListBucketContentsAsync(string s3Host, string s3AccessKey, SecureString s3SecureKey, string bucketToTarget, string rootFolderPath = "")
        {
            try
            {
                IAmazonS3 _s3Client = GenerateS3Client(s3Host, s3AccessKey, s3SecureKey);

                rootFolderPath = rootFolderPath == "/" ? "" : rootFolderPath;
                rootFolderPath = rootFolderPath[0] == '/' ? rootFolderPath.Substring(1) : rootFolderPath;

                var request = new ListObjectsV2Request
                {
                    BucketName = bucketToTarget,
                    MaxKeys = 100,
                    Prefix = rootFolderPath
                };

                var response = new ListObjectsV2Response();

                response = await _s3Client.ListObjectsV2Async(request);

                foreach (S3Object obj in response.S3Objects)
                {
                    if (rootFolderPath != "")
                    {
                        Debug.WriteLine("Object - " + obj.Key);
                    }
                    else
                    {
                        if ((obj.Key.Count(c => (c == '/')) == 1) && (obj.Key.Substring(obj.Key.IndexOf('/')).Length == 1))
                        {
                            Debug.WriteLine("Object - " + obj.Key.Substring(0, obj.Key.IndexOf('/')));
                        }
                    }
                        
                   
                    
                }

                return true;




            }
            catch (AmazonS3Exception ex)
            {
                Debug.WriteLine($"Error encountered on server. Message:'{ex.Message}' getting list of objects.");
                return false;
            }
        }


        /// <summary>
        /// Uploads a given file to the given bucket
        /// </summary>
        /// <param name="s3Host">An S3 host/REST URL. Must include https://</param>
        /// <param name="s3AccessKey">The access / user key for S3 host</param>
        /// <param name="s3SecureKey">The secret key for the S3 host</param>
        /// <param name="bucketToTarget">The full name of the bucket to target</param>
        /// <param name="itemToUploadPath">Full path to single file to upload</param>
        /// <returns></returns>
        public async Task<bool> Test_UploadTestFile(string s3Host, string s3AccessKey, SecureString s3SecureKey, string bucketToTarget, string itemToUploadPath, Window parentWindow)
        {
            try
            {
                Output.WriteToUI($"[TEST] Spawning S3Client", parentWindow);
                IAmazonS3 _s3Client = GenerateS3Client(s3Host, s3AccessKey, s3SecureKey);



                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketToTarget,
                    Key = System.IO.Path.GetFileName(itemToUploadPath),
                    FilePath = itemToUploadPath

                };

                Output.WriteToUI($"[TEST] Target Bucket: {bucketToTarget}", parentWindow);
                Output.WriteToUI($"[TEST] File to upload: {itemToUploadPath}", parentWindow);
                Output.WriteToUI($"[TEST] Remote file name: {System.IO.Path.GetFileName(itemToUploadPath)}", parentWindow);

                putRequest.Metadata.Add("x-amz-meta-title", "FileUploadTest");

                Output.WriteToUI($"[TEST] Uploading file: {itemToUploadPath}", parentWindow);

                await _s3Client.PutObjectAsync(putRequest);

                Output.WriteToUI($"[TEST] Uploaded File: {itemToUploadPath}", parentWindow);

                return true;

            }
            catch (AmazonS3Exception e)
            {
                Output.WriteToUI($"[TEST] Error during upload: {e.Message}", parentWindow);
                Output.WriteToUI($"[TEST] Please check S3 setup and try again: {e.Message}", parentWindow);
                return false;
            }
        }
    }

}
