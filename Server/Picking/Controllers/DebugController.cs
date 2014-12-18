using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class DebugController : AuthorizedApiController
    {
        // GET /api/debug/<action>
        public string Get(string action)
        {
            switch (action)
            {
                case "reset_picked":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_Picked = 0").ToString();
                case "set_picked":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_Picked = 1").ToString();
                case "reset_pickedq":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_PickedQuantity = 0").ToString();
                case "set_pickedq":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_PickedQuantity = Quantidade").ToString();
                case "reset_putaway":
                    return _company.ExecuteQuery("UPDATE LinhasCompras SET CDU_Putaway = 0").ToString();
                case "set_putaway":
                    return _company.ExecuteQuery("UPDATE LinhasCompras SET CDU_Putaway = 1").ToString();
                case "reset_putawayq":
                    return _company.ExecuteQuery("UPDATE LinhasCompras SET CDU_PutawayQuantity = 0").ToString();
                case "set_putawayq":
                    return _company.ExecuteQuery("UPDATE LinhasCompras SET CDU_PutawayQuantity = Quantidade").ToString();
                case "returns_401":
                    throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized));
                case "returns_404":
                    throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
                default:
                    return "Action " + action + " not handled.";
            }
        }
    }
}
