using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Web.Http;
using Interop.GcpBE800;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class PickingListsController : ApiController
    {
        public PickingListsController()
        {
            _company = new Company("BELAFLOR", "", "");
        }

        public string Get()
        {
            
            try
            {
                var data = DateTime.Now;

                var doc = new GcpBEDocumentoStock();

                doc.set_Tipodoc("TRA");

                _company.Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);
                
                doc.set_ArmazemOrigem("A1"); // TODO: ArmazemOrigem comes from Order ? or from parameters
                doc.set_DataDoc(DateTime.Now);

                var lines = new GcpBELinhasDocumentoStock();

                var itemLines = _company.Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: "FC.0002", Armazem: "A1", Quantidade: 10D, Localizacao: "A1.S2.P1");

                // _company.Engine.Comercial.Stocks.AdicionaLinha(ClsDocStk: doc, Artigo: "FC.0002", Armazem: "A1", Quantidade: 10D, Localizacao: "A1.S2.P1");


                for (var i = 1; i <= itemLines.NumItens; ++i)
                {
                    var line = itemLines.get_Edita(i);
                    line.set_LocalizacaoOrigem("A1.S1.P3");
                    line.set_DataStock(data);

                    lines.Insere(line);
                }

                doc.set_Linhas(lines);

                var avisos = "";

                _company.Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

                return avisos;

            }
            catch (COMException ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, "COMException: " + ex.ToString()));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, "Exception: " + ex.ToString()));
            }
        }

        private readonly Company _company;
    }
}
