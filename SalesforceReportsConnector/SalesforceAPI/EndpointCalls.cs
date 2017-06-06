using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using QlikView.Qvx.QvxLibrary;
using SalesforceReportsConnector.QVX;

namespace SalesforceReportsConnector.SalesforceAPI
{
	public class EndpointCalls
	{
		public static T ValidateAccessTokenAndPerformRequest<T>(QvxConnection connection, IDictionary<string, string> connectionParams, Func<string, T> endpointCall)
		{
			try
			{
				return endpointCall(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN]);
			}
			catch (WebException e)
			{
				if (((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.Unauthorized || ((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.Forbidden)
				{
					Uri authHostnameUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST]);
					string newTokenPath = string.Format("/services/oauth2/token?grant_type=refresh_token&client_id={0}&refresh_token={1}", QvxSalesforceConnectionInfo.CLIENT_ID, connectionParams[QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN]);
					HttpWebRequest newTokenRequest = (HttpWebRequest) HttpWebRequest.Create(new Uri(authHostnameUri, newTokenPath));
					newTokenRequest.Method = "POST";

					string newAccessToken = connectionParams[QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN];
					using (HttpWebResponse newTokenResponse = (HttpWebResponse) newTokenRequest.GetResponse())
					{
						using (Stream stream = newTokenResponse.GetResponseStream())
						{
							StreamReader reader = new StreamReader(stream, Encoding.UTF8);
							String responseString = reader.ReadToEnd();
							JObject response = JObject.Parse(responseString);
							newAccessToken = response["access_token"].Value<string>();
						}
					}
					connection.MParameters[QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN] = newAccessToken;
					return endpointCall(newAccessToken);
				}
				else
				{
					throw new Exception("Invalid Web Response");
				}
			}
			catch (Exception e)
			{
				return default(T);
			}
		}

		public static string GetUsername(QvxConnection connection, string authHostname, string accessToken, string refreshToken, string hostname, string idURL)
		{
			Dictionary<string, string> connectionValues = new Dictionary<string, string>();
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_HOST, hostname);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST, authHostname);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN, accessToken);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN, refreshToken);

			return ValidateAccessTokenAndPerformRequest<string>(connection, connectionValues, (token) =>
			{
				Uri idURI = new Uri(idURL);
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(idURI);
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + token);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);

						return jsonResponse["username"].Value<string>();
					}
				}
			});
		}

		public static IEnumerable<string> GetReportFoldersList(QvxConnection connection)
		{
			IDictionary<string, string> connectionParams = GetParamsFromConnection(connection);

			return ValidateAccessTokenAndPerformRequest<IEnumerable<string>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(hostUri,
					"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + "/query?q=SELECT Name FROM Folder WHERE Type = 'Report' ORDER BY Name"));
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + accessToken);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						try
						{
							StreamReader reader = new StreamReader(stream, Encoding.UTF8);
							String responseString = reader.ReadToEnd();
							JObject jsonResponse = JObject.Parse(responseString);
							IEnumerable<JObject> folders = jsonResponse["records"].Values<JObject>();
							folders = folders.Where(x => !string.IsNullOrEmpty(x["Name"].Value<string>()) && x["Name"].Value<string>() != "*");
							return folders.Select(f => f["Name"].Value<string>());
						}
						catch (Exception e)
						{
							return new List<string>();
						}
					}
				}
			});
		}

		public static IEnumerable<string> GetTableNameList(QvxConnection connection, string databaseName)
		{
			IDictionary<string, string> connectionParams = GetParamsFromConnection(connection);

			return ValidateAccessTokenAndPerformRequest<IEnumerable<string>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);

				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(hostUri,
					"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Folder WHERE Type = 'Report' AND Name = '{0}' ORDER BY Name", databaseName)));
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + accessToken);
				request.Headers = headers;

				string databaseId = "";

				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						try
						{
							StreamReader reader = new StreamReader(stream, Encoding.UTF8);
							String responseString = reader.ReadToEnd();
							JObject jsonResponse = JObject.Parse(responseString);
							IEnumerable<JObject> folders = jsonResponse["records"].Values<JObject>();
							if (folders.Count() > 1)
							{
								throw new DataMisalignedException("Too many matches for folder: " + databaseName);
							}
							JObject firstFolder = folders.First();
							databaseId = firstFolder["Id"].Value<string>();
						}
						catch (Exception e)
						{
							return new List<string>();
						}
					}
				}


				return ValidateAccessTokenAndPerformRequest<IEnumerable<string>>(connection, connectionParams, (newAccessToken) =>
				{
					request = (HttpWebRequest) WebRequest.Create(new Uri(hostUri,
						"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Report WHERE OwnerId = '{0}' ORDER BY Name", databaseId)));
					request.Method = "GET";
					headers = new WebHeaderCollection();
					headers.Add("Authorization", "Bearer " + newAccessToken);
					request.Headers = headers;

					using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
					{
						using (Stream stream = response.GetResponseStream())
						{
							try
							{
								StreamReader reader = new StreamReader(stream, Encoding.UTF8);
								String responseString = reader.ReadToEnd();
								JObject jsonResponse = JObject.Parse(responseString);
								IEnumerable<JObject> tables = jsonResponse["records"].Values<JObject>();
								IEnumerable<string> tableStringList = tables.Select(t => t["Name"].Value<string>());
								return tableStringList;
							}
							catch (Exception e)
							{
								return new List<string>();
							}
						}
					}
				});
			});
		}

		// Helper Functions

		public static Dictionary<string, string> GetParamsFromConnection(QvxConnection connection)
		{
			string provider, host, authHost, username, access_token, refresh_token;

			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_PROVIDER, out provider); // Set to the name of the connector by QlikView Engine
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_USERID, out username); // Set when creating new connection or from inside the QlikView Management Console (QMC)
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_HOST, out host);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST, out authHost);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN, out access_token);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN, out refresh_token);

			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(authHost) || string.IsNullOrEmpty(access_token) || string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(provider))
			{
				return null;
			}

			Dictionary<string, string> connectionValues = new Dictionary<string, string>();
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_PROVIDER, provider);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_USERID, username);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_HOST, host);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST, authHost);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN, access_token);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN, refresh_token);

			return connectionValues;
		}
	}
}
