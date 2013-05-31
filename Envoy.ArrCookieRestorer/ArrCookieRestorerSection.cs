using System.Configuration;

namespace Envoy.ArrCookieRestorer
{
	public sealed class ArrCookieRestorerSection : ConfigurationSection
	{
		[ConfigurationProperty("cookieName", IsRequired = true)]
		public string CookieName
		{
			get { return (string)base["cookieName"]; }
			set { base["cookieName"] = value; }
		}

		[ConfigurationProperty("queryStringName", IsRequired = true)]
		public string QueryStringName
		{
			get { return (string)base["queryStringName"]; }
			set { base["queryStringName"] = value; }
		}
	}
}
