using System;
using System.Collections.Specialized;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace Envoy.ArrCookieRestorer.Testing
{
	[TestClass]
	public class ArrCookieRestorerModuleTest
	{
		HttpRequestBase request;
		HttpResponseBase response;

		[TestInitialize]
		public void Initialise()
		{
			request = MockRepository.GenerateStub<HttpRequestBase>();
			request.Stub(r => r.Headers).Return(new NameValueCollection());
			request.Stub(r => r.QueryString).Return(new NameValueCollection());
			request.Stub(r => r.ApplicationPath).Return("/");
			request.Stub(r => r.Url).Return(new Uri("http://www.example.com/"));
			response = MockRepository.GenerateStub<HttpResponseBase>();
			response.Stub(r => r.Headers).Return(new NameValueCollection());
		}

		[TestMethod]
		public void TestRestore_HasQueryString_HasCookie_HasArrCookie()
		{
			request.QueryString["arrQueryString"] = "good";
			request.Headers["Cookie"] = "a=b; arrCookie=bad; x=y";
			
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("a=b; arrCookie=good; x=y", request.Headers["Cookie"]);
			Assert.AreEqual("arrCookie=good;Path=/;Domain=www.example.com", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_HasQueryString_HasCookie_NoArrCookie()
		{
			request.QueryString["arrQueryString"] = "good";
			request.Headers["Cookie"] = "a=b";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("a=b; arrCookie=good", request.Headers["Cookie"]);
			Assert.AreEqual("arrCookie=good;Path=/;Domain=www.example.com", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_HasQueryString_NoCookie()
		{
			request.QueryString["arrQueryString"] = "good";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("arrCookie=good", request.Headers["Cookie"]);
			Assert.AreEqual("arrCookie=good;Path=/;Domain=www.example.com", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_NoQueryString()
		{
			request.Headers["Cookie"] = "a=b; arrCookie=bad; x=y";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("a=b; arrCookie=bad; x=y", request.Headers["Cookie"]);
			Assert.IsNull(response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestResolveDuplicateCookies_NoCookies()
		{
			request.Headers["Cookie"] = null;
			ArrCookieRestorerModule.ResolveDuplicateCookies(request, response);
			Assert.IsNull(response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestResolveDuplicateCookies_HasCookie_NoArrCookie()
		{
			request.Headers["Cookie"] = "a=b";
			ArrCookieRestorerModule.ResolveDuplicateCookies(request, response);
			Assert.IsNull(response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestResolveDuplicateCookies_HasCookie_OneArrCookie()
		{
			request.Headers["Cookie"] = "a=b;arrCookie=good;c=d";
			ArrCookieRestorerModule.ResolveDuplicateCookies(request, response);
			Assert.IsNull(response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestResolveDuplicateCookies_HasCookie_DuplicateArrCookie()
		{
			request.Headers["Cookie"] = "a=b;arrCookie=good;c=d;arrCookie=bad";
			ArrCookieRestorerModule.ResolveDuplicateCookies(request, response);
			Assert.AreEqual("arrCookie=;Path=/;Expires=Fri, 01-Jan-1970 00:00:00 GMT", response.Headers["Set-Cookie"]);
		}
	}
}
