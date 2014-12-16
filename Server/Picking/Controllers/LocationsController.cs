using System.Collections.Generic;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class LocationsController : ApiController
    {
        // GET /api/locations/
        public IEnumerable<StorageLocation> Get()
        {
            return _company.ListStorageLocations();
        }

        // GET /api/locations/<id>
        public StorageLocation Get(string id)
        {
            return _company.GetStorageLocation(id);
        }

        private readonly Company _company = new Company(Company.COMPANY);
    }
}
