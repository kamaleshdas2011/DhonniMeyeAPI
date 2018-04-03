using System;
using System.Net.Mail;
using System.Web.Http;
using DataAccess;

namespace DhonniMeyeAPI.Controllers
{
    [RoutePrefix("api/useractivity")]
    public class UserActivityController : ApiController
    {
        [HttpGet]
        [Route("sendmail")]
        public string SendMail()
        {
            MailMessage message = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            message.From = new MailAddress("kamaleshkritodas@gmail.com");
            message.To.Add("kamaleshkritodas@gmail.com");
            message.Subject = "txt_subject.Text";
            message.Body = "richTextBox1.Text";
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("kamaleshkritodas@gmail.com", "Pass=0000");
            SmtpServer.EnableSsl = false;
            

            //SmtpClient client = new SmtpClient();
            //client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //client.EnableSsl = true;
            //client.Host = "smtp.office365.com";
            //client.Port = 587;
            //// setup Smtp authentication
            //System.Net.NetworkCredential credentials =
            //    new System.Net.NetworkCredential("kamaleshdas2011@outlook.com", "hiAll@0000");
            //client.UseDefaultCredentials = false;
            //client.Credentials = credentials;
            ////can be obtained from your model
            //MailMessage msg = new MailMessage();
            //msg.From = new MailAddress("your_account@gmail.com");
            //msg.To.Add(new MailAddress("kamaleshkritodas@gmail.com"));

            //msg.Subject = "Message from A.info";
            //msg.IsBodyHtml = true;
            //msg.Body = string.Format("<html><head></head><body><b>Message Email</b></body>");
            try
            {
                SmtpServer.Send(message);
                //client.Send(msg);
                return "OK";
            }
            catch (Exception ex)
            {

                return "error:" + ex.ToString();
            }
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
