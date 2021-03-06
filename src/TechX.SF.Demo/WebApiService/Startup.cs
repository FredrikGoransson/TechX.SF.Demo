﻿using System.Web.Http;
using Owin;

namespace WebApiService
{
	public static class Startup
	{
		// This code configures Web API. The Startup class is specified as a type
		// parameter in the WebApp.Start method.
		public static void ConfigureApp(IAppBuilder appBuilder)
		{
			// Configure Web API for self-host. 
			HttpConfiguration config = new HttpConfiguration();

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{id}",
				defaults: new { id = RouteParameter.Optional, controller = "home" }
			);

			appBuilder.UseWebApi(config);
		}
	}
}
