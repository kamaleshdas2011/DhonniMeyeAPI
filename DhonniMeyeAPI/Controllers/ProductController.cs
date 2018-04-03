﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using System.Transactions;

namespace TheAPI.Controllers
{
    public class ProductController : ApiController
    {
        // GET api/values
        [HttpGet]
        //[Authorize]
        public HttpResponseMessage LoadProducts()
        {
            try
            {
                using (TheDBEntities db = new TheDBEntities())
                {
                    var prod = db.Products.ToList();
                    if (prod != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, prod);
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