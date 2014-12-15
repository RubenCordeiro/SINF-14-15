using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class OrdersController : ApiController
    {
        // GET /orders/
        public IEnumerable<Order> Get()
        {
            return _company.ListOrders();
        }

        // GET /orders/<id>
        public Order Get(int id)
        {
            var docvenda = _company.GetOrder(id);
            if (docvenda == null)
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return docvenda;
        }

        private readonly Company _company = new Company("BELAFLOR");
    }
}
