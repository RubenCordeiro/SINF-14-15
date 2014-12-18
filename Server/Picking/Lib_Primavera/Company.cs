using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ADODB;
using Interop.ErpBS800;
using Interop.GcpBE800;
using Interop.StdBE800;
using Interop.StdPlatBS800;
using Picking.Lib_Primavera.Model;

// ReSharper disable UseIndexedProperty

namespace Picking.Lib_Primavera
{
    public class Company
    {

        #region Initialization

        public const string TargetCompany = "PRIMADELL";

        public Company(string name, string user = "", string password = "")
        {
            _name = name;
            if (!Initialize(user, password))
            {
                throw new InvalidOperationException("Could not initialize company " + name);
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

            _connection = _platform.BaseDados.AbreBaseDadosADO("Default", "PRI" + _name);

            _initialized = true;

            return true;
        }

        #endregion

        
        #region Document Generation

        public string GenerateStockRemovalDocument(PickingItem item, double quantity)
        {
            var data = DateTime.Now;
            var doc = new GcpBEDocumentoStock();

            doc.set_Tipodoc("SS");

            Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);

            doc.set_ArmazemOrigem(item.StorageFacility);
            doc.set_DataDoc(data);

            var lines = new GcpBELinhasDocumentoStock();

            var itemLines = Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: item.Item.Id, Armazem: item.StorageFacility, Localizacao: item.StorageLocation, Quantidade: quantity, EntradaSaida: "S", TipoDocStock: "SS");
            for (var i = 1; i <= itemLines.NumItens; ++i)
            {
                var line = itemLines.get_Edita(i);
                line.set_LocalizacaoOrigem(item.StorageLocation);
                line.set_DataStock(data);

                lines.Insere(line);
            }

            doc.set_Linhas(lines);

            var avisos = String.Empty;
            Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

            return avisos;
        }

        public string GenerateStockTransferDocument(IList<PutawayItem> items)
        {
            var data = DateTime.Now;
            var doc = new GcpBEDocumentoStock();

            doc.set_Tipodoc("TRA");

            Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);

            var facility = items[0].StorageFacility;
            var facilityIn = facility + ".IN";

            doc.set_ArmazemOrigem(facility);
            doc.set_DataDoc(data);

            var lines = new GcpBELinhasDocumentoStock();

