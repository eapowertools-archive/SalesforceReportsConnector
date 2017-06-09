using System;
using System.Collections.Generic;
using System.Linq;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;

namespace SalesforceReportsConnector.Cache
{
	public static class TableCache
	{
		public static string CurrentFolder { get; private set; }
		public static List<QvxTable> Tables { get; private set; }

		public static void SetCurrentFolder(QvxConnection connection, string newFolder)
		{
			CurrentFolder = newFolder;
			IDictionary<string, string> tableDictionary = EndpointCalls.GetTableNameList(connection, CurrentFolder);
			TempLogger.Log("got: " + tableDictionary.Count);
			if (Tables != null)
			{
				Tables.Clear();
			}
			TempLogger.Log("ready to add more tables");

			Tables = new List<QvxTable>(tableDictionary.Select(table =>
				{
					TempLogger.Log("Adding table " + table.Key);
					QvxField[] fields = GetFields(connection, table.Key);
					QvxTable.GetRowsHandler handler = () => { return GetData(connection, fields, table.Key); };
					return new QvxTable()
					{
						TableName = table.Value,
						Fields = fields,
						GetRows = handler
					};
				})
			);
			TempLogger.Log("Done adding tables.");
		}

		public static bool IsFolder(string folder)
		{
			if (string.IsNullOrEmpty(CurrentFolder))
			{
				return false;
			}

			return folder == CurrentFolder;
		}

		private static QvxField[] GetFields(QvxConnection connection, string tableID)
		{
			TempLogger.Log("Getting fields for: " + tableID);
			IDictionary<string, Type> fields = EndpointCalls.GetFieldsFromReport(connection, tableID);
			TempLogger.Log("Got fields.");

			QvxField[] qvxFields = fields.Select(f => new QvxField(f.Key, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII)).ToArray();

			return qvxFields;
		}

		private static IEnumerable<QvxDataRow> GetData(QvxConnection connection, QvxField[] fields, string tableKey)
		{
			for (int i = 0; i < 20; i++)
			{
				var row = new QvxDataRow();
				//var table = FindTable(tableKey, this.MTables);
				row[fields[0]] = "SomeTitle " + i;
				row[fields[1]] = i + " - This is my message. - " + i;
				yield return row;
			}
		}
	}
}
