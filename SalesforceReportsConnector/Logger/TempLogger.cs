using System.IO;

namespace SalesforceReportsConnector.Logger
{
	public static class TempLogger
	{
		public static string filePath = @"E:\UserData\Desktop\tempLog.txt";

		static TempLogger() {}

		public static void Log(string message)
		{
			using (StreamWriter streamWriter = new StreamWriter(filePath, true))
			{
				streamWriter.WriteLine(message);

				streamWriter.Close();
			}
		}
	}
}
