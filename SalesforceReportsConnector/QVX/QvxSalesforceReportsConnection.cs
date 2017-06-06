using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using QlikView.Qvx.QvxLibrary;
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
			List<QvxTable> tables = new List<QvxTable>();

			string host, authHost, access_token, refresh_token, folder_name;
			try
			{
				this.MParameters.TryGetValue("host", out host);
				this.MParameters.TryGetValue("authHost", out authHost);
				this.MParameters.TryGetValue("access_token", out access_token);
				this.MParameters.TryGetValue("refresh_token", out refresh_token);
				this.MParameters.TryGetValue("folder_name", out folder_name);
			}
			catch (Exception e)
			{
				return tables;
			}

			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(authHost) || string.IsNullOrEmpty(access_token) || string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(folder_name))
			{
				return tables;
			}

			IEnumerable<string> tableNames = EndpointCalls.GetTableNameList(this, folder_name);

			foreach (string tableName in tableNames)
			{
				tables.Add(item: new QvxTable()
				{
					TableName = tableName,
					Fields = GetTableFields(host, authHost, access_token, refresh_token, tableName),
					GetRows = GetApplicationEvents
				});
			}

			return tables;
		}

		private QvxField[] GetTableFields(string host, string authHost, string access_token, string refresh_token, string tableName)
		{
//			Tuple<string, IEnumerable<string>> tuple = EndpointCalls.getTableNameList(host, authHost, access_token, refresh_token, folder_name);
//			this.MParameters["access_token"] = tuple.Item1;

			return new QvxField[]
			{
				new QvxField("Category", QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII),
			};
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

			query = Regex.Replace(query, "\\\"", "");

			return base.ExtractQuery(query, qvxTables);
		}
	}
}
