﻿using EasyMicroservices.Cores.AspCoreApi;
using EasyMicroservices.Cores.Contracts.Requests;
using EasyMicroservices.Cores.Database.Interfaces;
using EasyMicroservices.ServiceContracts;
using EasyMicroservices.SupportsMicroservice.Contracts.Common;
using EasyMicroservices.SupportsMicroservice.Contracts.Requests;
using EasyMicroservices.SupportsMicroservice.Database.Entities;

namespace EasyMicroservices.SupportsMicroservice.WebApi.Controllers
{
    public class TicketAssignController : SimpleQueryServiceController<TicketAssignEntity, TicketAssignCreateRequestContract, TicketAssignUpdateRequestContract, TicketAssignContract, long>
    {
        private readonly IContractLogic<TicketAssignEntity, TicketAssignCreateRequestContract, TicketAssignUpdateRequestContract, TicketAssignContract, long> _contractlogic;
        private readonly IContractLogic<TicketEntity, TicketCreateRequestContract, TicketUpdateRequestContract, TicketContract, long> _ticketlogic;

        public TicketAssignController(IContractLogic<TicketEntity, TicketCreateRequestContract, TicketUpdateRequestContract, TicketContract, long> ticketlogic , IContractLogic<TicketAssignEntity, TicketAssignCreateRequestContract, TicketAssignUpdateRequestContract, TicketAssignContract, long> contractLogic) : base(contractLogic)
        {
            _contractlogic = contractLogic;
            _ticketlogic  = ticketlogic;
        }
        public override async Task<MessageContract<long>> Add(TicketAssignCreateRequestContract request, CancellationToken cancellationToken = default)
        {
            var checkTicketId = await _ticketlogic.GetById(new GetIdRequestContract<long>(){ Id = request.TicketId});
            if (checkTicketId.IsSuccess)
            return await _contractlogic.Add(request, cancellationToken);
            return (EasyMicroservices.ServiceContracts.FailedReasonType.Empty, "TicketId is incorrect");
        }
        public override async Task<MessageContract<TicketAssignContract>> Update(TicketAssignUpdateRequestContract request, CancellationToken cancellationToken = default)
        {
            var checkTicketId = await _ticketlogic.GetById(new GetIdRequestContract<long>() { Id = request.TicketId });
            if (checkTicketId.IsSuccess)
            return await _contractlogic.Update(request, cancellationToken);
            return (EasyMicroservices.ServiceContracts.FailedReasonType.Empty, "TicketId is incorrect");
        }
    }
}
