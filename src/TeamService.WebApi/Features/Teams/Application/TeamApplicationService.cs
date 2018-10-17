using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFDS.TeamService.WebApi.Features.Teams.Domain.Events;
using DFDS.TeamService.WebApi.Features.Teams.Domain.Models;
using DFDS.TeamService.WebApi.Features.Teams.Domain.Repositories;

namespace DFDS.TeamService.WebApi.Features.Teams.Application
{
    public class TeamApplicationService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;

        public TeamApplicationService(ITeamRepository teamRepository, IUserRepository userRepository)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
        }

        public Task<IEnumerable<Team>> GetAllTeams()
        {
            return _teamRepository.GetAll();
        }

        public Task<Team> GetTeam(Guid id)
        {
            return _teamRepository.GetById(id);
        }

        public async Task<Team> CreateTeam(string name, string department)
        {
            var team = Team.Create(name, department);

            await _teamRepository.Add(team);

            DomainEventPublisher.Publish(new TeamCreated(
                teamId: team.Id,
                teamName: team.Name,
                department: team.Department
            ));

            return team;
        }

        public Task<bool> Exists(string teamName)
        {
            return _teamRepository.ExistsWithName(teamName);
        }

        public async Task JoinTeam(Guid teamId, string userId)
        {
            var team = await _teamRepository.GetById(teamId);
            if (team == null)
            {
                throw new Exception($"Team with id \"{teamId}\" could not be found. User \"{userId}\" can therefore not join that team.");
            }

            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                throw new Exception($"User with id\"{userId}\" could not be found and can therefore not join team \"{team.Name}\".");
            }

            team.StartMembership(user, MembershipType.Developer);
        }
    }

    #region old cognito based team service

    //public class CognitoTeamService : ITeamService
    //{
    //    private readonly UserPoolClient _userPoolClient;

    //    public CognitoTeamService(UserPoolClient userPoolClient)
    //    {
    //        _userPoolClient = userPoolClient;
    //    }

    //    public async Task<List<Team>> GetAllTeams()
    //    {
    //        var groupNames = await _userPoolClient.ListGroupsAsync();

    //        var getTeamsTask = groupNames
    //            .Select(g => GetTeam(g));


    //        var teams = (await Task.WhenAll(getTeamsTask)).ToList();


    //        return teams;
    //    }

    //    public async Task<Team> GetTeam(string id)
    //    {
    //        var usersInGroup = await _userPoolClient.ListUsersInGroupAsync(id);

    //        var teamNameAndDepartment = id.Split(new[] {"_D_"}, StringSplitOptions.None);
    //        var departmentName = 1 < teamNameAndDepartment.Length ? teamNameAndDepartment[1] : null;

    //        var team = new Team
    //        {
    //            Id = id,
    //            Name = teamNameAndDepartment[0],
    //            Department = departmentName,
    //            Members = usersInGroup
    //                .Select(u => CreateUserFromUserType(u)
    //                ).ToList()
    //        };


    //        return team;
    //    }

    //    public User CreateUserFromUserAndAttributes(string username, List<AttributeType> attributes)
    //    {
    //        var user = new User();
    //        user.Id = username;

    //        if (attributes == null)
    //        {
    //            return user;
    //        }

    //        user.Name = attributes.FirstOrDefault(a => a.Name == "name")?.Value;
    //        user.Email = attributes.FirstOrDefault(a => a.Name == "email")?.Value;


    //        return user;
    //    }

    //    public User CreateUserFromUserType(UserType userType)
    //    {
    //        var user = CreateUserFromUserAndAttributes(
    //            userType.Username,
    //            userType.Attributes
    //        );


    //        return user;
    //    }

    //    public Task<Team> CreateTeam(string name, string department)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<bool> Exists(string teamName)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task JoinTeam(string teamId, string userId)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    #endregion
}