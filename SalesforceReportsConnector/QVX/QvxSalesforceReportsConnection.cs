using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

			tables.AddRange(tableDictionary.Select(table =>
			{
				QvxField[] fields = GetFields(table.Key);
				QvxTable.GetRowsHandler handler = () => { return GetData(fields, table.Key); };
				return new QvxTable()
				{
					TableName = table.Value, Fields = fields, GetRows = handler
				};
			}));

			return tables;
		}

		private QvxField[] GetFields(string tableID)
		{
			return new QvxField[]
			{
				new QvxField("Title" + tableID, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII),
				new QvxField("Message" + tableID, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII)
			};
		}

		private IEnumerable<QvxDataRow> GetData(QvxField[] fields, string tableKey)
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

		public override QvxDataTable ExtractQuery(string query, List<QvxTable> qvxTables)
		{
			QvxDataTable returnTable = null;

			IList<ParseError> errors = null;
			var parser = new TSql100Parser(true);
			TextReader reader = new StringReader(query);
			TSqlScript script = parser.Parse(reader, out errors) as TSqlScript;

			IList<TSqlParserToken> tokens = script.Batches[0].Statements[0].ScriptTokenStream;

			// get record folder
			TSqlParserToken fromToken = tokens.First(t => t.TokenType == TSqlTokenType.From);
			int indexOfFromToken = tokens.IndexOf(fromToken);
			IEnumerable<TSqlParserToken> tableTokens = tokens.Skip(indexOfFromToken);
			TSqlParserToken identifier = tableTokens.First(t => t.TokenType == TSqlTokenType.Identifier || t.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier);
			string folderName = identifier.Text;
			if (identifier.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
			{
				folderName = folderName.Substring(1, folderName.Length - 2);
			}

			// get report name
			tableTokens = tokens.Skip(tokens.IndexOf(identifier));
			TSqlParserToken reportSeparator = tableTokens.First(t => t.TokenType == TSqlTokenType.Dot);
			tableTokens = tokens.Skip(tokens.IndexOf(reportSeparator));
			TSqlParserToken reportNameToken = tableTokens.First(t => t.TokenType == TSqlTokenType.Identifier || t.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier);
			string reportName = reportNameToken.Text;

			if (reportNameToken.TokenType == TSqlTokenType.AsciiStringOrQuotedIdentifier)
			{
				reportName = reportName.Substring(1, reportName.Length - 2);
			}

			if (this.MParameters.ContainsKey("folder_name"))
			{
				if (folderName == this.MParameters["folder_name"] && this.MTables == null)
				{
					this.Init();
				}
				else if (folderName != this.MParameters["folder_name"])
				{
					this.MParameters["folder_name"] = folderName;
					this.Init();
				}
			}
			else
			{
				this.MParameters.Add("folder_name", folderName);
				this.Init();
			}

			var newTable = this.FindTable(reportName, this.MTables);
			returnTable = new QvxDataTable(newTable);

			return returnTable;
		}
	}
}
