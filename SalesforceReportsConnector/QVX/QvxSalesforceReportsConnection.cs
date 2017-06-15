using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Diagnostics;

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
			catch
			{
				return tables;
			}

			if (string.IsNullOrEmpty(folder_name))
			{
				return tables;
			}

            IDictionary<string, string> tableDictionary = EndpointCalls.GetTableNameList(this, folder_name);


            List<QvxTable> newTables = new List<QvxTable>(tableDictionary.Select(table =>
            {
                TempLogger.Log("Adding table " + table.Key);
                QvxField[] fields = GetFields(this, table.Key);
                QvxTable.GetRowsHandler handler = () => { return GetData(this, fields, table.Key); };
                return new QvxTable()
                {
                    TableName = table.Value,
                    Fields = fields,
                    GetRows = handler
                };
            })
            );

            return newTables;
		}

        private static QvxField[] GetFields(QvxConnection connection, string tableID)
        {
            TempLogger.Log("Getting fields for: " + tableID);
            IDictionary<string, Type> fields = EndpointCalls.GetFieldsFromReport(connection, tableID);
            TempLogger.Log("Got fields.");

            QvxField[] qvxFields = fields.Select(f => new QvxField(f.Key, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII)).ToArray();

            return qvxFields;
        }

        private static IEnumerable<QvxDataRow> GetData(QvxConnection connection, QvxField[] fields, string reportID)
        {

            IEnumerable<QvxDataRow> rows = EndpointCalls.GetReportData(connection, fields, reportID);

            return rows;
        }

        public override QvxDataTable ExtractQuery(string query, List<QvxTable> qvxTables)
		{
			QvxDataTable returnTable = null;

			IList<ParseError> errors = null;
			var parser = new TSql100Parser(true);
			TSqlScript script;
			using (TextReader reader = new StringReader(query))
			{
				script = parser.Parse(reader, out errors) as TSqlScript;
			}

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
