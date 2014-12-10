﻿using System;
using System.Collections.Generic;
using Interop.ErpBS800;
using Interop.GcpBE800;
using Interop.StdBE800;
using Interop.StdPlatBS800;
using Picking.Lib_Primavera.Model;

namespace Picking.Lib_Primavera
{
    public class Company
    {
        public Company(string name)
        {
            _name = name;
        }

        public Company(string name, string user, string password)
        {
            _name = name;
            if (!Initialize(user, password))
            {
                throw new Exception("Could not intialize company " + name);
            }
        }

        public bool Initialize(string user, string password)
        {
            if (_initialized) return true;

            var objAplConf = new StdBSConfApl
            {
                Instancia = "Default",
                AbvtApl = "GCP",
                Utilizador = user,
                PwdUtilizador = password
            };

            var objTipoPlataforma = EnumTipoPlataforma.tpProfissional;

            var objStdTransac = new StdBETransaccao();


            _platform.AbrePlataformaEmpresaIntegrador(
                ref _name,
                ref objStdTransac,
                ref objAplConf,
                ref objTipoPlataforma
                );

            if (!_platform.Inicializada) return false;

            var blnModoPrimario = true;

            _engine.AbreEmpresaTrabalho(
                EnumTipoPlataforma.tpProfissional,
                ref _name,
                ref user,
                ref password,
                ref objStdTransac,
                "Default",
                ref blnModoPrimario
                );

            _initialized = true;

            return true;
        }

        public Item GetItem(string id)
        {
            EnsureInitialized();

            if (!_engine.Comercial.Artigos.Existe(id)) return null;

            var objArtigo = _engine.Comercial.Artigos.Edita(id);

            return new Item
            {
                Id = objArtigo.get_Artigo(),
                Description = objArtigo.get_Descricao()
            };
        }

        public List<Item> ListItems()
        {
            EnsureInitialized();

            var result = new List<Item>();

            for (var objList = _engine.Comercial.Artigos.LstArtigos(); !objList.NoFim(); objList.Seguinte())
            {
                var art = new Item
                {
                    Id = objList.Valor("artigo"),
                    Description = objList.Valor("descricao")
                };

                result.Add(art);
            }

            return result;
        }

        public StorageLocation GetStorageLocation(string location)
        {
            EnsureInitialized();

            if (!_engine.Comercial.ArmazemLocalizacao.Existe(location)) return null;

            var objLocation = _engine.Comercial.ArmazemLocalizacao.Edita(location);

            return new StorageLocation
            {
                Id = objLocation.get_ID(),
                Location = objLocation.get_Localizacao(),
                StorageFacility = objLocation.get_Armazem(),
                Description = objLocation.get_Descricao(),
                IdParent = objLocation.get_IdPai()
            };
        }

        public List<string> ListStorageFacilities()
        {
            EnsureInitialized();

            var result = new List<string>();
            for (var objFacilities = _engine.Comercial.Armazens.LstArmazens();
                    !objFacilities.NoFim();
                    objFacilities.Seguinte())
            {
                result.Add(objFacilities.Valor("Armazem"));
            }

            return result;
        }

        public List<StorageLocation> ListStorageLocations()
        {
            EnsureInitialized();

            var result = new List<StorageLocation>();

            for (var objLocations = _engine.Comercial.ArmazemLocalizacao.LstLocalizacoes();
                !objLocations.NoFim();
                objLocations.Seguinte())
            {
                result.Add(new StorageLocation
                {
                    Id = objLocations.Valor("Id"),
                    Location = objLocations.Valor("Localizacao"),
                    StorageFacility = objLocations.Valor("Armazem"),
                    Description = objLocations.Valor("Descricao"),
                    IdParent = objLocations.Valor("IdPai")
                });
            }

            return result;
        } 

        public Order GetOrder(int numdoc)
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta(
                    "SELECT id, Entidade, Data, NumDoc, TotalMerc, Serie From CabecDoc where TipoDoc='ECL' and NumDoc='" +
                    numdoc + "'");

            var dv = new Order
            {
                Id = objListCab.Valor("id"),
                Entity = objListCab.Valor("Entidade"),
                NumDoc = objListCab.Valor("NumDoc"),
                Data = objListCab.Valor("Data"),
                TotalMerc = objListCab.Valor("TotalMerc"),
                Serie = objListCab.Valor("Serie")
            };

            var listlindv = new List<OrderLine>();           
            
            var objListLin = _engine.Consulta(
                "SELECT idCabecDoc, Artigo, Descricao, Quantidade, Unidade, PrecUnit, Desconto1, TotalILiquido, PrecoLiquido from LinhasDoc where IdCabecDoc='" +
                dv.Id + "' order By NumLinha");

