﻿namespace DFDS.CapabilityService.WebApi.Domain.Models
{
    public class Member
    {
        private Member()
        {
            
        }

        public Member(string email)
        {
            Email = email;
        }

        public string Email { get; private set; }
    }
}