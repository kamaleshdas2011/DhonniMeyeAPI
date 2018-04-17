using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using DhonniMeyeAPI.Models;
using DhonniMeyeAPI.Providers;
using DhonniMeyeAPI.Results;
using DataAccess;
using System.Linq;
using System.Net;

namespace DhonniMeyeAPI.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
        private TheDBEntities _dbContext;
        public AccountController()
        {
            _dbContext = new TheDBEntities();
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // POST api/Account/Logout
        [HttpPost]
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser()
            {
                UserName = model.Email,
                Email = model.Email,
            };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            else
            {
                var newBeId = _dbContext.BusinessEntity.Max(m => m.BusinessEntityID) + 1;
                _dbContext.BusinessEntity.Add(new BusinessEntity
                {
                    BusinessEntityID = newBeId,
                    rowguid = Guid.NewGuid(),
                    ModifiedDate = DateTime.Now
                });
                _dbContext.Person.Add(new Person
                {
                    rowguid = Guid.Parse(user.Id),
                    ModifiedDate = DateTime.Now,
                    EmailPromotion = 1,
                    FirstName = "",
                    LastName = "",
                    NameStyle = false,
                    PersonType = "IN",
                    BusinessEntityID = newBeId
                });
                _dbContext.SaveChanges();
            }
            return Ok<string>("success");
        }
        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            return Ok();
        }

        [HttpPost]
        [Route("addnewaddress")]
        public IHttpActionResult AddAddress(UserAddress address)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var user = UserManager.FindById(User.Identity.GetUserId());
                var userid = user.Id;
                var beid = _dbContext.Person.Where(p => p.rowguid.ToString() == userid).ToList().FirstOrDefault().BusinessEntityID;

                var newAddId = _dbContext.BusinessEntityAddress.Max(p => p.AddressID) + 1;

                _dbContext.BusinessEntityAddress.Add(new BusinessEntityAddress
                {
                    AddressID = newAddId,
                    BusinessEntityID = beid,
                    AddressTypeID = address.AddressTypeID,
                    ModifiedDate = DateTime.Now,
                    rowguid = Guid.NewGuid(),
                });
                _dbContext.Address.Add(new Address
                {
                    AddressID = newAddId,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City = address.City,
                    PostalCode = address.PostalCode,
                    StateProvinceID = address.StateProvinceID,
                    ModifiedDate = DateTime.Now,
                    rowguid = Guid.NewGuid(),
                    AlternatePhoneNumber=address.AlternatePhoneNumber,
                    Landmark=address.Landmark,
                    Name=address.Name,
                    PhoneNumber=address.PhoneNumber
                });
                _dbContext.SaveChanges();
                return Ok("Success");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("getaddresstypes")]
        public IHttpActionResult GetAddressTypes()
        {
            var types = (from add in _dbContext.AddressType
                         where add.Name == "Home" || add.Name == "Office"
                         select new
                         {
                             Name = add.Name,
                             Code = add.AddressTypeID
                         });
            if (types!=null)
            {
                return Ok(types);
            }
            else
            {
                return NotFound();
            }
        }
        [Route("getstates")]
        public IHttpActionResult GetStates()
        {
            var states = (from add in _dbContext.StateProvince
                          where add.CountryRegionCode == "IN"
                          orderby add.Name
                          select new
                          {
                              Name = add.Name,
                              Code = add.StateProvinceCode
                          });
            if (states != null)
            {
                return Ok(states);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost]
        [Route("editaccount")]
        public async Task<IHttpActionResult> EditAccount(UserUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the existing user from the userdb
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var userid = user.Id;
            user.UserName = model.Email;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await UserManager.UpdateAsync(user);



            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            else
            {
                // udate the existing user from the thedb "Person"
                //var userid = user.Id;
                //var beid = 1;//(from per in db.Person where per.rowguid.ToString() == User.Identity.GetUserId() select per).FirstOrDefault().BusinessEntityID;
                var beid = _dbContext.Person.Where(p => p.rowguid.ToString() == userid).ToList().FirstOrDefault().BusinessEntityID;

                //update person basic
                var person = _dbContext.Person.Where(p => p.rowguid.ToString() == user.Id).FirstOrDefault();
                person.FirstName = model.FirstName;
                person.MiddleName = model.MiddleName;
                person.LastName = model.LastName;

                //insert/update phone details
                if (_dbContext.PersonPhone.Any(m => m.BusinessEntityID == beid))
                {
                    var ph = _dbContext.PersonPhone.Where(m => m.BusinessEntityID == beid).FirstOrDefault();
                    ph.PhoneNumber = model.PhoneNumber;
                }
                else
                {
                    _dbContext.PersonPhone.Add(new PersonPhone
                    {
                        BusinessEntityID = beid,
                        ModifiedDate = DateTime.Now,
                        PhoneNumber = model.PhoneNumber,
                        PhoneNumberTypeID = 1,
                    });
                }
                _dbContext.SaveChanges();
                //insert/update address
            }
            return Ok<string>("success");





        }

        [Route("accountinfo")]
        public HttpResponseMessage GetAccountInfo()
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                var user = UserManager.FindById(User.Identity.GetUserId());

                var userInfo = (from per in db.Person.Where(p => p.rowguid.ToString() == user.Id)
                                    //join bent in db.BusinessEntities on per.BusinessEntityID equals bent.BusinessEntityID where per.rowguid.ToString() == user.Id
                                join bentadd in db.BusinessEntityAddress on per.BusinessEntityID equals bentadd.BusinessEntityID
                                into left1
                                from lef1 in left1.DefaultIfEmpty()
                                    //where per.rowguid.ToString() == user.Id
                                //join addr in db.Addresses on lef1.AddressID equals addr.AddressID
                                //into left2
                                //from lef2 in left2.DefaultIfEmpty()
                                join phone in db.PersonPhone on per.BusinessEntityID equals phone.BusinessEntityID
                                into left3
                                from lef3 in left3.DefaultIfEmpty()
                                join addtype in db.AddressType on lef1.AddressTypeID equals addtype.AddressTypeID
                                into left4
                                from lef4 in left4.DefaultIfEmpty()
                                    //where lef4.Name == "Primary"
                                //join state in db.StateProvinces on lef2.StateProvinceID equals state.StateProvinceID
                                //into left5
                                //from lef5 in left5.DefaultIfEmpty()
                                select new
                                {
                                    Email = user.Email,
                                    Title = per.Title,
                                    FirstName = per.FirstName,
                                    LastName = per.LastName,
                                    MiddleName = per.MiddleName,
                                    PhoneNumber = lef3.PhoneNumber,
                                    //AddressLine1 = lef2.AddressLine1,
                                    //AddressLine2 = lef2.AddressLine2,
                                    //City = lef2.City,
                                    //PostalCode = lef2.PostalCode,
                                    //State = lef5.Name,
                                    //StateCode = lef5.StateProvinceCode,
                                    AddressType = lef4.Name,
                                }).ToList().FirstOrDefault();
                if (userInfo != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, userInfo);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found.");
                }



            }


        }
        [Route("getalladdress")]
        public HttpResponseMessage GetAllAddress()
        {
            using (TheDBEntities db = new TheDBEntities())
            {
                var user = UserManager.FindById(User.Identity.GetUserId());

                var userInfo = (from per in db.Person
                                    //join bent in db.BusinessEntities on per.BusinessEntityID equals bent.BusinessEntityID where per.rowguid.ToString() == user.Id
                                join bentadd in db.BusinessEntityAddress on per.BusinessEntityID equals bentadd.BusinessEntityID
                                where per.rowguid.ToString() == user.Id
                                join addr in db.Address on bentadd.AddressID equals addr.AddressID
                                join phone in db.PersonPhone on per.BusinessEntityID equals phone.BusinessEntityID
                                join addtype in db.AddressType on bentadd.AddressTypeID equals addtype.AddressTypeID
                                //where addtype.Name == "Primary"
                                join state in db.StateProvince on addr.StateProvinceID equals state.StateProvinceID
                                select new
                                {
                                    //Email = user.Email,
                                    //Title = per.Title,
                                    FirstName = addr.Name,
                                    //LastName = per.LastName,
                                    //MiddleName = per.MiddleName,
                                    PhoneNumber = addr.PhoneNumber,
                                    AddressLine1 = addr.AddressLine1,
                                    AddressLine2 = addr.AddressLine2,
                                    City = addr.City,
                                    PostalCode = addr.PostalCode,
                                    State = state.Name,
                                    StateCode = state.StateProvinceCode,
                                    AddressType = addtype.Name,
                                }).ToList();
                if (userInfo != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, userInfo);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found.");
                }



            }


        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
