using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Schema.ScriptDom;
using Microsoft.Data.Schema.ScriptDom.Sql;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Cache;
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

			if (!TableCache.IsFolder(folder_name))
			{
				TableCache.SetCurrentFolder(this, folder_name);
			}

			return TableCache.Tables;
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
