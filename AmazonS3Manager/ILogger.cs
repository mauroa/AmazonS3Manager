namespace AmazonS3Manager
{
	public interface ILogger
	{
		void LogInfo (string message, params object[] args);

		void LogError (string message, params object[] args);
	}
}
