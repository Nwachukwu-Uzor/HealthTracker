using AutoMapper;
using HealthTracker.DataService.Data;
using HealthTracker.DataService.IConfiguration;
using HealthTracker.Entities.DbSet;
using HealthTracker.Entities.Dtos.Incoming;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HealthTracker.Api.Controllers.V1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : BaseController
    {

        public UsersController(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
        {

        }
        // Get Single User
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _unitOfWork.Users.GetById(id);
            return Ok(_mapper.Map<UserDto>(user));
        }

        // Get All Users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _unitOfWork.Users.GetAll();
            return Ok(users);
            //return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
        }


        // Create a User
        [HttpPost]
        public async Task<IActionResult> CreateUser(UserDto user)
        {
            var _user = _mapper.Map<User>(user);
            await _unitOfWork.Users.Add(_user);
            await _unitOfWork.CompletedAsync();

            return CreatedAtAction(nameof(GetUserById), new { _user.Id}, _user);
        }
    }
}
