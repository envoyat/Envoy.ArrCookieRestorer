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
		readonly static Regex duplicateCookieRegex;

		static ArrCookieRestorerModule()
		{
			var config = (ArrCookieRestorerSection)ConfigurationManager.GetSection("arrCookieRestorer");
			cookieName = config.CookieName;
			queryStringName = config.QueryStringName;
			regex = new Regex(cookieName + "=[0-9a-f]+;?", RegexOptions.Compiled);
			duplicateCookieRegex = new Regex(cookieName + "=.*" + cookieName + "=", RegexOptions.Compiled);
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
							var wrappedRequest = new HttpRequestWrapper(request);
							var wrappedResponse = new HttpResponseWrapper(response);

							ResolveDuplicateCookies(wrappedRequest, wrappedResponse);
							Restore(wrappedRequest, wrappedResponse);
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

		/// <summary>
		/// The cookie created by ARR changed so it now includes the Domain property.  Some browsers treat cookies with
		/// the same name but with/without the domain property as separate cookies and keep both of them.
		/// Also, given that Chrome (in particular - see https://support.google.com/chrome/answer/95421?hl=en) may 
		/// never clear session cookies, this presents a possibly serious duplicate cookie issue.
		/// 
		/// The fix implemented is to detect if the request contains multiple cookies with the appropriate name, and if
		/// so, send a response which expires the duplicate cookie.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		public static void ResolveDuplicateCookies(HttpRequestBase request, HttpResponseBase response)
		{
			string cookieHeader = request.Headers["Cookie"];
			if (cookieHeader != null && duplicateCookieRegex.IsMatch(cookieHeader))
			{
				response.Headers.Add("Set-Cookie", cookieName + "=;Path=" + request.ApplicationPath + ";Expires=Fri, 01-Jan-1970 00:00:00 GMT");
			}
		}
	}
}
