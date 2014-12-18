using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class RegisterController : ApiController
    {
        // POST /api/register
        public string Post(UserPassword loginInfo)
        {
            if (!_company.Register(loginInfo.Username, loginInfo.Password))
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized));

            var tokenContents = Encoding.UTF8.GetBytes(loginInfo.Username + ":" + loginInfo.Password);
            return Convert.ToBase64String(tokenContents);
        }

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}