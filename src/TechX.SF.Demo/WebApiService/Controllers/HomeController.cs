using System.Collections.Generic;
using System.Web.Http;

namespace WebApiService.Controllers
{
	[ServiceRequestActionFilter]
	public class HomeController : ApiController
	{
		// GET api/values 
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}
	}
}
