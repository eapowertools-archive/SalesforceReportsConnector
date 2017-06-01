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

		/**
         * QlikView 12 classes
         */

		public override string HandleJsonRequest(string method, string[] userParameters, QvxConnection connection)
		{
			TempLogger.Log("I made a request");

			if (method.StartsWith("API-"))
			{
				return HandleAPIRequests(method, userParameters);
			}
			else
			{
				return HandleRequest(method, userParameters, connection);
			}
		}

		private string HandleRequest(string method, string[] userParameters, QvxConnection connection)
		{

			QvDataContractResponse response;

			string provider, host, username, access_token, refresh_token;
			connection.MParameters.TryGetValue("provider", out provider); // Set to the name of the connector by QlikView Engine
			connection.MParameters.TryGetValue("userid", out username); // Set when creating new connection or from inside the QlikView Management Console (QMC)
			connection.MParameters.TryGetValue("host", out host); // Defined when calling createNewConnection in connectdialog.js
			connection.MParameters.TryGetValue("access_token", out access_token);
			connection.MParameters.TryGetValue("refresh_token", out refresh_token);

			TempLogger.Log("All the infoz");
			TempLogger.Log(provider);
			TempLogger.Log(username);
			TempLogger.Log(host);
			TempLogger.Log(access_token);
			TempLogger.Log(refresh_token);



			switch (method)
			{
				case "getDatabases":
					response = null;
					//response = getDatabases(allTheThings, password);
					break;
				case "getOwner":
					response = new Info { qMessage = username };
					break;
				case "getTables":
					response = getTables(username, connection, userParameters[0], userParameters[1]);
					break;
				case "getFields":
					response = getFields(username, connection, userParameters[0], userParameters[1], userParameters[2]);
					break;
				default:
					response = new Info { qMessage = "Unknown command" };
					break;
			}
			return ToJson(response); // serializes response into JSON string		
		}

		private string HandleAPIRequests(string method, string[] userParameters)
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
					string username = EndpointCalls.getUsername(userParameters[0], userParameters[1], userParameters[2], userParameters[3], userParameters[4]);
					response = new Info { qMessage = username };
					break;
				default:
					response = new Info { qMessage = "Unknown command" };
					break;
			}
			return ToJson(response);
		}

		public QvDataContractResponse getDatabases(string username, string password)
		{
			return new QvDataContractDatabaseListResponse
			{
				qDatabases = new Database[]
				{
					new Database { qName =  username + "Salesforce Reports" }
				}
			};
		}

		public QvDataContractResponse getTables(string username, QvxConnection connection, string database, string owner)
		{
			return new QvDataContractTableListResponse
			{
				qTables = connection.MTables
			};
		}

		public QvDataContractResponse getFields(string username, QvxConnection connection, string database, string owner, string table)
		{
			QvxTable currentTable = null;

			return new QvDataContractFieldListResponse
			{
				qFields = (currentTable != null) ? currentTable.Fields : new QvxField[0]
			};
		}
	}
}
