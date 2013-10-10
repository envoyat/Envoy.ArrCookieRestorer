using System.Configuration;
using System.Text.RegularExpressions;
using System.Web;

namespace Envoy.ArrCookieRestorer
{
	// Concept based on http://improve.dk/blog/2009/12/09/fixing-flash-bugs-by-intercepting-iis-application-request-routing-cookies
	public sealed class ArrCookieRestorerModule : IHttpModule
	{
		readonly static string cookieName;
		readonly static string queryStringName;
		readonly static Regex regex;

		static ArrCookieRestorerModule()
		{
			var config = (ArrCookieRestorerSection)ConfigurationManager.GetSection("arrCookieRestorer");
			cookieName = config.CookieName;
			queryStringName = config.QueryStringName;
			regex = new Regex(cookieName + "=[0-9a-f]+;?", RegexOptions.Compiled);
		}

		public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			context.BeginRequest += (sender, e) =>
			{
				try
				{
					var application = sender as HttpApplication;
					if (application != null)
					{
						var request = application.Request;
						var response = application.Response;

						if (request != null && response != null)
						{
							Restore(new HttpRequestWrapper(request), new HttpResponseWrapper(response));
						}
					}
				}
				catch
				{
				}
			};
		}

		public static void Restore(HttpRequestBase request, HttpResponseBase response)
		{
			string serverHash = request.QueryString[queryStringName];

			if (serverHash != null)
			{
				string cookieHeader = request.Headers["Cookie"];
				string cookieValue = cookieName + "=" + serverHash;

				// Modifying request.Cookies doesn't work

				if (cookieHeader != null)
				{
					if (cookieHeader.Contains(cookieName + "="))
					{
						cookieHeader = regex.Replace(cookieHeader, cookieValue + ";");
					}
					else
					{
						cookieHeader += "; " + cookieValue;
					}

					request.Headers["Cookie"] = cookieHeader;
				}
				else
				{
					request.Headers.Add("Cookie", cookieValue);
				}

				// response.Cookies also updates request.Cookies, which may have other implications, so we set the raw cookie
				response.Headers.Add("Set-Cookie", cookieName + "=" + serverHash + ";Path=" + request.ApplicationPath + ";Domain=" + request.Url.Host);
			}
		}
	}
}
