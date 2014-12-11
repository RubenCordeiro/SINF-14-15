﻿using System.Web.Http;
using Picking.Lib_Primavera;

namespace Picking.Controllers
{
    public class DebugController : ApiController
    {
        public string Get(string action)
        {
            switch (action)
            {
                case "reset_picked":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_Picked = 0").ToString();
                case "set_picked":
                    return _company.ExecuteQuery("UPDATE LinhasDoc SET CDU_Picked = 1").ToString();
                default:
                    return "Action " + action + " not handled.";
            }
        }

        private readonly Company _company = new Company("BELAFLOR", "", "");
    }
}