using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class ItemsController : ApiController
    {
        public ItemsController()
        {
            _company = new Company("BELAFLOR", "", "");
        }

        // GET: /items/
        public IEnumerable<Item> Get()
        {
            return _company.ListItems();
        }

        // GET: api/items/5
        public Item Get(string id)
        {
            var artigo = _company.GetItem(id);

            if (artigo == null) 
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return artigo;
        }

        private readonly Company _company;
    }
}
