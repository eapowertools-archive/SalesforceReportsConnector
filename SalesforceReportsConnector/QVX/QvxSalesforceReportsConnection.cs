using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;

namespace SalesforceReportsConnector.QVX
{
	internal class QvxSalesforceReportsConnection : QvxConnection
	{
		public override void Init()
		{
			MTables = GetTables();
		}

		private List<QvxTable> GetTables()
		{
			TempLogger.Log("i am creating tables.");


			List<QvxTable> tables = new List<QvxTable>();

			string host, authHost, access_token, refresh_token;
			try
			{
				this.MParameters.TryGetValue("host", out host);
				this.MParameters.TryGetValue("authHost", out authHost);
				this.MParameters.TryGetValue("access_token", out access_token);
				this.MParameters.TryGetValue("refresh_token", out refresh_token);
			}
			catch (Exception e)
			{
				TempLogger.Log(e.Message);
				return tables;
			}

			TempLogger.Log("past the exceptions");


			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(authHost) || string.IsNullOrEmpty(access_token) || string.IsNullOrEmpty(refresh_token))
			{
				return tables;
			}

			TempLogger.Log("i got here");

			Tuple<string, IList<string>> tuple = EndpointCalls.getTableNameList(host, authHost, access_token, refresh_token);
			this.MParameters["access_token"] = tuple.Item1;

			foreach (string tableName in tuple.Item2)
			{
				tables.Add(new QvxTable()
				{
					TableName = tableName,
					Fields = new QvxField[] { },
					GetRows = GetApplicationEvents
				});
			}

			return tables;
		}

		private IEnumerable<QvxDataRow> GetApplicationEvents()
		{
			QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Notice, "GetApplicationEvents()");

			for (int i = 0; i < 20; i++)
			{
				var row = new QvxDataRow();
				var table = FindTable("DummyData", MTables);
				row[table.Fields[0]] = "SomeTitle " + i;
				row[table.Fields[1]] = i + " - This is my message. - " + i;
				yield return row;
			}
		}

		public override QvxDataTable ExtractQuery(string query, List<QvxTable> qvxTables)
		{
			/* Make sure to remove your quotesuffix, quoteprefix, 
             * quotesuffixfordoublequotes, quoteprefixfordoublequotes
             * as defined in selectdialog.js somewhere around here.
             * 
             * In this example it is an escaped double quote that is
             * the quoteprefix/suffix
             */
			TempLogger.Log("Extract Query");
			TempLogger.Log(query);

			query = Regex.Replace(query, "\\\"", "");

			return base.ExtractQuery(query, qvxTables);
		}
	}
}
