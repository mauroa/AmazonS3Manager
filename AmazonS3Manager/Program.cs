using System;
using System.Configuration;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Bits;

namespace AmazonS3Manager
{
	class Program
	{
		static readonly ILogger logger = new ConsoleLogger ();

		static void Main (string[] args)
		{
			UploadFileAsync ().Wait ();

			logger.LogInfo ("Press any key to finish...");

			Console.ReadKey ();
		}

		static async Task UploadFileAsync ()
		{
			var downloadUrl = ConfigurationManager.AppSettings["DownloadUrl"];
			var destinationPath = ConfigurationManager.AppSettings["DownloadPath"];

			logger.LogInfo ("Preparing for downloading {0}...", downloadUrl);

			var downloadManager = new DownloadManager ();
			var downloader = new WindowsBitsDownloader (downloadManager);

			logger.LogInfo ("Starting download of {0}", downloadUrl);

			try {
				await downloader
					.DownloadAsync (downloadUrl, destinationPath)
					.Do (progress => {
						logger.LogInfo ("File: {0}, Downloaded: {1}%", downloadUrl, Math.Round (progress, 1));
					});

				logger.LogInfo ("Finished download of {0} on {1} successfully", downloadUrl, destinationPath);

				logger.LogInfo ("Preparing for uploading to S3...");

				var uploader = new AmazonS3Uploader ();

				logger.LogInfo ("Starting upload to S3...");

				await uploader
					.UploadAsync (destinationPath)
					.ConfigureAwait (continueOnCapturedContext: false);

				logger.LogInfo ("Finished downloadto S3 successfully");
			} catch (Exception ex) {
				logger.LogError ("An error occurred while downloading or uploading the file. Details: {0}", ex.Message);
			}
		}
	}
}
