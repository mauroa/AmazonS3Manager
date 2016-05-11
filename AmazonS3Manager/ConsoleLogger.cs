using System;

namespace AmazonS3Manager
{
	public class ConsoleLogger : ILogger
	{
		public void LogInfo (string message, params object[] args)
		{
			Console.WriteLine (message, args);
		}

		public void LogError (string message, params object[] args)
		{
			Console.Error.WriteLine (message, args);
		}
	}
}
