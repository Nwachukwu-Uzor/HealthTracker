using AutoMapper;
using HealthTracker.Authentication.Models.DTO.Incoming;
using HealthTracker.Entities.DbSet;
using HealthTracker.Entities.Dtos.Incoming;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthTracker.Api.Profiles
{
    public class UsersProfile : Profile
    {
        public UsersProfile()
        {
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.DateOfBirth, option => option.MapFrom(src => Convert.ToDateTime(src.DateOfBirth)))
                .ReverseMap();

            CreateMap<UserRegistrationRequestDto, IdentityUser>()
                .ForMember(dest => dest.EmailConfirmed, option => option.MapFrom(src => true))
                .ForMember(dest => dest.UserName, option => option.MapFrom(src => src.Email));

            CreateMap<UserRegistrationRequestDto, User>()
                .ForMember(dest => dest.DateOfBirth, option => option.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Country, option => option.MapFrom(src => ""))
                .ForMember(dest => dest.Phone, option => option.MapFrom(src => ""))
                .ForMember(dest => dest.Status, option => option.MapFrom(src => 1));
        }
    }
}
