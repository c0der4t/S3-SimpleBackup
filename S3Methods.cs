using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3_SimpleBackup
{
    public class S3Methods
    {

        /// <summary>
        /// Lists the objects in a S3 bucket.
        /// </summary>
        /// <param name="_s3Client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the bucket for which to list
        /// the contents.</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// copy operation.</returns>
        public async Task<bool> Test_ListBucketContentsAsync(string s3Host, string s3AccessKey, string s3SecureKey, string bucketToTarget)
        {
            try
            {
                IAmazonS3 _s3Client;
                var config = new AmazonS3Config { ServiceURL = s3Host };
                var s3Credentials = new BasicAWSCredentials(s3AccessKey, s3SecureKey);
                _s3Client = new AmazonS3Client(s3Credentials, config);

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

    }

}
