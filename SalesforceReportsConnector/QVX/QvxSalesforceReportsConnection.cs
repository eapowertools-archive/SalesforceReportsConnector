using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Schema.ScriptDom;
using Microsoft.Data.Schema.ScriptDom.Sql;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;

namespace SalesforceReportsConnector.QVX
{
	internal class QvxSalesforceReportsConnection : QvxConnection
	{
		public Guid myGuid = Guid.NewGuid();

		public override void Init()
		{
//			foreach (KeyValuePair<string, string> mParameter in MParameters)
//			{
//				TempLogger.Log("param: " + mParameter.Key + "|" + mParameter.Value);
//			}
			this.MTables = GetTables();
		}

		public List<QvxTable> GetTables()
		{
			List<QvxTable> tables = new List<QvxTable>();

			string folder_name;
			try
			{
				this.MParameters.TryGetValue("folder_name", out folder_name);
			}
			catch (Exception e)
			{
				return tables;
			}

			if (string.IsNullOrEmpty(folder_name))
			{
				return tables;
			}

			IDictionary<string, string> tableDictionary = EndpointCalls.GetTableNameList(this, folder_name);

			TempLogger.Log("Dictionary has: " + tableDictionary.Count);

			tables.AddRange(tableDictionary.Select(table =>
			{
				QvxTable.GetRowsHandler handler = () => GetData(table.Value);
				return new QvxTable()
				{
					TableName = table.Value, Fields = GetFields(table.Key), GetRows = handler
				};
			}));

			return tables;
		}

		private QvxField[] GetFields(string tableID)
		{
			return new QvxField[]
			{
				new QvxField("Title", QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII),
				new QvxField("Message", QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII)
			};
		}

		private IEnumerable<QvxDataRow> GetData(string tableName)
		{
			for (int i = 0; i < 20; i++)
			{
				var row = new QvxDataRow();
				var table = FindTable(tableName, this.MTables);
				row[table.Fields[0]] = "SomeTitle " + i;
				row[table.Fields[1]] = i + " - This is my message. - " + i;
				yield return row;
			}
		}

		public override QvxDataTable ExtractQuery(string query, List<QvxTable> qvxTables)
		{
			IList<ParseError> errors = null;
			var parser = new TSql100Parser(true);
			TextReader reader = new StringReader(query);
			TSqlScript script = parser.Parse(reader, out errors) as TSqlScript;

			IList<TSqlParserToken> tokens = script.Batches[0].Statements[0].ScriptTokenStream;
			TSqlParserToken fromToken = tokens.First(t => t.TokenType == TSqlTokenType.From);
			int indexOfFromToken = tokens.IndexOf(fromToken);
			IEnumerable<TSqlParserToken> tableTokens = tokens.Skip(indexOfFromToken);
			var identifier = tableTokens.First(t => t.TokenType == TSqlTokenType.Identifier || t.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier);
			string folderName = identifier.Text;
			if (identifier.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
			{
				folderName = folderName.Substring(1, folderName.Length - 2);
			}
			TempLogger.Log("table name: " + folderName);

			this.MParameters.Add("folder_name", folderName);
			this.Init();


			/* Make sure to remove your quotesuffix, quoteprefix, 
             * quotesuffixfordoublequotes, quoteprefixfordoublequotes
             * as defined in selectdialog.js somewhere around here.
             * 
             * In this example it is an escaped double quote that is
             * the quoteprefix/suffix
             */

			//Entity

			TempLogger.Log("------------------\ndone with query");

			query = Regex.Replace(query, "\\\"", "");

			return base.ExtractQuery(query, this.MTables);
		}
	}
}
