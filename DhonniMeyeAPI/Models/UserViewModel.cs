using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DhonniMeyeAPI.Models
{
    public class UserAddress
    {
        public int AddressID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int StateProvinceID { get; set; }
        public string PostalCode { get; set; }
        public string Landmark { get; set; }

        public int BusinessEntityID { get; set; }
        public int AddressTypeID { get; set; }
        public string AlternatePhoneNumber { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }

    }
}