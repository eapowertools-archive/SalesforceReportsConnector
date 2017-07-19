using QlikView.Qvx.QvxLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SalesforceReportsConnector.QVX
{
	public class QvxFieldsWrapper
	{
		public QvxField[] Fields { get; private set; }
		public FieldAttrType[] FieldTypes { get; private set; }

		public QvxFieldsWrapper(int numOfFields)
		{
			this.Fields = new QvxField[numOfFields];
			this.FieldTypes = new FieldAttrType[numOfFields];
		}

		public void SetFieldValue(int index, string fieldName, QvxFieldType fieldType, QvxNullRepresentation nullRepresentation, FieldAttrType attribType)
		{
			Fields[index] = new QvxField(fieldName, fieldType, nullRepresentation, attribType);
			FieldTypes[index] = attribType;
		}

		public FieldAttrType GetFieldType(int index)
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
