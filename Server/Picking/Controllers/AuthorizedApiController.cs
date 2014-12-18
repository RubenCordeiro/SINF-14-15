using System.Web;
using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    [BasicAuthentication]
    public class AuthorizedApiController : ApiController
    {
        public string AuthorizedUser { get { return ((ApiIdentity)HttpContext.Current.User.Identity).Name; } }

        protected readonly Company _company = new Company(Company.TargetCompany);
    }
}
