using System.Web;
using System.Web.Http;

namespace Picking.Controllers
{
    [BasicAuthentication]
    public class AuthorizedApiController : ApiController
    {
        public string AuthorizedUser { get { return ((ApiIdentity)HttpContext.Current.User.Identity).Name; } }
    }
}
