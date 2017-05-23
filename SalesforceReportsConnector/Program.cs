using System;

namespace SalesforceReportsConnector
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args != null && args.Length >= 2)
			{
				new QvEventLogServer().Run(args[0], args[1]);
			}
		}
	}
}
