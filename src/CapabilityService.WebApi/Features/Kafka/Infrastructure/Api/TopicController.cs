﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DFDS.CapabilityService.WebApi.Domain.Models;
using DFDS.CapabilityService.WebApi.Domain.Repositories;
using DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Services;
using DFDS.CapabilityService.WebApi.Features.Kafka.Infrastructure.RestClients;
using DFDS.CapabilityService.WebApi.Infrastructure.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topic = DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Models.Topic;
using ITopicRepository = DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Repositories.ITopicRepository;

namespace DFDS.CapabilityService.WebApi.Infrastructure.Api
{
	[Authorize(AuthenticationSchemes = "AzureADBearer")]
	[ApiController]
	[Route("api/v1/capabilities")]
	public class TopicController : ControllerBase
	{
		private readonly ITopicDomainService _topicDomainService;
		private readonly ITopicRepository _topicRepository;
		private readonly ICapabilityRepository _capabilityRepository;
		private readonly IKafkaJanitorRestClient _kafkaJanitorRestClient;

		public TopicController(
			ITopicDomainService topicDomainService,
			ITopicRepository topicRepository,
			ICapabilityRepository capabilityRepository,
			IKafkaJanitorRestClient kafkaJanitorRestClient
		)
		{
			_topicDomainService = topicDomainService;
			_topicRepository = topicRepository;
			_capabilityRepository = capabilityRepository;
			_kafkaJanitorRestClient = kafkaJanitorRestClient;
		}

		[HttpGet("{id}/topics")]
		public async Task<IActionResult> GetAllByCapability(string id)
		{
			var topics = await _topicRepository.GetAllAsync();

			var capabilityId = Guid.Empty;
			Guid.TryParse(id, out capabilityId);

			var result = new
			{
				Items = topics
					.Where(t => t.CapabilityId == capabilityId)
					.Select(DTOs.Topic.CreateFrom)
					.ToArray()
			};

			return Ok(result);
		}
		
		[HttpGet("/api/v1/topics")]
		public async Task<IActionResult> GetAll()
		{
			var topics = await _topicRepository.GetAllAsync();

			var result = new
			{
				Items = topics
					.Select(DTOs.Topic.CreateFrom)
					.ToArray()
			};

			return Ok(result);
		}


		[HttpPost("{id}/topics")]
		public async Task<IActionResult> AddTopicToCapability(string id, [FromBody] TopicInput input)
		{
			var capabilityId = Guid.Empty;
			Guid.TryParse(id, out capabilityId);

			if (capabilityId == Guid.Empty) return BadRequest(new {Message = $"the capability id: {id} is malformed"});

			var capability = await
				_capabilityRepository.Get(capabilityId);

			IActionResult actionResult;
			try
			{
				var configurations = new Dictionary<string, object>();
				if (input.Configurations != null)
				{
					foreach (var (key, value) in input.Configurations)
					{
						var jsonElement = (JsonElement)value;
						configurations[key] = JsonObjectTools.GetValueFromJsonElement(jsonElement);
					}
				}
				
				var topic = Topic.Create(
					capabilityId,
					capability.RootId,
					input.Name,
					input.Description,
					input.Partitions,
					input.Availability,
					configurations
				);

				await _topicDomainService.CreateTopic(
					topic: topic,
					dryRun: true
				);

				if (input.DryRun) { return Ok(DTOs.Topic.CreateFrom(topic)); }

				TaskFactoryExtensions.StartActionWithConsoleExceptions(async () =>
				{
					await _kafkaJanitorRestClient.CreateTopic(topic, capability);

					await _topicDomainService.CreateTopic(
						topic: topic,
						dryRun:input.DryRun
						);
				});

				var topicDto = DTOs.Topic.CreateFrom(topic);
				actionResult = Ok(topicDto);
			}
			catch (Exception exception) when (ExceptionToStatusCode.CanConvert(exception, out actionResult)) { }

			return actionResult;
		}
	}
}
