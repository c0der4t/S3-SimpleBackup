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
using System.Threading;
using S3_SimpleBackup.Models;
using Avalonia.Controls.Mixins;
using System.Xml;

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
        /// /// <param name="parentWindow">Reference to the window calling the function. Normally equal to 'this'</param>
        /// <returns></returns>
        public async Task<string> Test_BucketConnectionAsync(string s3Host, string s3AccessKey, SecureString s3SecureKey, string bucketToTarget, Window parentWindow)
        {
            //TODO: Add an auto cancel token to cancel after 40 seconds
            try
            {
                Output.WriteToUI($"[TEST] Testing Connection to Bucket: {bucketToTarget}", parentWindow);

                Output.WriteToUI($"[TEST] Spawning S3Client", parentWindow);
                IAmazonS3 _s3Client = GenerateS3Client(s3Host, s3AccessKey, s3SecureKey);

                var request = new ListObjectsV2Request
                {
                    BucketName = bucketToTarget,
                    MaxKeys = 1,
                };

                var response = new ListObjectsV2Response();

                Output.WriteToUI($"[TEST] Connecting to server...", parentWindow);
                response = await _s3Client.ListObjectsV2Async(request);
                Output.WriteToUI($"[TEST] Connection Succeeded", parentWindow);
                return "Connection Succeeded";


            }
            catch (AmazonS3Exception ex)
            {
                Debug.WriteLine($"Error encountered on server:'{ex.Message}'");
                Output.WriteToUI($"[TEST] Error encountered on server:'{ex.Message}'", parentWindow);
                return $"Error encountered: \n'{ex.Message}'";
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
                    MaxKeys = 1000,
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

                Output.WriteToUI($"[TEST] Uploaded file: {itemToUploadPath}", parentWindow);

                return true;

            }
            catch (AmazonS3Exception e)
            {
                Output.WriteToUI($"[TEST] Error during upload: {e.Message}", parentWindow);
                Output.WriteToUI($"[TEST] Please check S3 setup and try again: {e.Message}", parentWindow);
                return false;
            }
        }

        public async Task<bool> UploadToS3(string s3Host, string s3AccessKey, SecureString s3SecureKey, string SourcePath, string s3BucketName, string JobName, bool RecursiveSync, Window parentWindow)
        {
            try
            {
                Output.WriteToUI($"Starting Job {JobName}", parentWindow);

                Output.WriteToUI($"Indexing Objects in {SourcePath}", parentWindow);

                //Index all files and folders in the source directory
                SharedMethods.FileInteraction fileActor = new SharedMethods.FileInteraction();
                List<FileInformation> objectsToUpload = await fileActor.FileIndexFromPath(SourcePath, false, RecursiveSync, parentWindow);
                Output.WriteToUI($"Discovered {objectsToUpload.Count} objects in {SourcePath}", parentWindow);


                // Create an S3 client
                var s3Client = GenerateS3Client(s3Host, s3AccessKey, s3SecureKey);

                int SuccessCount = 0;
                int FailedCount = 0;

                Output.WriteToUI($"Uploading {objectsToUpload.Count} objects to bucket {s3BucketName}", parentWindow);

                foreach (var singleObject in objectsToUpload)
                {
                    // Create a PutObjectRequest to upload the file / directory

                    PutObjectRequest request;

                    if (singleObject.isDirectory)
                    {
                        request = new PutObjectRequest
                        {
                            BucketName = s3BucketName,
                            Key = singleObject.ToS3Path(singleObject.FQPath) + "/",
                            ContentBody = ""
                        };

                        try
                        {
                            await s3Client.PutObjectAsync(request);
                            Output.WriteToUI($"Uploaded {singleObject.FQPath}", parentWindow);
                            SuccessCount++;
                        }
                        catch (Exception putException)
                        {
                            Output.WriteToUI($"Upload Failed {singleObject.FQPath}" +
                                $" ({putException.Message})", parentWindow);
                            FailedCount++;
                        }

                    }
                    else
                    {
                       // * await ValidateObjectHash(s3Client, s3BucketName, singleObject.ToS3Path(singleObject.FQPath), singleObject.FileHash);

                        using (var objectDataStream = new FileStream(singleObject.FQPath, FileMode.Open, FileAccess.Read))
                        {
                            Debug.WriteLine($"{singleObject.ObjectName} - {singleObject.FileHash}");
                            request = new PutObjectRequest
                            {
                                BucketName = s3BucketName,
                                Key = singleObject.ToS3Path(singleObject.FQPath),
                                InputStream = objectDataStream
                            };

                            request.Metadata.Add("FileHash", singleObject.FileHash);

                            try
                            {
                                await s3Client.PutObjectAsync(request);

                                string SizeTag = (singleObject.FileSize / 1024f) / 1024f > 1 ? $"{Math.Floor((singleObject.FileSize / 1024f) / 1024f)}MB" : $"{singleObject.FileSize} bytes";
                                Output.WriteToUI($"Uploaded [{SizeTag}] {singleObject.FQPath}", parentWindow);
                                SuccessCount++;
                            }
                            catch (Exception putException)
                            {
                                Output.WriteToUI($"Upload Failed {singleObject.FQPath}" +
                                    $" ({putException.Message})", parentWindow);
                                FailedCount++;
                            }
                        }
                    }

                };
                Output.WriteToUI($"Finished Job {JobName}", parentWindow);
                Output.WriteToUI($"Job Summary - Success [{SuccessCount}] | Failed [{FailedCount}]", parentWindow);
                return true;
            }
            catch (Exception e)
            {
                Output.WriteToUI($"An error occured with the S3 connection:\n{e.Message}", parentWindow);
                return false;
            }


        }


        public async Task<bool> ValidateObjectHash(IAmazonS3 s3Client, string s3BucketName, string keyToObject, string hashToValidateAgainst)
        {
            try
            {
                //Define a new GetObjectRequest object
                GetObjectRequest getRequest;

                //Use the IAmazonS3 object to initialize a new amazon client
                var _s3Client = s3Client;

                //Build out our request
                getRequest = new GetObjectRequest
                {
                    BucketName = s3BucketName,
                    Key = keyToObject
                };

                GetObjectResponse response = await _s3Client.GetObjectAsync(getRequest);

                //response.Metadata.Keys.
                Debug.WriteLine(response.Metadata.Keys);

                
                foreach (var item in response.Metadata.Keys)
                {
                    Debug.WriteLine($"{item}");
                }

                

                return true;

            }
            catch (Amazon.S3.AmazonS3Exception e)
            {
                if (!e.Message.Contains("key does not exist"))
                {
                    throw new Exception("Error while retrieving file hash for remote object: " + e.Message);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error while retrieving file hash for remote object: " + e.Message);
            }

        }

        public async void DeleteAllObjectsinBucket(string s3Host, string s3AccessKey, SecureString s3SecureKey, string bucketToTarget, Window parentWindow)
        {
            try
            {
                Output.WriteToUI($"Deleting all objects in bucket {bucketToTarget}", parentWindow);

                // Create an S3 client
                var config = new AmazonS3Config { ServiceURL = s3Host };
                var s3Credentials = new BasicAWSCredentials(s3AccessKey, UnProtect.ConvertToInsecureString(s3SecureKey));

                using (AmazonS3Client s3Client = new AmazonS3Client(s3Credentials, config))
                {
                    var ListRequest = new ListObjectsV2Request
                    {
                        BucketName = bucketToTarget
                    };

                    ListObjectsV2Response ListofObjects;

                    do
                    {
                        ListofObjects = await s3Client.ListObjectsV2Async(ListRequest);

                        foreach (S3Object obj in ListofObjects.S3Objects)
                        {
                            Output.WriteToUI($"Deleting Object {obj.Key}", parentWindow);
                            await s3Client.DeleteObjectAsync(bucketToTarget, obj.Key);
                        }

                        ListRequest.ContinuationToken = ListofObjects.NextContinuationToken;

                    } while (ListofObjects.IsTruncated);
                }

                Output.WriteToUI($"Successfully deleted all contents from bucket {bucketToTarget}", parentWindow);

            }
            catch (Exception deleteException)
            {
                Output.WriteToUI($"An error occurred\n:{deleteException}\n\nPlease try again", parentWindow);
            }

        }

    }

}
