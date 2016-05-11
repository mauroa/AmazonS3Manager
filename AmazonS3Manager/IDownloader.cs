using System;

namespace AmazonS3Manager
{
	public interface IDownloader
	{
		event EventHandler<Guid> Cancelled;

		event EventHandler<Guid> Paused;

		IObservable<decimal> DownloadAsync (string url, string destinationPath);

		void PauseDownload (Guid jobId);

		void CancelDownload (Guid jobId);
	}
}
