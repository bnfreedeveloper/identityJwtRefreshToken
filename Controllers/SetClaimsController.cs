using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using todaapp.Configuration;
using todaapp.Data;

namespace todaapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SetClaimsController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ConfigJwt _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly TodoDbContext _db;

        public SetClaimsController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IOptionsMonitor<ConfigJwt> jwtConfig, TokenValidationParameters tokenValidationParams, TodoDbContext dbcontext)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.CurrentValue;
            _tokenValidationParameters = tokenValidationParams;
            _db = dbcontext;
            _roleManager = roleManager;

        }
        [HttpGet]
        public async Task<IActionResult> GetClaimsOfUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return BadRequest(new
            {
                error = "user doesn't exist"
            });
            var userClaims = await _userManager.GetClaimsAsync(user);
            return Ok(userClaims);
        }
        [HttpPost]
        [Route("addClaim")]
        public async Task<IActionResult> AddClaim(string email, string claimName, string claimValue)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return BadRequest(new
            {
                error = "user doesn't exist"
            });
            var userClaim = new Claim(claimName, claimValue);
            var result = await _userManager.AddClaimAsync(user, userClaim);
            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = $"the user {email} got the claim {claimName} added"
                });
            }
            return BadRequest(new
            {
                message = $"ce claim wasn't added to {email} "
            });
        }

    }
}