            for (; !objListLin.NoFim(); objListLin.Seguinte())
            {
                var lindv = new OrderLine
                {
                    IdCabecDoc = objListLin.Valor("idCabecDoc"),
                    ItemId = objListLin.Valor("Artigo"),
                    ItemDescription = objListLin.Valor("Descricao"),
                    Quantity = objListLin.Valor("Quantidade"),
                    Unit = objListLin.Valor("Unidade"),
                    Discount = objListLin.Valor("Desconto1"),
                    UnitPrice = objListLin.Valor("PrecUnit"),
                    TotalINet = objListLin.Valor("TotalILiquido"),
                    TotalNet = objListLin.Valor("PrecoLiquido"),
                };

                listlindv.Add(lindv);
            }

            dv.OrderLines = listlindv;
            return dv;
        }

        public List<Order> ListOrders()
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta("SELECT id, Entidade, Clientes.Nome as EntidadeNome, Data, NumDoc, TotalMerc, Serie FROM CabecDoc INNER JOIN Clientes ON Clientes.Cliente = CabecDoc.Entidade where TipoDoc='ECL'");

            var listDv = new List<Order>();

            for (; !objListCab.NoFim(); objListCab.Seguinte())
            {
                var dv = new Order
                {
                    Id = objListCab.Valor("id"),
                    Entity = objListCab.Valor("Entidade"),
                    EntityName = objListCab.Valor("EntidadeNome"),
                    NumDoc = objListCab.Valor("NumDoc"),
                    Data = objListCab.Valor("Data"),
                    TotalMerc = objListCab.Valor("TotalMerc"),
                    Serie = objListCab.Valor("Serie")
                };

                var listLinDv = new List<OrderLine>();
                var objListLin = _engine.Consulta(
                    "SELECT idCabecDoc, Artigo, Descricao, Quantidade, Unidade, PrecUnit, Desconto1, TotalILiquido, PrecoLiquido from LinhasDoc where IdCabecDoc='" +
                    dv.Id + "' order By NumLinha"
                    );

                for (; !objListLin.NoFim(); objListLin.Seguinte())
                {
                    var linDv = new OrderLine
                    {
                        IdCabecDoc = objListLin.Valor("idCabecDoc"),
                        ItemId = objListLin.Valor("Artigo"),
                        ItemDescription = objListLin.Valor("Descricao"),
                        Quantity = objListLin.Valor("Quantidade"),
                        Unit = objListLin.Valor("Unidade"),
                        Discount = objListLin.Valor("Desconto1"),
                        UnitPrice = objListLin.Valor("PrecUnit"),
                        TotalINet = objListLin.Valor("TotalILiquido"),
                        TotalNet = objListLin.Valor("PrecoLiquido")
                    };

                    listLinDv.Add(linDv);
                }

                dv.OrderLines = listLinDv;
                listDv.Add(dv);
            }

            return listDv;
        }

        public List<ItemStock> ListItemStock()
        {
            EnsureInitialized();

            var result = new List<ItemStock>();

            for (
                var objItemStocksList =
                    _engine.Consulta("SELECT Artigo, Armazem, StkActual, Localizacao FROM ArtigoArmazem");
                !objItemStocksList.NoFim();
                objItemStocksList.Seguinte())
            {
                result.Add(new ItemStock
                {
                    Item = objItemStocksList.Valor("Artigo"),
                    Stock = objItemStocksList.Valor("StkActual"),
                    StorageFacility = objItemStocksList.Valor("Armazem"),
                    StorageLocation = objItemStocksList.Valor("Localizacao"),
                });
            }

            return result;
        }

        public bool CreateStorageTransferDocument(Order order)
        {
            EnsureInitialized();

            // _engine.Comercial.Stocks.
            var doc = new GcpBEDocumentoStock();
            doc.set_Tipodoc("TRA");
            doc.set_ArmazemOrigem("A1"); // TODO: ArmazemOrigem comes from Order ? or from parameters

            var lines = new GcpBELinhasDocumentoStock();
            
            foreach (var orderLine in order.OrderLines)
            {
                _engine.Comercial.Stocks.AdicionaLinha(doc, orderLine.ItemId, "", orderLine.Quantity, "A1",
                    orderLine.UnitPrice, orderLine.Discount, "", "A1.S1.P3");
            }

            _engine.IniciaTransaccao();
            _engine.Comercial.Stocks.Actualiza(doc);
            _engine.TerminaTransaccao();

            return true;
        }

        private void EnsureInitialized()
        {
            if (!_initialized) throw new Exception("Company not initialized!");
        }

        private bool _initialized = false;
        private string _name;
        private readonly StdPlatBS _platform = new StdPlatBS();
        private readonly ErpBS _engine = new ErpBS();

        public ErpBS Engine { get { return _engine;  } }
    }
}
