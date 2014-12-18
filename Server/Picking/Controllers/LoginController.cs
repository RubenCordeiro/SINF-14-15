using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class LoginController : ApiController
    {
        // POST /api/login
        public string Post(UserPassword loginInfo)
        {
            if (_company.Login(loginInfo.Username, loginInfo.Password))
            {
                var tokenContents = Encoding.UTF8.GetBytes(loginInfo.Username + ":" + loginInfo.Password);
                return Convert.ToBase64String(tokenContents);
            }

            throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized));
        }

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}