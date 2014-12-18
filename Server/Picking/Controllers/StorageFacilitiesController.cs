using System.Collections.Generic;

namespace Picking.Controllers
{
    public class StorageFacilitiesController : AuthorizedApiController
    {
        // GET /api/storagefacilities
        public IEnumerable<string> Get()
        {
            return _company.ListStorageFacilities();
        }
    }
}
