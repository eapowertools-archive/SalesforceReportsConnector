using System;
using System.Collections.Generic;
using System.Linq;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.SalesforceAPI;
using SalesforceReportsConnector.Logger;

namespace SalesforceReportsConnector.QVX
{
	internal class QvxSalesforceReportsServer : QvxServer
	{
		public override QvxConnection CreateConnection()
		{
			return new QvxSalesforceReportsConnection();
		}

		public override string CreateConnectionString()
		{
			QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Debug, "CreateConnectionString()");
			return "Server=localhost";
		}

		public override string HandleJsonRequest(string method, string[] userParameters, QvxConnection connection)
		{
            TempLogger.Log("logging stuff.");
			if (method.StartsWith("API-"))
			{
				return HandleAPIRequests(method, userParameters, connection);
			}
			else
			{
				return HandleRequest(method, userParameters, connection);
			}
		}

		private string HandleRequest(string method, string[] userParameters, QvxConnection connection)
		{
			QvDataContractResponse response;

			switch (method)
			{
				case "getDatabases":
					response = getDatabases(connection);
					break;
				case "getOwner":
					string username = "";
					connection.MParameters.TryGetValue("userid", out username); // Set when creating new connection or from inside the QlikView Management Console (QMC)
					response = new Info { qMessage = username };
					break;
				case "getTables":
					response = getTables(connection, userParameters[0]);
					break;
				case "getFields":
					response = getFields(connection, userParameters[0], userParameters[1]);
					break;
				default:
					response = new Info { qMessage = "Unknown command" };
					break;
			}
			return ToJson(response); // serializes response into JSON string		
		}

		private string HandleAPIRequests(string method, string[] userParameters, QvxConnection connection)
		{
			QvDataContractResponse response;

			switch (method)
			{
				case "API-getSalesforcePath":
					Uri baseUri = new Uri(userParameters[0]);
					string url = new Uri(baseUri, string.Format("services/oauth2/authorize?response_type=token&client_id={0}&redirect_uri=https%3A%2F%2Flogin.salesforce.com%2Fservices%2Foauth2%2Fsuccess", QvxSalesforceConnectionInfo.CLIENT_ID)).AbsoluteUri;
					response = new Info { qMessage = url };
					break;
				case "API-BuildConnectionString":
					string connectionString = String.Format("{0}={1};{2}={3};{4}={5};{6}={7};",
						QvxSalesforceConnectionInfo.CONNECTION_HOST,
						userParameters[0],
						QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST,
						userParameters[1],
						QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN,
						userParameters[2],
						QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN,
						userParameters[3]);
					response = new Info { qMessage = connectionString };
					break;
				case "API-getUsername":
					string username = EndpointCalls.GetUsername(connection, userParameters[0], userParameters[1], userParameters[2], Uri.UnescapeDataString(userParameters[3]), Uri.UnescapeDataString(userParameters[4]));
					response = new Info
					{
						qMessage = string.Format("{{\"username\": \"{0}\", \"host\": \"{1}\" }}", username, Uri.UnescapeDataString(userParameters[3]))
					};
					break;
				default:
					response = new Info { qMessage = "Unknown command" };
					break;
			}
			return ToJson(response);
		}

		public QvDataContractResponse getDatabases(QvxConnection connection)
		{
			IEnumerable<string> databases = EndpointCalls.GetReportFoldersList(connection);

			return new QvDataContractDatabaseListResponse
			{
				qDatabases = databases.Select(name => new Database() { qName = name }).ToArray()
			};
		}

		public QvDataContractResponse getTables(QvxConnection connection, string folderName)
		{
			if (connection.MParameters.ContainsKey("folder_name"))
			{
				connection.MParameters["folder_name"] = folderName;
			}
			else
			{
				connection.MParameters.Add("folder_name", folderName);
			}
			connection.Init();

			return new QvDataContractTableListResponse
			{
				qTables = connection.MTables
			};
		}

		public QvDataContractResponse getFields(QvxConnection connection, string folderName, string table)
		{
			if (connection.MParameters.ContainsKey("folder_name"))
			{
				connection.MParameters["folder_name"] = folderName;
			}
			else
			{
				connection.MParameters.Add("folder_name", folderName);
			}
			connection.Init();
			QvxTable currentTable = connection.FindTable(table, connection.MTables);

			return new QvDataContractFieldListResponse
			{
				qFields = (currentTable != null) ? currentTable.Fields : new QvxField[0]
			};
		}
	}
}
