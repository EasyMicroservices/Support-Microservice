﻿using EasyMicroservices.Cores.AspCoreApi;
using EasyMicroservices.Cores.Database.Interfaces;
using EasyMicroservices.SupportsMicroservice.Contracts.Common;
using EasyMicroservices.SupportsMicroservice.Contracts.Requests;
using EasyMicroservices.SupportsMicroservice.Database.Entities;

namespace EasyMicroservices.SupportsMicroservice.WebApi.Controllers
{
    public class TicketController : SimpleQueryServiceController<TicketEntity, CreateTicketRequestContract, UpdateTicketRequestContract, TicketContract, long>
    {
        public TicketController(IContractLogic<TicketEntity, CreateTicketRequestContract, UpdateTicketRequestContract, TicketContract, long> contractLogic) : base(contractLogic)
        {

        }
    }
}
