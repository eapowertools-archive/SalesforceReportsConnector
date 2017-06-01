using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
					return new Tuple<string, string>(accessToken, jsonResponse["username"].Value<string>());
				}
			}
		}

		public static Tuple<string, IList<string>> getTableNameList(string host, string authHostname, string accessToken, string refreshToken)
		{
			accessToken = getAccessToken(authHostname, accessToken, refreshToken, host);

			Uri hostUri = new Uri(host);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(hostUri, "/services/data/" + SALESFORCE_API_VERSION + "/analytics/reports"));
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
					TempLogger.Log(responseString);
					return new Tuple<string, IList<string>>(accessToken, new List<string>());
				}
			}
		}
	}
}
