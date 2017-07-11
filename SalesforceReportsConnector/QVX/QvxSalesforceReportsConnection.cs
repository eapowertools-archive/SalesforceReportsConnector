using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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

			List<QvxTable> newTables = BuildTablesAsync(this, tableDictionary);
			//List<QvxTable> newTables = BuildTablesSync(this, tableDictionary);

			return newTables;
		}

		private static List<QvxTable> BuildTablesSync(QvxConnection connection, IDictionary<string, string> tableDictionary)
		{
			List<QvxTable> newTables = new List<QvxTable>(tableDictionary.Select(table =>
			{
				QvxField[] fields = GetFields(connection, table.Key);
				if (fields.Length == 0)
				{
					return new QvxTable()
					{
						TableName = "---invalid---",
						Fields = fields,
						GetRows = null
					};
				}
				QvxTable.GetRowsHandler handler = () => { return GetData(connection, fields, table.Key); };
				return new QvxTable()
				{
					TableName = table.Value,
					Fields = fields,
					GetRows = handler
				};
			}).Where(t => t.TableName != "---invalid---" && t.Fields.Length != 0)
			);

			return newTables;
		}

		private static List<QvxTable> BuildTablesAsync(QvxConnection connection, IDictionary<string, string> tableDictionary)
		{
			int index = 0;
			int concurrentTables = 20;
			if (tableDictionary.Count < concurrentTables)
			{
				concurrentTables = tableDictionary.Count;
			}
			List<QvxTable> newTables = new List<QvxTable>();

			Task<QvxTable>[] taskArray = new Task<QvxTable>[concurrentTables];
			for (int i = 0; i < concurrentTables; i++)
			{
				string key = tableDictionary.ElementAt(index).Key;
				string value = tableDictionary.ElementAt(index).Value;
				taskArray[i] = Task<QvxTable>.Factory.StartNew(() => BuildSingleTable(connection, key, value));
				index++;
			}

			while (index < (tableDictionary.Count))
			{
				int taskIndex = Task.WaitAny(taskArray);
				newTables.Add(taskArray[taskIndex].Result);
				string key = tableDictionary.ElementAt(index).Key;
				string value = tableDictionary.ElementAt(index).Value;
				taskArray[taskIndex] = Task<QvxTable>.Factory.StartNew(() => BuildSingleTable(connection, key, value));
				index++;
			}
			
			foreach(Task<QvxTable> t in taskArray)
			{
				if (!t.IsCompleted)
				{
					t.Wait();
				}

				newTables.Add(t.Result);
			}

			return newTables.Where(t => t.TableName != "---invalid---" && t.Fields.Length != 0).ToList();
		}

		private static QvxTable BuildSingleTable(QvxConnection connection, string tableID, string tableName)
		{
			QvxField[] fields = GetFields(connection, tableID);
			if (fields.Length == 0)
			{
				return new QvxTable()
				{
					TableName = "---invalid---",
					Fields = fields,
					GetRows = null
				};
			}
			QvxTable.GetRowsHandler handler = () => { return GetData(connection, fields, tableID); };
			return new QvxTable()
			{
				TableName = tableName,
				Fields = fields,
				GetRows = handler
			};
		}

		private static QvxField[] GetFields(QvxConnection connection, string tableID)
        {
            IDictionary<string, Type> fields = EndpointCalls.GetFieldsFromReport(connection, tableID);
			if (fields == default(IDictionary<string, Type>))
			{
				return new QvxField[0];
			}

            QvxField[] qvxFields = fields.Select(f => {
                QvxField newField = null;
                if (f.Value == typeof(string))
                {
                    newField = new QvxField(f.Key, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII);
                }
				else if (f.Value == typeof(int) || f.Value == typeof(bool))
				{
					newField = new QvxField(f.Key, QvxFieldType.QVX_SIGNED_INTEGER, QvxNullRepresentation.QVX_NULL_NEVER, FieldAttrType.INTEGER);
				}
				else if (f.Value == typeof(float) || f.Value == typeof(double))
				{
					newField = new QvxField(f.Key, QvxFieldType.QVX_IEEE_REAL, QvxNullRepresentation.QVX_NULL_NEVER, FieldAttrType.REAL);
				}
				else if (f.Value == typeof(DateTime))
				{
					newField = new QvxField(f.Key, QvxFieldType.QVX_IEEE_REAL, QvxNullRepresentation.QVX_NULL_NEVER, FieldAttrType.TIMESTAMP);
				}
				else
				{
					newField = new QvxField(f.Key, QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_WITH_UNDEFINED_DATA, FieldAttrType.ASCII);
				}

                return newField;
                }).ToArray();

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
