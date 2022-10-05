using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using todaapp.Configuration;
using todaapp.Data;
using todaapp.Dtos.Requests;
using todaapp.Dtos.Responses;
using todaapp.Models;

namespace todaapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ConfigJwt _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly TodoDbContext _db;

        public AuthManagementController(UserManager<IdentityUser> userManager, IOptionsMonitor<ConfigJwt> jwtConfig, TokenValidationParameters tokenValidationParams, TodoDbContext dbcontext)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.CurrentValue;
            _tokenValidationParameters = tokenValidationParams;
            _db = dbcontext;

        }
        [HttpPost]
        [Route("register")]

        public async Task<ActionResult<RegistrationResponse>> Register([FromBody] UserRegistrationDto user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new RegistrationResponse()
                {
                    Errors = new List<string>(){
                        "Invalid Payload"
                    }
                });
            }
            var userExist = await _userManager.FindByEmailAsync(user.Email);
            if (userExist != null) return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>(){
                        "Email already registered"
                    }
            });

            var newUser = new IdentityUser
            {
                Email = user.Email,
                UserName = user.Username

            };
            var isCreated = await _userManager.CreateAsync(newUser, user.Password);
            if (!isCreated.Succeeded)
            {
                return BadRequest(new RegistrationResponse()
                {
                    Errors = isCreated.Errors.Select(x => x.Description).ToList(),

                    Success = false
                });
            }
            var token = await GenerateToken(newUser);
            return Ok(token);
        }
        [HttpPost("login")]
        public async Task<ActionResult<RegistrationResponse>> Login([FromBody] UserLoginReq user)
        {
            if (!ModelState.IsValid)
            {
                return
                BadRequest(new RegistrationResponse()
                {
                    Errors = new List<string>(){
                        "Invalid Payload"
                    }
                });
            }
            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser is null)
            {
                BadRequest(new RegistrationResponse()
                {
                    Errors = new List<string>(){
                        "Invalid login infos"
                    },
                    Success = false
                });
            }
            var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);
            if (!isCorrect)
            {
                return BadRequest(new RegistrationResponse
                {
                    Errors = new List<string>(){
                        "Invalid login infos"
                    },
                    Success = false
                });
            }
            var token = await GenerateToken(existingUser);

            return Ok(new RegistrationResponse
            {
                Success = true,
                Token = token.Token,
                RefreshToken = token.RefreshToken,
            });
        }

        [HttpPost]
        [Route("refreshtoken")]
        public async Task<ActionResult<AuthResult>> RefreshToken([FromBody] TokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new RegistrationResponse
                {
                    Errors = new List<string>{
                        "invalid payload"
                    },
                    Success = false
                });
            }
            var result = await VerifityToken(request);
            if (result == null) return BadRequest(
                new AuthResult
                {
                    Success = false,
                    Errors = new List<string>{
                            "invalid token"
                        }
                }
            );
            return Ok(result);
        }

        private async Task<AuthResult> VerifityToken(TokenRequest request)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            try
            {
                //first validation we check the token based on our configuration 
                ClaimsPrincipal tokenValidation = jwtTokenHandler.ValidateToken(request.Token, _tokenValidationParameters, out var validateToken);

                //second validation check if algorithm same
                if (validateToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);
                    if (result == false) return null;
                }

                //third validation check expiring date
                var UnixexpiryDate = long.Parse(tokenValidation.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDate = FromTimeStampToDateTime(UnixexpiryDate);
                if (expiryDate > DateTime.UtcNow.AddHours(2))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>{
                            "token not yet expired"
                        }
                    };
                }

                //fourth validation check token with database
                var tokenStored = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

                if (tokenStored == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>{
                            "token doesn't exist"
                        }
                    };
                }
                //fifth validation check if token has alread been used
                if (tokenStored.IsUsed)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>{
                            "token has been used"
                        }
                    };
                }
                // validation 6 
                if (tokenStored.IsRevoked)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>{
                            "token has been revoked"
                        }
                    };
                }

                //validation 7 token and stored refrehtoken tokenjti match
                var jti = tokenValidation.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (tokenStored.JwId != jti)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>{
                            "token doesn't match"
                        }
                    };
                }

                //we update the current refreshtoken
                tokenStored.IsUsed = true;
                _db.RefreshTokens.Update(tokenStored);
                await _db.SaveChangesAsync();
                var identityuser = await _userManager.FindByIdAsync(tokenStored.UserId);
                return await GenerateToken(identityuser);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DateTime FromTimeStampToDateTime(long unixexpiryDate)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(unixexpiryDate).ToLocalTime();
            return dateTimeVal;
        }

        private async Task<AuthResult> GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new List<Claim>{
                   new Claim("Id",user.Id),
                   new Claim(JwtRegisteredClaimNames.Email,user.Email),
                   new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                   new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                }),
                //possibly 10-15min normally
                Expires = DateTime.UtcNow.AddSeconds(40),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenCreated = jwtTokenHandler.CreateToken(tokenDescriptor);
            var tokenStringify = jwtTokenHandler.WriteToken(tokenCreated);


            //refreshTOken
            var refreshToken = new RefreshToken
            {
                JwId = tokenCreated.Id,
                //tokenDescriptor.Subject.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value,
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                Token = RamdomString(35) + Guid.NewGuid()
            };
            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
            return new AuthResult
            {
                Token = tokenStringify,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        private string RamdomString(int length)
        {
            var Random = new Random();
            var chars = "ABCDEFGHIJIKLMOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(x => x[Random.Next(x.Length)]).ToArray());
        }
    }
}