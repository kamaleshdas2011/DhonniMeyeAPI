using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DhonniMeyeAPI.Controllers
{
    [RoutePrefix("api/miscellaneous")]
    
    public class MiscellaneousController : ApiController
    {
        // GET: api/Miscellaneous
        [Route("validatecoupon/{code}")]
        [HttpGet]
        public IHttpActionResult ValidateCoupon(string code)
        {
            return Ok(new { Valid = true, Discount = 300, Description="This coupon is valid and discount of RS.300 is applicaple on this order." });

        }
    }
}
