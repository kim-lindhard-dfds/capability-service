﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFDS.CapabilityService.WebApi.Persistence;

namespace DFDS.CapabilityService.WebApi.Models
{
    public class CapabilityTransactionalDecorator : ICapabilityApplicationService
    {
        private readonly ICapabilityApplicationService _inner;
        private readonly CapabilityServiceDbContext _dbContext;

        public CapabilityTransactionalDecorator(ICapabilityApplicationService inner, CapabilityServiceDbContext dbContext)
        {
            _inner = inner;
            _dbContext = dbContext;
        }

        public Task<IEnumerable<Capability>> GetAllCapabilities() => _inner.GetAllCapabilities();
        public Task<Capability> GetCapability(Guid id) => _inner.GetCapability(id);

        public async Task<Capability> CreateCapability(string name)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                var capability = await _inner.CreateCapability(name);

                await _dbContext.SaveChangesAsync();
                transaction.Commit();

                return capability;
            }
        }

        public async Task JoinCapability(Guid capabilityId, string memberEmail)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                await _inner.JoinCapability(capabilityId, memberEmail);

                await _dbContext.SaveChangesAsync();
                transaction.Commit();
            }
        }

        public async Task LeaveCapability(Guid capabilityId, string memberEmail)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                await _inner.LeaveCapability(capabilityId, memberEmail);

                await _dbContext.SaveChangesAsync();
                transaction.Commit();
            }
        }
    }
}