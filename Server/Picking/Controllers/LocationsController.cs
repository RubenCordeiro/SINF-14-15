using System.Collections.Generic;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class LocationsController : ApiController
    {
        public LocationsController()
        {
            _company = new Company("BELAFLOR", "", "");
        }

        // GET: /api/locations/
        public IEnumerable<StorageLocation> Get()
        {
            return _company.ListStorageLocations();
        }

        public StorageLocation Get(string id)
        {
            var result = _company.GetStorageLocation(id);
            return result;
        }

        private readonly Company _company;
    }
}
