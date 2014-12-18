using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        // GET /api/locations/<type>
        public IEnumerable<string> Get(string type)
        {
            switch (type)
            {
                case "full":
                    return _company.ListStorageLocations()
                        .Where(location => Location.FromString(location.Location) != null)
                        .Select(location => location.Location);
                default:
                    throw new HttpRequestValidationException(type);
            }
        }

        // GET /api/locations/<id>
        //public StorageLocation Get(string id)
        //{
        //    return _company.GetStorageLocation(id);
        //}

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}
