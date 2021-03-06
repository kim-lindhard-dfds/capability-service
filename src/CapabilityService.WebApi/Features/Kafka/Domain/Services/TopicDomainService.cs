using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Exceptions;
using DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Models;
using DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Repositories;

namespace DFDS.CapabilityService.WebApi.Features.Kafka.Domain.Services
{
	public class TopicDomainService : ITopicDomainService
	{
		private readonly ITopicRepository _topicRepository;

		public TopicDomainService(ITopicRepository topicRepository)
		{
			_topicRepository = topicRepository;
		}

		public async Task CreateTopic(Topic topic, bool dryRun)
		{
			var existingTopics = await _topicRepository.GetAllAsync();
			
			if(existingTopics.Any(t => t.Name.Equals(topic.Name))){ throw new TopicAlreadyExistException(topic.Name);}

			if(dryRun) return;
			
			await _topicRepository.AddAsync(topic);
		}

		public async Task<IEnumerable<Topic>> GetAllTopics()
		{
			return await _topicRepository.GetAllAsync();
		}
	}
}
