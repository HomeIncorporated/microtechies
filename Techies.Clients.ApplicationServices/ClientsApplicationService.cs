﻿using DotNetCore.CAP;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Techies.Clients.ApplicationServices.Abstract;
using Techies.Clients.ApplicationServices.Resources;
using Techies.Clients.ApplicationServices.Validators;
using Techies.Clients.Domain;
using Techies.Clients.DTOs.Request;
using Techies.Clients.DTOs.Responses;
using Techies.Clients.Infrastructure;
using Techies.Clients.Infrastructure.Persistence;

namespace Techies.Clients.ApplicationServices
{
    public class ClientsApplicationService: IClientApplicationService
    {
        private readonly IValidator<RegisterClient> _validator;
        private readonly ICapPublisher _publisher;
        private readonly IUnitOfWOrk _unitOfWOrk;
        private readonly IClientsRepository _clientsRepository;
        private readonly ILogger<ClientsApplicationService> _logger;

        public ClientsApplicationService(
            IValidator<RegisterClient> validator,
            ICapPublisher publisher,
            IUnitOfWOrk unitOfWOrk,
            IClientsRepository clientsRepository,
            ILogger<ClientsApplicationService> logger)
        {
            _validator = validator;
            _publisher = publisher;
            _unitOfWOrk = unitOfWOrk;
            _clientsRepository = clientsRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Register(RegisterClient client)
        {
            if(client == null) return OperationResult.WithError<string>(Messages.VerifyYourRequest);

            var result = _validator.Validate(client);

            if(!result.IsValid) return result.ToOperationResult<string>();

            var newClient = new Client(client.Id, client.User)
            {
                FirstName = client.FirstName,
                LastName = client.LastName,
                Birthdate = client.Birthdate
            };            

            _clientsRepository.Add(newClient);

            var recordsaffected = await _unitOfWOrk.SaveAsync();
            
            _logger.LogError($"records affected {recordsaffected}");
            
            if(recordsaffected == 0)            
                return OperationResult.WithError<string>($"Something wrong happened");

            await _publisher.PublishAsync("client.services.registered", newClient);

            return OperationResult.Correct(client.Id.ToString());
        }

        public async Task<Client> GetById(Guid id)
        {            
            var client = await _clientsRepository.GetById(id);
            return client;
        }
    }
}
