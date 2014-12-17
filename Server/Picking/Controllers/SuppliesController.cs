using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class SuppliesController : ApiController
    {
        // GET /supplies/
        public IEnumerable<Supply> Get()
        {
            return _company.ListSupplies();
        }

        // GET /supplies/<id>
        public Supply Get(int id)
        {
            var supply = _company.GetSupply(id);
            if (supply == null)
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return supply;
        }

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}
