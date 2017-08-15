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
				if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized || ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Forbidden)
				{
					Uri authHostnameUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST]);
					string newTokenPath = string.Format("/services/oauth2/token?grant_type=refresh_token&client_id={0}&refresh_token={1}", QvxSalesforceConnectionInfo.CLIENT_ID, connectionParams[QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN]);
					HttpWebRequest newTokenRequest = (HttpWebRequest)HttpWebRequest.Create(new Uri(authHostnameUri, newTokenPath));
					newTokenRequest.Method = "POST";

					string newAccessToken = connectionParams[QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN];
					using (HttpWebResponse newTokenResponse = (HttpWebResponse)newTokenRequest.GetResponse())
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

					try
					{
						return endpointCall(newAccessToken);
					}
					catch (Exception ex)
					{
						QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Error, "Call to Salesforce API failed with exception: " + ex.Message);
						return default(T);
					}
				}
				else
				{
					QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Error, "Failed trying to get new access token. Refresh token is: " + connectionParams[QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN]);
					QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Error, String.Format("Status code: '{0}' Exception: {1}", ((HttpWebResponse)e.Response).StatusCode, e.Message));
					throw new Exception("Invalid Web Response");
				}
			}
			catch (Exception exception)
			{
				QvxLog.Log(QvxLogFacility.Application, QvxLogSeverity.Error, "Call to Salesforce API failed with exception: " + exception.Message);
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
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(idURI);
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + token);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
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
			if (connectionParams == null)
			{
				return new List<string>();
			}

			return ValidateAccessTokenAndPerformRequest<IEnumerable<string>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
					"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + "/query?q=SELECT Name FROM Folder WHERE Type = 'Report' ORDER BY Name"));
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + accessToken);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);
						IEnumerable<JObject> folders = jsonResponse["records"].Values<JObject>();
						folders = folders.Where(x => !string.IsNullOrEmpty(x["Name"].Value<string>()) && x["Name"].Value<string>() != "*");
						IList<string> folderNames = folders.Select(f => f["Name"].Value<string>()).ToList();
						folderNames.Insert(0, "Private Reports");
						folderNames.Insert(0, "Public Reports");
						return folderNames;
					}
				}
			});
		}

		public static IDictionary<string, string> GetTableNameList(QvxConnection connection, string databaseName)
		{
			IDictionary<string, string> connectionParams = GetParamsFromConnection(connection);
			if (connectionParams == null)
			{
				return new Dictionary<string, string>();
			}

			return ValidateAccessTokenAndPerformRequest<IDictionary<string, string>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);
				HttpWebRequest request;
				WebHeaderCollection headers;
				string databaseId = "";
				if (databaseName == "Public Reports" || databaseName == "Private Reports")
				{
					databaseId = databaseName;
				}
				else
				{
					request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
						"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Folder WHERE Type = 'Report' AND Name = '{0}' ORDER BY Name", Uri.EscapeDataString(databaseName))));
					request.Method = "GET";
					headers = new WebHeaderCollection();
					headers.Add("Authorization", "Bearer " + accessToken);
					request.Headers = headers;

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					{
						using (Stream stream = response.GetResponseStream())
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
					}
				}

				return ValidateAccessTokenAndPerformRequest<IDictionary<string, string>>(connection, connectionParams, (newAccessToken) =>
				{
					if (databaseId == "Public Reports" || databaseId == "Private Reports")
					{
						request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
						  "/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Report WHERE FolderName = '{0}' ORDER BY Name", databaseId)));
					}
					else
					{
						request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
						  "/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Report WHERE OwnerId = '{0}' ORDER BY Name", databaseId)));
					}
					request.Method = "GET";
					headers = new WebHeaderCollection();
					headers.Add("Authorization", "Bearer " + newAccessToken);
					request.Headers = headers;

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					{
						using (Stream stream = response.GetResponseStream())
						{
							StreamReader reader = new StreamReader(stream, Encoding.UTF8);
							String responseString = reader.ReadToEnd();
							JObject jsonResponse = JObject.Parse(responseString);
							IEnumerable<JObject> tables = jsonResponse["records"].Values<JObject>();
							IDictionary<string, string> tableIdDictionary = tables.ToDictionary(r => r["Id"].Value<string>(), r => r["Name"].Value<string>());
							return tableIdDictionary;
						}
					}
				});
			});
		}

		// Helper Functions

		public static Dictionary<string, string> GetParamsFromConnection(QvxConnection connection)
		{
			string host, authHost, username, access_token, refresh_token;

			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_USERID, out username); // Set when creating new connection or from inside the QlikView Management Console (QMC)
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_HOST, out host);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST, out authHost);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN, out access_token);
			connection.MParameters.TryGetValue(QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN, out refresh_token);

			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(authHost) || string.IsNullOrEmpty(access_token) || string.IsNullOrEmpty(refresh_token))
			{
				return null;
			}

			Dictionary<string, string> connectionValues = new Dictionary<string, string>();
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_USERID, username);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_HOST, host);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_AUTHHOST, authHost);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_ACCESS_TOKEN, access_token);
			connectionValues.Add(QvxSalesforceConnectionInfo.CONNECTION_REFRESH_TOKEN, refresh_token);

			return connectionValues;
		}

		public static IDictionary<string, SalesforceDataType> GetFieldsFromReport(QvxConnection connection, string reportID)
		{
			IDictionary<string, string> connectionParams = GetParamsFromConnection(connection);
			if (connectionParams == null)
			{
				return new Dictionary<string, SalesforceDataType>();
			}

			return ValidateAccessTokenAndPerformRequest<IDictionary<string, SalesforceDataType>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
					"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + "/analytics/reports/" + reportID + "/describe"));
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + accessToken);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);

						JToken columnArray = jsonResponse["reportExtendedMetadata"]["detailColumnInfo"];
						IDictionary<string, SalesforceDataType> columns = columnArray.ToDictionary(c => c.First["label"].Value<string>(), t =>
						{
							SalesforceDataType columnType = SalesforceDataType.String;
							switch (t.First["dataType"].Value<string>())
							{
								case "string":
									columnType = SalesforceDataType.String;
									break;
								case "int":
									columnType = SalesforceDataType.Integer;
									break;
								case "double":
									columnType = SalesforceDataType.Double;
									break;
								case "boolean":
									columnType = SalesforceDataType.Boolean;
									break;
								case "percent":
									columnType = SalesforceDataType.Percent;
									break;
								case "date":
									columnType = SalesforceDataType.Date;
									break;
								case "datetime":
									columnType = SalesforceDataType.DateTime;
									break;
								case "currency":
									columnType = SalesforceDataType.Currency;
									break;
							}
							return columnType;
						});
						return columns;

					}
				}
			});
		}

		public static IEnumerable<QvxDataRow> GetReportData(QvxConnection connection, QvxFieldsWrapper fields, string reportID)
		{
			IDictionary<string, string> connectionParams = GetParamsFromConnection(connection);
			if (connectionParams == null)
			{
				return new List<QvxDataRow>();
			}

			return ValidateAccessTokenAndPerformRequest<IEnumerable<QvxDataRow>>(connection, connectionParams, (accessToken) =>
			{
				Uri hostUri = new Uri(connectionParams[QvxSalesforceConnectionInfo.CONNECTION_HOST]);
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
					"/services/data/" + QvxSalesforceConnectionInfo.SALESFORCE_API_VERSION + "/analytics/reports/" + reportID + "?includeDetails=true"));
				request.Method = "GET";
				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add("Authorization", "Bearer " + accessToken);
				request.Headers = headers;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);

						IEnumerable<QvxDataRow> rows = jsonResponse["factMap"].First.First["rows"].Values<JObject>().Select(
						dr =>
						{
							QvxDataRow row = new QvxDataRow();
							for (int i = 0; i < fields.GetLength(); i++)
							{
								if (fields.GetFieldType(i) == SalesforceDataType.Double || fields.GetFieldType(i) == SalesforceDataType.Percent)
								{
									try
									{
										row[fields.GetField(i)] = dr.First.First.ElementAt(i)["value"].Value<double>();
									}
									catch
									{
										row[fields.GetField(i)] = null;
									}
								}
								else if (fields.GetFieldType(i) == SalesforceDataType.Integer)
								{
									try
									{
										row[fields.GetField(i)] = dr.First.First.ElementAt(i)["value"].Value<int>();
									}
									catch
									{
										row[fields.GetField(i)] = null;
									}
								}
								else if (fields.GetFieldType(i) == SalesforceDataType.Boolean)
								{
									try
									{
										row[fields.GetField(i)] = dr.First.First.ElementAt(i)["value"].Value<bool>();
									}
									catch
									{
										row[fields.GetField(i)] = null;
									}
								}
								else if (fields.GetFieldType(i) == SalesforceDataType.Currency)
								{
									if (dr.First.First.ElementAt(i)["value"].HasValues)
									{
										row[fields.GetField(i)] = dr.First.First.ElementAt(i)["value"].Value<double>("amount");

									}
									else
									{
										row[fields.GetField(i)] = null;
									}
								}
								else if (fields.GetFieldType(i) == SalesforceDataType.Date || fields.GetFieldType(i) == SalesforceDataType.DateTime)
								{
									string dt = dr.First.First.ElementAt(i)["value"].Value<string>();
									if (string.IsNullOrWhiteSpace(dt) || dt == "-")
									{
										row[fields.GetField(i)] = null;
									}
									else
									{
										DateTime datetime = DateTime.Parse(dt);
										row[fields.GetField(i)] = datetime.ToOADate();
									}
								}
								else
								{
									row[fields.GetField(i)] = dr.First.First.ElementAt(i)["label"].Value<string>();
								}
							}
							return row;
						});
						return rows;
					}
				}
			});

		}
	}
}
