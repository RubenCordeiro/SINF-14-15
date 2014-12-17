using System;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class BasicAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization == null)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            else
            {
                var authToken = actionContext.Request.Headers.Authorization.Parameter;
                var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(authToken));

                var username = decodedToken.Substring(0, decodedToken.IndexOf(":", StringComparison.Ordinal));
                var password = decodedToken.Substring(decodedToken.IndexOf(":", StringComparison.Ordinal) + 1);

                if (_company.Login(username, password))
                {
                    HttpContext.Current.User = new GenericPrincipal(new ApiIdentity(username), new string[] { });
                    base.OnActionExecuting(actionContext);
                }
                else
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
        }

        private readonly Company _company = new Company(Company.COMPANY);
    }
}