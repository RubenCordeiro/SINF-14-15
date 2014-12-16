using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class DebugController : ApiController
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
                default:
                    return "Action " + action + " not handled.";
            }
        }

        private readonly Company _company = new Company(Company.COMPANY);
    }
}
