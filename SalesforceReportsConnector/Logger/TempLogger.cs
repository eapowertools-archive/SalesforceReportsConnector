using System;
using System.Collections.Concurrent;
using System.IO;

namespace SalesforceReportsConnector.Logger
{
	public static class TempLogger
	{
		public static string filePath = @"C:\Users\qlikservice\Desktop\tempLog.txt";

		public static ConcurrentQueue<string> StringsToLog = new ConcurrentQueue<string>();


		private static Object thisLock = new Object();

		static TempLogger() {}

		public static void Log(string message)
		{
			StringsToLog.Enqueue(message);

			RunLogger();
        }

		public static void RunLogger()
		{
			lock (thisLock)
			{
				string message = "";
				StringsToLog.TryDequeue(out message);
				if (!string.IsNullOrEmpty(message))
				{
					using (StreamWriter streamWriter = new StreamWriter(filePath, true))
					{
						streamWriter.WriteLine(message);
						streamWriter.Close();
					}
				}
			}
		}
	}
}
