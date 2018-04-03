using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DhonniMeyeAPI.Controllers
{
    public class ImageController : ApiController
    {
        // GET: api/Image
        //[Authorize]
        public IEnumerable<ProductImage> Get(int id, string imageType)
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                if (imageType.ToLower().Equals("details"))
                {
                    var images = (from img in db.ProductImages where img.ProductID == id && img.ImageType == imageType select img).OrderBy(m=>m.ProductID).ToList();
                    return images;
                }
                else
                {
                    var images = (from img in db.ProductImages where img.ImageType == imageType select img).OrderBy(m => m.ProductID).ToList();
                    return images;
                }
                
            }
        }

        // POST: api/Image
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Image/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Image/5
        public void Delete(int id)
        {
        }
    }
}
