using System.Collections.Generic;
using System.Text.RegularExpressions;
using QlikView.Qvx.QvxLibrary;

namespace SalesforceReportsConnector.QVX
{
	internal class QvxSalesforceReportsConnection : QvxConnection
	{
		public override void Init()
		{
			QvxLog.SetLogLevels(true, true);

			QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Notice, "Init()");

			var eventLogFields = new QvxField[]
			{
				new QvxField("Title", QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII),
				new QvxField("Message", QvxFieldType.QVX_TEXT, QvxNullRepresentation.QVX_NULL_FLAG_SUPPRESS_DATA, FieldAttrType.ASCII)
			};

			MTables = new List<QvxTable>
			{
				new QvxTable
				{
					TableName = "DummyData",
					GetRows = GetApplicationEvents,
					Fields = eventLogFields
				}
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
