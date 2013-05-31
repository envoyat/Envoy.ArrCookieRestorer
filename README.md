Envoy.ArrCookieRestorer: ARR Client Affinity Across Multiple Domains
=

Microsoft's IIS [Application Request Routing (ARR)] [1] is a load balancer with support for "client affinity" (also known as "sticky sessions").

ARR can automatically set a cookie which is used on subsequent requests to direct a browser to the same content server. (This cookie is named `ARRAffinity` by default.)

This works for sites which share a single domain (and therefore share cookies), but we faced a situation where we needed session affinity across multiple domains. The Envoy.ArrCookieRestorer module provides a way to solve this problem.

**Major credit for this is due to [Mark S. Rasmussen] [3] who published this technique and similar code for solving problems with Flash authentication cookies and ARR.**

Example
-

Consider:

* A single IIS site
* Listening on multiple bindings:
  * http://www.AAA
  * http://www.BBB
  * http://www.CCC
  * https://secure.MAINSITE
* The various AAA, BBB, CCC sites may be landing pages; and MAINSITE is a centralised secure checkout page

A typical browser request flow:

1. Browse around http://www.AAA site
2. POST to http://www.AAA to enter the checkout
3. http://www.AAA responds with a redirect to `https://secure.MAINSITE/?cartId=RANDOM_ID_HERE`
4. The page at https://secure.MAINSITE uses the `cartId` query string variable to locate the user's shopping cart (remembering this is the same IIS site, same worker process - there is no database involved)

We were faced with the problem of load balancing this application. It was essential that requests to the different URLs went to the same IIS site so that the application's cart loading mechanism would continue to work. (Changing the application to store its state externally was not feasible.) Therefore, we looked to implement sticky sessions across domains.


Solution
-

1. Modify the application to read the `ARRAffinity` cookie value and append it to the query string when redirecting.

   That is, instead of the application redirecting to `https://secure.MAINSITE/?cartId=RANDOM_ID_HERE`, it redirects to `https://secure.MAINSITE/?cartId=RANDOM_ID_HERE&ARRAffinity=7e62a6ea5da8fe...`

   This was a very minor change to make to the application. In our case we had source code available, but techniques like [URL Rewrite] [2] outbound rules or ASP.NET Control Adapters could be other ways to do this.

2. Install our Envoy.ArrCookieRestorer Module into ARR.

3. When a request comes in to `https://secure.MAINSITE/?cartId=RANDOM_ID_HERE&ARRAffinity=7e62a6ea5da8fe...`, Envoy.ArrCookieRestorer intercepts the request before ARR processes it. Envoy.ArrCookieRestorer sees the ARRAffinity item in the query string and translates it internally into a cookie. When ARR gets to process it, it acts as if it was a normal sticky session cookie, and routes the request to the same content server.

Installation
-

On the ARR server, install the Envoy.ArrCookieRestorer.dll in the GAC or in somewhere accessible to your ARR site.

In ARR's web.config, configure the following.

Configuration section:

		<configSections>
			<section name="arrCookieRestorer" type="Envoy.ArrCookieRestorer.ArrCookieRestorerSection, Envoy.ArrCookieRestorer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=a9d8578d4e655717"/>
		</configSections>

Configure the names of the cookie and query string variable:

		<arrCookieRestorer cookieName="ARRAffinity" queryStringName="ARRAffinity" />

Register the module:

		<system.webServer>
			<modules>
				<add name="ArrCookieRestorerModule" type="Envoy.ArrCookieRestorer.ArrCookieRestorerModule, Envoy.ArrCookieRestorer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=a9d8578d4e655717" />
			</modules>
		</system.webServer>

History
-
Technique published by [Mark S. Rasmussen] [3] 2009.

Implemented by Envoy 2010, and has happily served billions of requests since.

Code published to GitHub 2013.


  [1]: http://www.iis.net/downloads/microsoft/application-request-routing
  [2]: http://www.iis.net/downloads/microsoft/url-rewrite
  [3]: http://improve.dk/fixing-flash-bugs-by-intercepting-iis-application-request-routing-cookies/