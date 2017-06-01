using System;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.Logger;
using SalesforceReportsConnector.SalesforceAPI;

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
			if (method.StartsWith("API-"))
			{
				TempLogger.Log("I'm handling an API request" + method);

				return HandleAPIRequests(method, userParameters, connection);
			}
			else
			{
				TempLogger.Log("I'm handling another request");

				return HandleRequest(method, userParameters, connection);
			}
		}

		private string HandleRequest(string method, string[] userParameters, QvxConnection connection)
		{

			QvDataContractResponse response;

			string provider, host, authHost, username, access_token, refresh_token;
			connection.MParameters.TryGetValue("provider", out provider); // Set to the name of the connector by QlikView Engine
			connection.MParameters.TryGetValue("userid", out username); // Set when creating new connection or from inside the QlikView Management Console (QMC)
			connection.MParameters.TryGetValue("host", out host);
			connection.MParameters.TryGetValue("authHost", out authHost);
			connection.MParameters.TryGetValue("access_token", out access_token);
			connection.MParameters.TryGetValue("refresh_token", out refresh_token);

			switch (method)
			{
				case "getDatabases":
					response = getDatabases();
					break;
				case "getOwner":
					response = new Info { qMessage = username };
					break;
				case "getTables":
					response = getTables(connection);
					break;
				case "getFields":
					response = getFields(connection, userParameters[0]);
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
					string url = new Uri(baseUri, string.Format("services/oauth2/authorize?response_type=token&client_id={0}&redirect_uri=https%3A%2F%2Flogin.salesforce.com%2Fservices%2Foauth2%2Fsuccess", EndpointCalls.CLIENT_ID)).AbsoluteUri;
					response = new Info { qMessage = url };
					break;
				case "API-getUsername":
					TempLogger.Log("about to make the request");
					Tuple<string, string> tuple = EndpointCalls.getUsername(userParameters[0], userParameters[1], userParameters[2], Uri.UnescapeDataString(userParameters[3]), userParameters[4]);
					TempLogger.Log("going to set a tuple");

					connection.MParameters["access_token"] = tuple.Item1;
					TempLogger.Log("set the tuple!");

					response = new Info
					{
						qMessage = string.Format("{{\"username\": \"{0}\", \"host\": \"{1}\" }}", tuple.Item2, Uri.UnescapeDataString(userParameters[3]))
					};
					break;
				default:
					response = new Info { qMessage = "Unknown command" };
					break;
			}
			return ToJson(response);
		}

		public QvDataContractResponse getDatabases()
		{
			return new QvDataContractDatabaseListResponse
			{
				qDatabases = new Database[]
				{
					new Database { qName = "Salesforce Reports" }
				}
			};
		}

		public QvDataContractResponse getTables(QvxConnection connection)
		{
			return new QvDataContractTableListResponse
			{
				qTables = connection.MTables
			};
		}

		public QvDataContractResponse getFields(QvxConnection connection, string table)
		{
			QvxTable currentTable = null;

			return new QvDataContractFieldListResponse
			{
				qFields = (currentTable != null) ? currentTable.Fields : new QvxField[0]
			};
		}
	}
}