            foreach (var item in items)
            {
                var itemLines = Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: item.Item.Id, Armazem: facility, Localizacao: item.StorageLocation, Quantidade: item.Quantity, TipoDocStock: "TRA");
                for (var i = 1; i <= itemLines.NumItens; ++i)
                {
                    var line = itemLines.get_Edita(i);
                    line.set_LocalizacaoOrigem(facilityIn);
                    line.set_DataStock(data);
                    lines.Insere(line);
                }
            }

            doc.set_Linhas(lines);

            var avisos = String.Empty;
            Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

            return avisos;
        }

        public string GenerateStockTransferDocument(IList<PickingItem> items)
        {
            var data = DateTime.Now;
            var doc = new GcpBEDocumentoStock();

            doc.set_Tipodoc("TRA");

            Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);

            var facility = items[0].StorageFacility;
            var facilityOut = facility + ".OUT";

            doc.set_ArmazemOrigem(facility);
            doc.set_DataDoc(data);

            var lines = new GcpBELinhasDocumentoStock();

            foreach (var item in items)
            {
                var itemLines = Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: item.Item.Id, Armazem: facility, Localizacao: facilityOut, Quantidade: item.PickedQuantity, TipoDocStock: "TRA");
                for (var i = 1; i <= itemLines.NumItens; ++i)
                {
                    var line = itemLines.get_Edita(i);
                    line.set_LocalizacaoOrigem(item.StorageLocation);
                    line.set_DataStock(data);
                    lines.Insere(line);
                }
            }

            doc.set_Linhas(lines);

            var avisos = String.Empty;
            Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

            return avisos;
        }

        #endregion

        #region Items

        public Item GetItem(string id)
        {
            EnsureInitialized();

            if (!_engine.Comercial.Artigos.Existe(id)) return null;

            var objArtigo = _engine.Comercial.Artigos.Edita(id);

            return new Item
            {
                Id = objArtigo.get_Artigo(),
                Description = objArtigo.get_Descricao(),
                Volume = objArtigo.get_Volume()
            };
        }

        public IEnumerable<Item> ListItems()
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

        public IEnumerable<ItemStock> ListItemStock()
        {
            EnsureInitialized();

            var result = new List<ItemStock>();

            for (
                var objItemStocksList =
                    _engine.Consulta("SELECT Artigo, ArtigoArmazem.Armazem, StkActual, ArtigoArmazem.Localizacao, CDU_capacity FROM ArtigoArmazem INNER JOIN ArmazemLocalizacoes ON ArtigoArmazem.Localizacao = ArmazemLocalizacoes.Localizacao");
                !objItemStocksList.NoFim();
                objItemStocksList.Seguinte())
            {
                result.Add(new ItemStock
                {
                    Item = objItemStocksList.Valor("Artigo"),
                    Stock = objItemStocksList.Valor("StkActual"),
                    StorageFacility = objItemStocksList.Valor("Armazem"),
                    StorageLocation = objItemStocksList.Valor("Localizacao"),
                    StorageCapacity = objItemStocksList.Valor("CDU_capacity")
                });
            }

            return result;
        }

        #endregion

        #region Facilities & Locations

        public IEnumerable<string> ListStorageFacilities()
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

        public IEnumerable<StorageLocation> ListStorageLocations()
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
                    StorageCapacity = objLocations.Valor("CDU_capacity"),
                    IdParent = objLocations.Valor("IdPai")
                });
            }

            return result;
        }

        #endregion

        #region Orders

        public IEnumerable<Order> ListOrders()
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
                    "SELECT LinhasDoc.idCabecDoc, LinhasDoc.Id, LinhasDoc.NumLinha, Artigo.Artigo, LinhasDoc.Descricao, LinhasDoc.Quantidade, LinhasDoc.Unidade, Volume, LinhasDoc.PrecUnit," +
                    "LinhasDoc.Desconto1, LinhasDoc.TotalILiquido, LinhasDoc.PrecoLiquido, LinhasDoc.CDU_Picked, LinhasDoc.CDU_PickedQuantity from LinhasDoc INNER JOIN Artigo ON Artigo.Artigo = LinhasDoc.Artigo where IdCabecDoc='" +
                    dv.Id + "' order By NumLinha"
                );

                for (; !objListLin.NoFim(); objListLin.Seguinte())
                {
                    var linDv = new OrderLine
                    {
                        Id = objListLin.Valor("Id"),
                        IdCabecDoc = objListLin.Valor("idCabecDoc"),
                        LineNo = objListLin.Valor("NumLinha"),
                        Item = new Item
                        {
                            Id = objListLin.Valor("Artigo"),
                            Description = objListLin.Valor("Descricao"),
                            Volume = objListLin.Valor("Volume")
                        },
                        Quantity = objListLin.Valor("Quantidade"),
                        Unit = objListLin.Valor("Unidade"),
                        Discount = objListLin.Valor("Desconto1"),
                        UnitPrice = objListLin.Valor("PrecUnit"),
                        TotalINet = objListLin.Valor("TotalILiquido"),
                        TotalNet = objListLin.Valor("PrecoLiquido"),
                        Picked = objListLin.Valor("CDU_Picked") == 1,
                        PickedQuantity = objListLin.Valor("CDU_PickedQuantity")
                    };

                    listLinDv.Add(linDv);
                }

                dv.OrderLines = listLinDv;
                listDv.Add(dv);
            }

            return listDv;
        }

        public Order GetOrder(int id)
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta(
                    "SELECT id, Entidade, Data, NumDoc, TotalMerc, Serie From CabecDoc where TipoDoc='ECL' and NumDoc='" +
                    id + "'");

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
                "SELECT LinhasDoc.idCabecDoc, LinhasDoc.Id, LinhasDoc.NumLinha, Artigo.Artigo, LinhasDoc.Descricao, LinhasDoc.Quantidade, LinhasDoc.Unidade, Volume, LinhasDoc.PrecUnit," +
                "LinhasDoc.Desconto1, LinhasDoc.TotalILiquido, LinhasDoc.PrecoLiquido, LinhasDoc.CDU_Picked, LinhasDoc.CDU_PickedQuantity from LinhasDoc INNER JOIN Artigo ON Artigo.Artigo = LinhasDoc.Artigo where IdCabecDoc='" +
                dv.Id + "' order By NumLinha"
            );

            for (; !objListLin.NoFim(); objListLin.Seguinte())
            {
                var lindv = new OrderLine
                {
                    IdCabecDoc = objListLin.Valor("idCabecDoc"),
                    LineNo = objListLin.Valor("NumLinha"),
                    Item = new Item
                    {
                        Id = objListLin.Valor("Artigo"),
                        Description = objListLin.Valor("Descricao"),
                        Volume = objListLin.Valor("Volume")
                    },
                    Quantity = objListLin.Valor("Quantidade"),
                    Unit = objListLin.Valor("Unidade"),
                    Discount = objListLin.Valor("Desconto1"),
                    UnitPrice = objListLin.Valor("PrecUnit"),
                    TotalINet = objListLin.Valor("TotalILiquido"),
                    TotalNet = objListLin.Valor("PrecoLiquido"),
                    Id = objListLin.Valor("Id"),
                    Picked = objListLin.Valor("CDU_Picked") == 1,
                    PickedQuantity = objListLin.Valor("CDU_PickedQuantity")
                };

                listlindv.Add(lindv);
            }

            dv.OrderLines = listlindv;
            return dv;
        }

        #endregion

        #region Supplies

        public IEnumerable<Supply> ListSupplies()
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta("SELECT Id, Entidade, Fornecedores.Nome as EntidadeNome, DataDoc, NumDoc, TotalMerc, Serie FROM CabecCompras INNER JOIN Fornecedores ON Fornecedores.Fornecedor = CabecCompras.Entidade where TipoDoc='ECF'");

            var listDv = new List<Supply>();

            for (; !objListCab.NoFim(); objListCab.Seguinte())
            {
                var dv = new Supply
                {
                    Id = objListCab.Valor("Id"),
                    Entity = objListCab.Valor("Entidade"),
                    EntityName = objListCab.Valor("EntidadeNome"),
                    Data = objListCab.Valor("DataDoc"),
                    NumDoc = objListCab.Valor("NumDoc"),
                    TotalMerc = objListCab.Valor("TotalMerc"),
                    Serie = objListCab.Valor("Serie")
                };

                var listLinDv = new List<SupplyLine>();
                var objListLin = _engine.Consulta(
                    "SELECT LinhasCompras.IdCabecCompras, LinhasCompras.Id, LinhasCompras.NumLinha, LinhasCompras.Artigo, LinhasCompras.Descricao, LinhasCompras.Quantidade, LinhasCompras.Unidade, Volume, LinhasCompras.PrecUnit, LinhasCompras.Desconto1," +
                    "LinhasCompras.TotalILiquido, LinhasCompras.PrecoLiquido, CDU_Putaway, CDU_PutawayQuantity from LinhasCompras INNER JOIN Artigo ON Artigo.Artigo = LinhasCompras.Artigo where IdCabecCompras='" +
                    dv.Id + "' order By NumLinha"
                );

                for (; !objListLin.NoFim(); objListLin.Seguinte())
                {
                    var linDv = new SupplyLine
                    {
                        IdCabecCompras = objListLin.Valor("IdCabecCompras"),
                        Id = objListLin.Valor("Id"),
                        LineNo = objListLin.Valor("NumLinha"),
                        Item = new Item
                        {
                            Id = objListLin.Valor("Artigo"),
                            Description = objListLin.Valor("Descricao"),
                            Volume = objListLin.Valor("Volume")
                        },
                        Quantity = objListLin.Valor("Quantidade"),
                        Unit = objListLin.Valor("Unidade"),
                        Discount = objListLin.Valor("Desconto1"),
                        UnitPrice = objListLin.Valor("PrecUnit"),
                        TotalINet = objListLin.Valor("TotalIliquido"),
                        TotalNet = objListLin.Valor("PrecoLiquido"),
                        Putaway = objListLin.Valor("CDU_Putaway") == 1,
                        PutawayQuantity = objListLin.Valor("CDU_PutawayQuantity")
                    };

                    listLinDv.Add(linDv);
                }

                dv.SupplyLines = listLinDv;
                listDv.Add(dv);
            }

            return listDv;
        }

        public Supply GetSupply(int id)
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta("SELECT Id, Entidade, Fornecedores.Nome as EntidadeNome, DataDoc, NumDoc, TotalMerc, Serie FROM CabecCompras INNER JOIN Fornecedores ON Fornecedores.Fornecedor = CabecCompras.Entidade where TipoDoc='ECF' AND NumDoc='" + id + "'");

            if (objListCab.Vazia())
                return null;

            var dv = new Supply
            {
                Id = objListCab.Valor("Id"),
                Entity = objListCab.Valor("Entidade"),
                EntityName = objListCab.Valor("EntidadeNome"),
                Data = objListCab.Valor("DataDoc"),
                NumDoc = objListCab.Valor("NumDoc"),
                TotalMerc = objListCab.Valor("TotalMerc"),
                Serie = objListCab.Valor("Serie")
            };

            var listLinDv = new List<SupplyLine>();
            var objListLin = _engine.Consulta(
                "SELECT LinhasCompras.IdCabecCompras, LinhasCompras.Id, LinhasCompras.NumLinha, LinhasCompras.Artigo, LinhasCompras.Descricao, LinhasCompras.Quantidade, LinhasCompras.Unidade, Volume, LinhasCompras.PrecUnit, LinhasCompras.Desconto1," +
                    "LinhasCompras.TotalILiquido, LinhasCompras.PrecoLiquido, CDU_Putaway, CDU_PutawayQuantity from LinhasCompras INNER JOIN Artigo ON Artigo.Artigo = LinhasCompras.Artigo where IdCabecCompras='" +
                    dv.Id + "' order By NumLinha"
            );

            for (; !objListLin.NoFim(); objListLin.Seguinte())
            {
                var linDv = new SupplyLine
                {
                    IdCabecCompras = objListLin.Valor("IdCabecCompras"),
                    Id = objListLin.Valor("Id"),
                    LineNo = objListLin.Valor("NumLinha"),
                    Item = new Item
                    {
                        Id = objListLin.Valor("Artigo"),
                        Description = objListLin.Valor("Descricao"),
                        Volume = objListLin.Valor("Volume")
                    },
                    Quantity = objListLin.Valor("Quantidade"),
                    Unit = objListLin.Valor("Unidade"),
                    Discount = objListLin.Valor("Desconto1"),
                    UnitPrice = objListLin.Valor("PrecUnit"),
                    TotalINet = objListLin.Valor("TotalIliquido"),
                    TotalNet = objListLin.Valor("PrecoLiquido"),
                    Putaway = objListLin.Valor("CDU_Putaway") == 1,
                    PutawayQuantity = objListLin.Valor("CDU_PutawayQuantity")
                };

                listLinDv.Add(linDv);
            }

            dv.SupplyLines = listLinDv;

            return dv;
        }

        #endregion

        #region Picking

        public void MarkOrderLinePicked(string orderLineId)
        {
            MarkOrderLinePicked(orderLineId, true);
        }

        public void MarkOrderLinePicked(string orderLineId, bool picked)
        {
            ExecuteQuery("UPDATE LinhasDoc SET CDU_Picked = {0} WHERE Id = '{1}'", picked ? 1 : 0, orderLineId);

            /* old, slow
            var doc = _engine.Comercial.Vendas.EditaID(order.Id);
            var fields = new StdBECampos();
            fields.Insere(new StdBECampo { Nome = "CDU_Picked", Valor = 1 });
            var line = doc.get_Linhas().get_Edita(orderLine.LineNo);
            line.set_CamposUtil(fields);
            _engine.Comercial.Vendas.Actualiza(doc);
             * */
        }

        public void SetOrderLinePickedQuantity(string orderLineId, double quantity)
        {
            ExecuteQuery("UPDATE LinhasDoc SET CDU_PickedQuantity = {0} WHERE Id = '{1}'", quantity, orderLineId);
        }

        public IEnumerable<PickingList> ListPickingLists()
        {
            EnsureInitialized();

            var objListCab = _engine.Consulta("SELECT CDU_id, CDU_date, CDU_pickerName FROM TDU_PickingList");

            var pickingLists = new List<PickingList>();

            for (; !objListCab.NoFim(); objListCab.Seguinte())
            {
                var pickingList = new PickingList
                {
                    Id = objListCab.Valor("CDU_id"),
                    Date = objListCab.Valor("CDU_date"),
                    PickerName = objListCab.Valor("CDU_pickerName")
                };

                var pickingItems = new List<PickingItem>();
                var objListLin = _engine.Consulta(
                    String.Format("SELECT CDU_id, CDU_pickingListId, CDU_itemId, Artigo.Descricao, Artigo.Volume, CDU_storageLocation, CDU_capacity, CDU_quantity, CDU_unit FROM TDU_PickingItems INNER JOIN Artigo ON Artigo.Artigo LIKE CDU_itemId INNER JOIN ArmazemLocalizacoes ON Localizacao LIKE CDU_storageLocation WHERE CDU_pickingListId='{0}' ORDER BY CDU_id",
                    pickingList.Id));

                for (; !objListLin.NoFim(); objListLin.Seguinte())
                {
                    var pickingItem = new PickingItem
                    {
                        Item = new Item
                        {
                            Id = objListLin.Valor("CDU_itemId"),
                            Description = objListLin.Valor("Descricao"),
                            Volume = objListLin.Valor("Volume")
                        },
                        StorageLocation = objListLin.Valor("CDU_storageLocation"),
                        StorageCapacity = objListLin.Valor("CDU_capacity"),
                        Quantity = objListLin.Valor("CDU_quantity"),
                        Unit = objListLin.Valor("CDU_unit")
                    };

                    pickingItem.StorageFacility = ExtractFacility(pickingItem.StorageLocation);
                    pickingItem.PickedQuantity = pickingItem.Quantity;

                    pickingItems.Add(pickingItem);
                }

                pickingList.Items = pickingItems;
                pickingList.SkippedOrders = new List<OrderLine>();
                pickingLists.Add(pickingList);
            }

            return pickingLists;
        }

        public PickingList GetPickingList(int id)
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta(
                    String.Format("SELECT CDU_id, CDU_date, CDU_pickerName FROM TDU_PickingList WHERE CDU_id={0}", id));

            if (objListCab.Vazia())
                return null;

            var pickingList = new PickingList
            {
                Id = objListCab.Valor("CDU_id"),
                Date = objListCab.Valor("CDU_date"),
                PickerName = objListCab.Valor("CDU_pickerName")
            };

            var pickingItems = new List<PickingItem>();
            var objListLin = _engine.Consulta(
                String.Format("SELECT CDU_id, CDU_pickingListId, CDU_itemId, Artigo.Descricao, Artigo.Volume, CDU_storageLocation, CDU_capacity, CDU_quantity, CDU_unit FROM TDU_PickingItems INNER JOIN Artigo ON Artigo.Artigo LIKE CDU_itemId INNER JOIN ArmazemLocalizacoes ON Localizacao LIKE CDU_storageLocation WHERE CDU_pickingListId='{0}' ORDER BY CDU_id",
                pickingList.Id));

            for (; !objListLin.NoFim(); objListLin.Seguinte())
            {
                var pickingItem = new PickingItem
                {
                    Item = new Item
                    {
                        Id = objListLin.Valor("CDU_itemId"),
                        Description = objListLin.Valor("Descricao"),
                        Volume = objListLin.Valor("Volume")
                    },
                    StorageLocation = objListLin.Valor("CDU_storageLocation"),
                    StorageCapacity = objListLin.Valor("CDU_capacity"),
                    Quantity = objListLin.Valor("CDU_quantity"),
                    Unit = objListLin.Valor("CDU_unit")
                };

                pickingItem.StorageFacility = ExtractFacility(pickingItem.StorageLocation);
                pickingItem.PickedQuantity = pickingItem.Quantity;

                pickingItems.Add(pickingItem);
            }

            pickingList.Items = pickingItems;
            pickingList.SkippedOrders = new List<OrderLine>();

            return pickingList;
        }

        public void InsertPickingItems(IEnumerable<PickingItem> items)
        {
            var objListLin = _engine.Consulta(
                "SELECT ISNULL(MAX(CDU_id), 0) as id FROM TDU_PickingList"
            );

            var maxId = (int) objListLin.Valor("id");

            ExecuteQuery("INSERT INTO TDU_PickingList (CDU_id, CDU_date, CDU_pickerName) VALUES ({0}, '{1}', '{2}')",
                maxId + 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), "Zebino");

            var i = 0;
            foreach (var item in items)
            {
                ExecuteQuery(
                    "INSERT INTO TDU_PickingItems (CDU_id, CDU_pickingListId, CDU_itemId, CDU_storageLocation, CDU_quantity," +
                    "CDU_unit) VALUES ({0}, {1}, '{2}', '{3}', {4}, '{5}')", i, maxId + 1, item.Item.Id, 
                        item.StorageLocation, item.PickedQuantity, item.Unit);
                ++i;
            }
        }

        #endregion

        #region Putaway

        public IEnumerable<PutawayList> ListPutawayLists()
        {
            EnsureInitialized();

            var objListCab = _engine.Consulta("SELECT CDU_id, CDU_date, CDU_pickerName FROM TDU_PutawayList");

            var putawayLists = new List<PutawayList>();

            for (; !objListCab.NoFim(); objListCab.Seguinte())
            {
                var putawayList = new PutawayList
                {
                    Id = objListCab.Valor("CDU_id"),
                    Date = objListCab.Valor("CDU_date"),
                    PickerName = objListCab.Valor("CDU_pickerName")
                };

                var putawayItems = new List<PutawayItem>();
                var objListLin = _engine.Consulta(
                    String.Format("SELECT CDU_id, CDU_putawayListId, CDU_itemId, Artigo.Descricao, Artigo.Volume, CDU_storageLocation, CDU_capacity, CDU_quantity, CDU_unit FROM TDU_PutawayItems INNER JOIN Artigo ON Artigo.Artigo LIKE CDU_itemId INNER JOIN ArmazemLocalizacoes ON Localizacao LIKE CDU_storageLocation WHERE CDU_putawayListId='{0}' ORDER BY CDU_id",
                    putawayList.Id));

                for (; !objListLin.NoFim(); objListLin.Seguinte())
                {
                    var putawayItem = new PutawayItem
                    {
                        Item = new Item
                        {
                            Id = objListLin.Valor("CDU_itemId"),
                            Description = objListLin.Valor("Descricao"),
                            Volume = objListLin.Valor("Volume")
                        },
                        StorageLocation = objListLin.Valor("CDU_storageLocation"),
                        Quantity = objListLin.Valor("CDU_quantity"),
                        Unit = objListLin.Valor("CDU_unit")
                    };

                    putawayItem.StorageFacility = ExtractFacility(putawayItem.StorageLocation);
                    putawayItem.PutawayQuantity = putawayItem.Quantity;

                    putawayItems.Add(putawayItem);
                }

                putawayList.Items = putawayItems;
                putawayList.SkippedSupplies = new List<SupplyLine>();
                putawayLists.Add(putawayList);
            }

            return putawayLists;
        }

        public PutawayList GetPutawayList(int id)
        {
            EnsureInitialized();

            var objListCab =
                _engine.Consulta(
                    String.Format("SELECT CDU_id, CDU_date, CDU_pickerName FROM TDU_PutawayList WHERE CDU_id={0}", id));

            if (objListCab.Vazia())
                return null;

            var putawayList = new PutawayList
            {
                Id = objListCab.Valor("CDU_id"),
                Date = objListCab.Valor("CDU_date"),
                PickerName = objListCab.Valor("CDU_pickerName")
            };

            var putawayItems = new List<PutawayItem>();
            var objListLin = _engine.Consulta(
                    String.Format("SELECT CDU_id, CDU_putawayListId, CDU_itemId, Artigo.Descricao, Artigo.Volume, CDU_storageLocation, CDU_capacity, CDU_quantity, CDU_unit FROM TDU_PutawayItems INNER JOIN Artigo ON Artigo.Artigo LIKE CDU_itemId INNER JOIN ArmazemLocalizacoes ON Localizacao LIKE CDU_storageLocation WHERE CDU_putawayListId='{0}' ORDER BY CDU_id",
                    putawayList.Id));

            for (; !objListLin.NoFim(); objListLin.Seguinte())
            {
                var putawayItem = new PutawayItem
                {
                    Item = new Item
                    {
                        Id = objListLin.Valor("CDU_itemId"),
                        Description = objListLin.Valor("Descricao"),
                        Volume = objListLin.Valor("Volume")
                    },
                    StorageLocation = objListLin.Valor("CDU_storageLocation"),
                    Quantity = objListLin.Valor("CDU_quantity"),
                    Unit = objListLin.Valor("CDU_unit")
                };

                putawayItem.StorageFacility = ExtractFacility(putawayItem.StorageLocation);
                putawayItem.PutawayQuantity = putawayItem.Quantity;

                putawayItems.Add(putawayItem);
            }

            putawayList.Items = putawayItems;
            putawayList.SkippedSupplies = new List<SupplyLine>();

            return putawayList;
        }

        public void MarkSupplyLinePutaway(string supplyLineId)
        {
            MarkSupplyLinePutaway(supplyLineId, true);
        }

        public void MarkSupplyLinePutaway(string supplyLineId, bool putaway)
        {
            ExecuteQuery("UPDATE LinhasCompras SET CDU_Putaway = {0} WHERE Id = '{1}'", putaway ? 1 : 0, supplyLineId);
        }

        public void InsertPutawayItems(IEnumerable<PutawayItem> items)
        {
            var objListLin = _engine.Consulta(
                "SELECT ISNULL(MAX(CDU_id), 0) as id FROM TDU_PutawayList"
            );

            var maxId = (int)objListLin.Valor("id");

            ExecuteQuery("INSERT INTO TDU_PutawayList (CDU_id, CDU_date, CDU_pickerName) VALUES ({0}, '{1}', '{2}')",
                maxId + 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), "Zebino");

            var i = 0;
            foreach (var item in items)
            {
                ExecuteQuery(
                    "INSERT INTO TDU_PutawayItems (CDU_id, CDU_putawayListId, CDU_itemId, CDU_storageLocation, CDU_quantity," +
                    "CDU_unit) VALUES ({0}, {1}, '{2}', '{3}', {4}, '{5}')", i, maxId + 1, item.Item.Id,
                        item.StorageLocation, item.Quantity /* item.PutawayQuantity */, item.Unit);
                ++i;
            }
        }

        #endregion

        #region Authentication

        public bool Login(string username, string password)
        {
            try
            {
                var passwordHash = _platform.Criptografia.Encripta(password, 50);

                var list = _engine.Consulta(String.Format("SELECT * FROM TDU_Pickers WHERE CDU_username = '{0}' AND CDU_passwordHash LIKE '{1}'", username, passwordHash));
                return list.NumLinhas() > 0;
            }
            catch (COMException)
            {
                return false;
            }
        }

        public bool Register(string username, string password)
        {
            try
            {
                var passwordHash = _platform.Criptografia.Encripta(password, 50);

                var noChanges = ExecuteQuery(String.Format("INSERT INTO TDU_Pickers (CDU_username, CDU_passwordHash) VALUES ('{0}', '{1}')", username, passwordHash));
                return noChanges > 0;
            }
            catch (COMException)
            {
                return false;
            }
        }

        #endregion

        #region Utilities

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("Company not initialized!");
        }

        public static string ExtractFacility(string location)
        {
            if (!String.IsNullOrWhiteSpace(location))
            {
                var splits = location.Split('.');
                if (splits.Length > 0)
                    return splits[0];
            }

            return String.Empty;
        }

        public int ExecuteQuery(string query, params object[] values)
        {
            return ExecuteQuery(String.Format(query, values));
        }

        public int ExecuteQuery(string query)
        {
            object count;
            _connection.Execute(query, out count);
            return (int)count;
        }

        public static string GetClosestLocation(IEnumerable<string> locations, string location)
        {
            var d = Double.PositiveInfinity;
            string result = null;

            var loc1 = LocationHelper.FromString(location);

            foreach (var s in locations)
            {
                var loc2 = LocationHelper.FromString(s);
                var dist = LocationHelper.GetDistance(loc1, loc2);

                if (dist < d)
                {
                    result = s;
                    d = dist;
                }
            }

            return result;
        }

        public static ItemStock GetClosestLocation(IEnumerable<ItemStock> stock, ItemStock previousStockLocation)
        {
            var d = double.PositiveInfinity;
            ItemStock result = null;

            var loc1 = LocationHelper.FromString(previousStockLocation.StorageLocation);

            foreach (var s in stock)
            {
                var loc2 = LocationHelper.FromString(s.StorageLocation);
                var dist = LocationHelper.GetDistance(loc1, loc2);

                if (dist < d)
                {
                    result = s;
                    d = dist;
                }
            }

            return result;
        }

        #endregion

        #region Data

        private bool _initialized;
        private string _name;
        private readonly StdPlatBS _platform = new StdPlatBS();
        private readonly ErpBS _engine = new ErpBS();
        private Connection _connection;

        public ErpBS Engine { get { return _engine;  } }

        #endregion
    }
}
