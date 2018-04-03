using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;

namespace DhonniMeyeAPI.Controllers
{
    public class UserActivityController : ApiController
    {
        // GET: api/UserActivity
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/UserActivity/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/UserActivity
        public void Post([FromBody]User value)
        {
        }

        // PUT: api/UserActivity/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/UserActivity/5
        public void Delete(int id)
        {
        }
    }
}
