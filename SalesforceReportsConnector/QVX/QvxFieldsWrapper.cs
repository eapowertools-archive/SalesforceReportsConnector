using QlikView.Qvx.QvxLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SalesforceReportsConnector.QVX
{
	public enum SalesforceDataType
	{
		String,
		Integer,
		Double,
		Boolean,
		Percent,
		DateTime,
		Currency
	}

	public class QvxFieldsWrapper
	{
		public QvxField[] Fields { get; private set; }
		public SalesforceDataType[] FieldTypes { get; private set; }

		public QvxFieldsWrapper(int numOfFields)
		{
			this.Fields = new QvxField[numOfFields];
			this.FieldTypes = new SalesforceDataType[numOfFields];
		}

		public void SetFieldValue(int index, QvxField field, SalesforceDataType attribType)
		{
			Fields[index] = field;
			FieldTypes[index] = attribType;
		}

		public SalesforceDataType GetFieldType(int index)
		{
			return FieldTypes[index];
		}

		public QvxField GetField(int index)
		{
			return Fields[index];
		}

		public int GetLength()
		{
			return this.Fields.Length;
		}
	}
}
