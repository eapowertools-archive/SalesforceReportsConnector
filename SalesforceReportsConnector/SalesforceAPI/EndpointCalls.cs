using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json.Linq;
using SalesforceReportsConnector.Logger;

namespace SalesforceReportsConnector.SalesforceAPI
{
	public class EndpointCalls
	{
		public static string CLIENT_ID = "3MVG9i1HRpGLXp.qErQ40T3OFL3qRBOgiz5J6AYv5uGazuHU3waZ1hDGeuTmDXVh_EadH._6FJFCwBCkMTCXk";
		public static string SALESFORCE_API_VERSION = "v39.0";

		public static string getAccessToken(string authHostname, string accessToken, string refreshToken, string hostname)
		{
			Uri baseUri = new Uri(hostname);
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(baseUri, "/services/data/" + SALESFORCE_API_VERSION + "/"));
			request.Method = "GET";
			WebHeaderCollection headers = new WebHeaderCollection();
			headers.Add("Authorization", "Bearer " + accessToken);
			request.Headers = headers;

			try
			{
				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						return accessToken;
					}
					else
					{
						throw new InvalidOperationException("Got the wrong response when pinging the salesforce server.");
					}
				}
			}
			catch (WebException e)
			{
				if (((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.Unauthorized)
				{
					Uri authHostnameUri = new Uri(authHostname);
					string newTokenPath = string.Format("/services/oauth2/token?grant_type=refresh_token&client_id={0}&refresh_token={1}", CLIENT_ID, refreshToken);
					HttpWebRequest newTokenRequest = (HttpWebRequest) HttpWebRequest.Create(new Uri(authHostnameUri, newTokenPath));
					newTokenRequest.Method = "POST";

					using (HttpWebResponse newTokenResponse = (HttpWebResponse) newTokenRequest.GetResponse())
					{
						using (Stream stream = newTokenResponse.GetResponseStream())
						{
							StreamReader reader = new StreamReader(stream, Encoding.UTF8);
							String responseString = reader.ReadToEnd();
							JObject response = JObject.Parse(responseString);
							return response["access_token"].Value<string>();
						}
					}

				}
				else
				{
					throw new InvalidOperationException("Got the wrong response when pinging the salesforce server.");
				}
			}
		}

		public static Tuple<string, string> getUsername(string authHostname, string accessToken, string refreshToken, string hostname, string idURL)
		{
			accessToken = getAccessToken(authHostname, accessToken, refreshToken, hostname);

			Uri idURI = new Uri(Uri.UnescapeDataString(idURL));
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(idURI);
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
					TempLogger.Log("about to return from getUserName");

					return new Tuple<string, string>(accessToken, jsonResponse["username"].Value<string>());
				}
			}
		}

		public static Tuple<string, IDictionary<string, string>> GetReportFoldersList(string host, string authHostname, string accessToken, string refreshToken)
		{
			accessToken = getAccessToken(authHostname, accessToken, refreshToken, host);
			Uri hostUri = new Uri(host);

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(hostUri,
				"https://eu1.salesforce.com/services/data/" + SALESFORCE_API_VERSION + "/query?q=SELECT Id,Name FROM Folder WHERE Type = 'Report' ORDER BY Name"));
			request.Method = "GET";
			WebHeaderCollection headers = new WebHeaderCollection();
			headers.Add("Authorization", "Bearer " + accessToken);
			request.Headers = headers;

			try
			{
				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);
						IEnumerable<JObject> folders = jsonResponse["records"].Values<JObject>();
						folders = folders.Where(x => !string.IsNullOrEmpty(x["Name"].Value<string>()) && x["Name"].Value<string>() != "*");
						Dictionary<string, string> folderDictionary = folders.ToDictionary(x => x["Name"].Value<string>(), y => y["Id"].Value<string>());
						return new Tuple<string, IDictionary<string, string>>(accessToken, folderDictionary);
					}
				}
			}
			catch (Exception e)
			{
				TempLogger.Log(e.Message);
				return new Tuple<string, IDictionary<string, string>>(accessToken, new Dictionary<string, string>());
			}
		}

		public static Tuple<string, IEnumerable<string>> getTableNameList(string host, string authHostname, string accessToken, string refreshToken, string databaseName)
		{
			accessToken = getAccessToken(authHostname, accessToken, refreshToken, host);
			Uri hostUri = new Uri(host);

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(hostUri,
				"https://eu1.salesforce.com/services/data/" + SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Folder WHERE Type = 'Report' AND Name = '{0}' ORDER BY Name", databaseName)));
			request.Method = "GET";
			WebHeaderCollection headers = new WebHeaderCollection();
			headers.Add("Authorization", "Bearer " + accessToken);
			request.Headers = headers;

			string databaseId = "";
			try
			{
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						String responseString = reader.ReadToEnd();
						JObject jsonResponse = JObject.Parse(responseString);
						IEnumerable<JObject> folders = jsonResponse["records"].Values<JObject>();
						TempLogger.Log("folders have: " + folders.Count());
						if (folders.Count() > 1)
						{
							throw new DataMisalignedException("Too many matches for folder: " + databaseName);
						}
						JObject firstFolder = folders.First();
						databaseId = firstFolder["Id"].Value<string>();
					}
				}

				accessToken = getAccessToken(authHostname, accessToken, refreshToken, host);
				hostUri = new Uri(host);
				request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri,
					"https://eu1.salesforce.com/services/data/" + SALESFORCE_API_VERSION + string.Format("/query?q=SELECT Id,Name FROM Report WHERE OwnerId = '{0}' ORDER BY Name", databaseId)));
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
						IEnumerable<JObject> tables = jsonResponse["records"].Values<JObject>();
						IEnumerable<string> tableStringList = tables.Select(t => t["Name"].Value<string>());
						return new Tuple<string, IEnumerable<string>>(accessToken, tableStringList);
					}
				}

			}
			catch (Exception e)
			{
				TempLogger.Log(e.Message);
				return new Tuple<string, IEnumerable<string>>(accessToken, new List<string>());
			}
		}
	}
}
