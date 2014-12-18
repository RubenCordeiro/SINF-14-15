using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class StocksController : AuthorizedApiController
    {
        // GET /api/stocks
        public IEnumerable<ItemStock> Get()
        {
            try
            {
                return _company.ListItemStock();
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }
    }
}
