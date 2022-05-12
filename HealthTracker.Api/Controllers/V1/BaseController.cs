using AutoMapper;
using HealthTracker.DataService.IConfiguration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthTracker.Api.Controllers.V1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected readonly IMapper _mapper;
        protected readonly IUnitOfWork _unitOfWork;

        public BaseController(IUnitOfWork unitOfWork, IMapper mapper) // AppDbContext context
        {
            // _context = context;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
    }
}
