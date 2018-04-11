using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using System.Transactions;

namespace TheAPI.Controllers
{
    //[Authorize]
    [RoutePrefix("api/product")]
    public class ProductController : ApiController
    {
        // GET api/values
        [HttpGet]
        //[Authorize]
        [Route("getproducts/{take}/{skip}")]
        public HttpResponseMessage GetProducts(int take = 20, int skip = 0)
        {
            try
            {
                using (TheDBEntities db = new TheDBEntities())
                {
                    //var rawprod = db.AllProducts.ToList().Take(10).Skip(0);

                    //var model=from pm in db.ProductModels group pm by pm.ProductModelID into gr select gr.OrderBy(m=>m.ProductModelID)
                    var prodlist = (from p in db.AllProducts
                                    join pm in db.ProductModels on p.ProductModelID equals pm.ProductModelID
                                    join pmx in db.ProductModelProductDescriptionCultures on pm.ProductModelID equals pmx.ProductModelID
                                    join pd in db.ProductDescriptions on pmx.ProductDescriptionID equals pd.ProductDescriptionID
                                    join map in db.ProductProductPhotoes on p.ProductID equals map.ProductID
                                    join ph in db.ProductPhotoes on map.ProductPhotoID equals ph.ProductPhotoID
                                    where ph.LargePhotoFileName != "no_image_available_large.gif" && pmx.CultureID == "en"
                                    //group new { p, pm, pmx, pd, map, ph } by new { pm.ProductModelID } into result
                                    //from res in result
                                    select new
                                    {
                                        ProductID = p.ProductID,
                                        ProductName = p.Name,
                                        //ModelName = pm.Name,
                                        CultureID = pmx.CultureID,
                                        Description = pd.Description,
                                        //LargePhoto = ph.LargePhoto,
                                        LargePhotoFileName = ph.LargePhotoFileName,
                                        //ThumbNailPhoto = ph.ThumbNailPhoto,
                                        ThumbnailPhotoFileName = ph.ThumbnailPhotoFileName
                                    }).ToList().OrderBy(m => m.ProductID).Take(take).Skip(skip);

                    
                    if (prodlist != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, prodlist);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Product not found");
                    }
                }
            }
            catch (Exception)
            {

                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }


        }

        // GET api/values/5
        [HttpGet]
        //[Authorize]
        public Product LoadProductById(int id)
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                return db.Products.FirstOrDefault(e => e.ProductID.Equals(id));
            }
        }
        [Route("productsearch/term")]
        [HttpGet]
        public IHttpActionResult SearchProduct(string term)
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                var prodlist = (from c in db.AllProducts
                                where c.Name.Contains(term)
                                select new
                                {
                                    Name = c.Name,
                                    ListPrice = c.ListPrice,
                                    ProductID = c.ProductID,
                                }).ToList();
                if (prodlist != null)
                {
                    return Ok(prodlist);
                }
                else
                {
                    return NotFound();
                }

            }

        }
        // POST api/values
        public void Post([FromBody]Product value)
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                db.Products.Add(value);
                db.SaveChanges();
            }
        }

        // PUT api/values/5
        public HttpResponseMessage Put(int id, [FromBody]Product value)
        {
            try
            {
                using (TheDBEntities db = new TheDBEntities())
                {
                    var prod = db.Products.FirstOrDefault(m => m.ProductID.Equals(id));
                    if (prod != null)
                    {
                        prod.ProductPrice = value.ProductPrice;
                        prod.ProductWeight = value.ProductWeight;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Product with id " + id + " not found");
                    }

                }
            }
            catch (Exception)
            {

                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // DELETE api/values/5
        public HttpResponseMessage Delete(int id)
        {
            try
            {
                using (TheDBEntities db = new TheDBEntities())
                {
                    var prod = db.Products.FirstOrDefault(m => m.ProductID.Equals(id));
                    if (prod != null)
                    {
                        db.Products.Remove(prod);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Product with id " + id + " not found");
                    }

                }
            }
            catch (Exception)
            {

                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }


        }
    }
}
