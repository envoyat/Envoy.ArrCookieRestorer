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
			Assert.AreEqual("arrCookie=good;Path=/", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_HasQueryString_HasCookie_NoArrCookie()
		{
			request.QueryString["arrQueryString"] = "good";
			request.Headers["Cookie"] = "a=b";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("a=b; arrCookie=good", request.Headers["Cookie"]);
			Assert.AreEqual("arrCookie=good;Path=/", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_HasQueryString_NoCookie()
		{
			request.QueryString["arrQueryString"] = "good";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("arrCookie=good", request.Headers["Cookie"]);
			Assert.AreEqual("arrCookie=good;Path=/", response.Headers["Set-Cookie"]);
		}

		[TestMethod]
		public void TestRestore_NoQueryString()
		{
			request.Headers["Cookie"] = "a=b; arrCookie=bad; x=y";
			ArrCookieRestorerModule.Restore(request, response);
			Assert.AreEqual("a=b; arrCookie=bad; x=y", request.Headers["Cookie"]);
			Assert.IsNull(response.Headers["Set-Cookie"]);
		}
	}
}
