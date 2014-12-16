using System.Collections.Generic;
using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class StorageFacilitiesController : ApiController
    {
        // GET /api/storagefacilities
        public IEnumerable<string> Get()
        {
            return _company.ListStorageFacilities();
        }

        private readonly Company _company = new Company(Company.COMPANY);
    }
}
