using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class ItemsController : AuthorizedApiController
    {
        // GET /items/
        public IEnumerable<Item> Get()
        {
            return _company.ListItems();
        }

        // GET api/items/<id>
        public Item Get(string id)
        {
            var artigo = _company.GetItem(id);

            if (artigo == null) 
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return artigo;
        }
    }
}
