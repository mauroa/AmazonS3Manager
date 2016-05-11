using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace AmazonS3Manager
{
	public class AmazonS3Uploader : IUploader
	{
		readonly AmazonS3Client client;

		public AmazonS3Uploader ()
		{
			var configuration = new AmazonS3Config {
				RegionEndpoint = RegionEndpoint.USEast1
			};

			client = new AmazonS3Client (configuration);
		}

		public async Task<bool> UploadAsync (string filePath)
		{
			var bucketName = ConfigurationManager.AppSettings["AWSBucket"];
			var request = new PutObjectRequest
			{
				BucketName = bucketName,
				FilePath = filePath
			};

			var response = await client
				.PutObjectAsync (request)
				.ConfigureAwait (continueOnCapturedContext: false);

			return response.HttpStatusCode == HttpStatusCode.OK;
		}
	}
}
