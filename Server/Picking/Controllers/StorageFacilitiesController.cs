using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class StorageFacilitiesController : ApiController
    {
        public IEnumerable<string> Get()
        {
            return _company.ListStorageFacilities();
        }

        private readonly Company _company = new Company("BELAFLOR", "", "");
    }
}
