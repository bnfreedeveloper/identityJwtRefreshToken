using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using todaapp.Data;

namespace todaapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SetupController : ControllerBase
    {
        private readonly TodoDbContext _context;
        private readonly UserManager<IdentityUser> _userManag;
        private readonly RoleManager<IdentityRole> _roleManag;
        private readonly ILogger<SetupController> _logger;

        public SetupController(TodoDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<SetupController> logger)
        {
            this._context = context;
            this._userManag = userManager;
            this._roleManag = roleManager;
            this._logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdentityRole>>> GetAllRoles()
        {
            var roles = await _roleManag.Roles.ToListAsync();
            return Ok(roles);

        }
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromForm] string name)
        {
            // we check if role already here
            var roleExist = await _roleManag.RoleExistsAsync(name);
            if (!roleExist)
            {
                //here we gonna add the role, we could try catch server errors also
                var roleAdded = await _roleManag.CreateAsync(new IdentityRole
                {
                    Name = name
                });
                if (roleAdded.Succeeded)
                {
                    _logger.LogInformation($"the role {name} was added");
                    return Ok(new
                    {
                        result = $"the role {name} was added successfully"
                    });
                }
                _logger.LogInformation("the role wasn't added");
                return BadRequest(new
                {
                    error = "the user hasn't been added"
                });
            }
            return BadRequest(new
            {
                error = "role already exists"
            });
        }

        [HttpGet]
        [Route("allUsers")]

        public async Task<ActionResult<IEnumerable<IdentityUser>>> GetAllUsers()
        {
            var users = await _userManag.Users.ToListAsync();
            return Ok(users);
        }

        [HttpPost]
        [Route("addRole")]
        public async Task<IActionResult> AddRoleToUser([FromForm] string email, [FromForm] string roleName)
        {

            // we check if the user exists
            var userExist = await _userManag.FindByEmailAsync(email);
            if (userExist == null)
            {
                _logger.LogInformation($"the user for the email {email} doesn't exist ");
                return BadRequest(new
                {
                    error = "this user doesn't exist"
                });
            }

            //we check if the role exists
            if (!await _roleManag.RoleExistsAsync(roleName))
            {
                _logger.LogInformation($"the role {roleName} doesn't exist");
                return BadRequest(new
                {
                    error = "this user doesn't exist"
                });
            }

            //we check if the role is successfully assigned
            var result = await _userManag.AddToRoleAsync(userExist, roleName);
            if (!result.Succeeded)
            {
                _logger.LogInformation($"the user {userExist.UserName} and role {roleName} weren't mixed");
                return BadRequest(new
                {
                    error = "this user wasn't associated with this role"
                });
            }
            return Ok(new
            {
                success = true,
                message = $"the user {userExist.UserName} was successfully associate with the role {roleName} "
            });
        }

        [HttpGet]
        [Route("userRoles")]
        public async Task<IActionResult> GetUserRoles([FromForm] string email)
        {
            // we check if the user exist
            var user = await _userManag.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation($"the user with the email {email} doesn't exist");
                return BadRequest(new
                {
                    error = "this user doesn't exist"
                });
            }

            //we get the roles
            var roles = await _userManag.GetRolesAsync(user);
            return Ok(roles);
        }
        [HttpPost]
        [Route("removeRoleFromUser")]

        public async Task<IActionResult> RemoveRoleFromUser([FromForm] string email, [FromForm] string roleName)
        {
            //we chech if user exists
            var user = await _userManag.FindByEmailAsync(email);
            if (user == null) return BadRequest(new
            {
                error = "no user for this email"
            });
            //we check if role exists
            var role = await _roleManag.FindByNameAsync(roleName);
            if (role == null) return BadRequest(new
            {
                error = "no role found"
            });
            var result = await _userManag.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                return Ok(new
                {
                    succes = true,
                    LoggerMessage = $"the {user.UserName} has been removed from role {roleName}"
                });
            }
            return BadRequest(new
            {
                error = $"unable to remove {roleName} for user {email}"
            });
        }
    }
}