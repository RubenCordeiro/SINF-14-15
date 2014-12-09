using System;
using System.Diagnostics;
using System.Web.Http;
using Interop.GcpBE800;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class HelloController : ApiController
    {

        public string Get()
        {

            var data = DateTime.Now;
            var doc = new GcpBEDocumentoStock();

            doc.set_Tipodoc("TRA");

            _company.Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);

            doc.set_ArmazemOrigem("A1"); // TODO: ArmazemOrigem comes from Order ? or from parameters
            doc.set_DataDoc(data);

            var lines = new GcpBELinhasDocumentoStock();

            var itemLines = _company.Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: "FC.0002", Armazem: "A1", Quantidade: 10D, Localizacao: "A1.S2.P1");

            for (var i = 1; i <= itemLines.NumItens; ++i)
            {
                var line = itemLines.get_Edita(i);
                line.set_LocalizacaoOrigem("A1.S1.P3");
                line.set_DataStock(data);

                lines.Insere(line);
            }

            doc.set_Linhas(lines);

            var avisos = "";

            var watch = new Stopwatch();
            watch.Start();

            _company.Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

            watch.Stop();

            return string.Format("It took {0} ms. Warnings: {1}", watch.ElapsedMilliseconds, avisos);
        }

        private readonly Company _company = new Company("BELAFLOR", "", "");
    }
}
