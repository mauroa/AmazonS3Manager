using System;
using System.Collections.Concurrent;
using Windows.Bits;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Timers;

namespace AmazonS3Manager
{
	public class WindowsBitsDownloader : IDownloader
	{
		static readonly ConcurrentDictionary<string, Guid> activeJobs;

		readonly IDownloadManager downloadManager;

		public event EventHandler<Guid> Cancelled;

		public event EventHandler<Guid> Paused;

		static WindowsBitsDownloader ()
		{
			activeJobs = new ConcurrentDictionary<string, Guid> ();
		}

		public WindowsBitsDownloader (IDownloadManager downloadManager)
		{
			this.downloadManager = downloadManager;
		}

		public IObservable<decimal> DownloadAsync (string url, string destinationPath)
		{
			var jobId = default (Guid);

			activeJobs.TryGetValue (url, out jobId);

			var job = jobId == Guid.Empty ? default (IDownloadJob) : downloadManager.FindJob (jobId);

			if (job == null) {
				job = CreateNewJob (url, destinationPath);
			}

			job.Resume ();

			return Observable.Create<decimal> (observer => {
				return SubscribeDownload (observer, job);
			});
		}

		public void PauseDownload (Guid jobId)
		{
			if (!activeJobs.Any (j => j.Value == jobId)) {
				return;
			}

			var job = downloadManager.FindJob (jobId);

			job.Suspend ();
			Paused?.Invoke (this, jobId);
		}

		public void CancelDownload (Guid jobId)
		{
			var activeJob = activeJobs.FirstOrDefault (j => j.Value == jobId);

			if (activeJob.Equals (default (KeyValuePair <string, Guid>))) {
				return;
			}

			var job = downloadManager.FindJob (jobId);

			job.Cancel ();
			RemoveActiveJob (activeJob.Key);
			Cancelled?.Invoke (this, jobId);
		}

		IDownloadJob CreateNewJob (string url, string destinationPath)
		{
			var job = downloadManager.CreateJob (Guid.NewGuid ().ToString (), url, destinationPath);

			activeJobs.TryAdd (url, job.Id);

			return job;
		}

		void RemoveActiveJob (Guid jobId)
		{
			var activeJob = activeJobs.FirstOrDefault (j => j.Value == jobId);

			if (activeJob.Equals (default (KeyValuePair<string, Guid>))) {
				return;
			}

			RemoveActiveJob (activeJob.Key);
		}

		void RemoveActiveJob (string url)
		{
			var removedJobId = default (Guid);

			activeJobs.TryRemove (url, out removedJobId);
		}

		IDisposable SubscribeDownload (IObserver<decimal> observer, IDownloadJob job)
		{
			var timer = new Timer ();

			timer.Enabled = true;
			timer.Interval = 5000;
			timer.Elapsed += (sender, args) => {
				AnalyzeJob (job, observer);
			};
			timer.Start ();

			return timer;
		}

		void AnalyzeJob (IDownloadJob job, IObserver<decimal> observer)
		{
			if (job == null || job.Status == DownloadStatus.Acknowledged || job.Status == DownloadStatus.Cancelled) {
				observer.OnCompleted ();
				return;
			}

			if (job == null || job.Status == DownloadStatus.Connecting || job.Status == DownloadStatus.Queued) {
				return;
			}

			if (job.Status == DownloadStatus.Error) {
				CancelDownload (job.Id);
				observer.OnError (new Exception (job.StatusMessage));
				return;
			}

			var progress = GetDownloadProgress (job);

			observer.OnNext (progress);

			if (job.Status == DownloadStatus.Transferred) {
				job.Complete ();
				RemoveActiveJob (job.Id);
				observer.OnCompleted ();
				return;
			}

			if (progress == 100m && job.Status != DownloadStatus.Transferred) {
				CancelDownload (job.Id);
				observer.OnError (new Exception (job.StatusMessage));
			}
		}

		decimal GetDownloadProgress (IDownloadJob job)
		{
			return job.BytesTotal > 0 ? job.BytesTransferred / (decimal)job.BytesTotal * 100 : 0;
		}
	}
}
