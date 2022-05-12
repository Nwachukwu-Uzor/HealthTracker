using AutoMapper;
using HealthTracker.Authentication.Configuration;
using HealthTracker.Authentication.Models.DTO.Generic;
using HealthTracker.Authentication.Models.DTO.Incoming;
using HealthTracker.Authentication.Models.DTO.Outgoing;
using HealthTracker.DataService.IConfiguration;
using HealthTracker.Entities.DbSet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HealthTracker.Api.Controllers.V1
{
    public class AccountsController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;
        public AccountsController(
            IUnitOfWork unitOfWork, IMapper mapper,
            UserManager<IdentityUser> userManager,
            IOptions<JwtConfig> optionMonitor,
            TokenValidationParameters tokenValidationParameters
        )
        : base(unitOfWork, mapper)
        {
            _userManager = userManager;
            _jwtConfig = optionMonitor.Value;
            _tokenValidationParameters = tokenValidationParameters;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserRegistrationResponseDto>> Register(UserRegistrationRequestDto registrationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new UserRegistrationResponseDto
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        "Invalid payload"
                    }
                });
            }

            // Check if the email exists
            var userExist = await _userManager.FindByEmailAsync(registrationDto.Email);

            if (userExist != null)
            {
                return BadRequest(new UserRegistrationResponseDto
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        "A user already exists with the email account"
                    }
                });
            }
            // Add the user
            var newUser = _mapper.Map<IdentityUser>(registrationDto);
            var isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);

            if (!isCreated.Succeeded)
            {
                return BadRequest(new UserRegistrationResponseDto
                {
                    Success = false,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList(),

                });
            }

            // Add create a user record from the dto
            var _user = _mapper.Map<User>(registrationDto);
            _user.IdentityId = new Guid(newUser.Id);
            await _unitOfWork.Users.Add(_user);
            await _unitOfWork.CompletedAsync();

            // Create a jwt token
            var token = await GenerateJwtToken(newUser);

            // return a success response
            return Ok(new UserRegistrationResponseDto
            {
                Success = true,
                Token = token.JwtToken,
                RefreshToken = token.RefreshToken
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new UserRegistrationResponseDto
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        "Invalid payload"
                    }
                });
            }

            // Check if the email exists
            var userExist = await _userManager.FindByEmailAsync(loginDto.Email);

            if (userExist == null)
            {
                return BadRequest(new UserLoginResponseDto
                {
                    Success = false,
                    Errors = new List<string> {
                        "Invalid authentication request"
                    }
                });
            }

            // check if the user provided the correct sign in password
            var isCorrect = await _userManager.CheckPasswordAsync(userExist, loginDto.Password);

            // if password is incorrect, send back a bad request
            if (!isCorrect)
            {
                return BadRequest(new UserLoginResponseDto
                {
                    Success = false,
                    Errors = new List<string> {
                        "Invalid authentication request"
                    }
                });
            }

            // if password is correct generate a jwt token
            var token = await GenerateJwtToken(userExist);

            return Ok(new UserLoginResponseDto
            {
                Token = token.JwtToken,
                Success = true,
                RefreshToken = token.RefreshToken
            });
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(TokenRequestDto tokenRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new UserLoginResponseDto
                {
                    Success = false,
                    Errors = new List<string> {
                        "Invalid authentication request"
                    }
                });
            }

            var result = await VerifyToken(tokenRequestDto);

            if (result == null)
            {
                return BadRequest(new UserLoginResponseDto
                {
                    Success = false,
                    Errors = new List<string> {
                        "Token validation failed."
                    }
                });
            }

            return Ok(result);
        }

        private async Task<AuthResult> VerifyToken(TokenRequestDto tokenRequestDto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                // We need to check the validity of the token
                var principal = tokenHandler.ValidateToken(tokenRequestDto.Token, _tokenValidationParameters, out var validatedToken);

                // We need to validate the results that have been generated for us
                // Validate if the string is an actual JWT token and not a random string
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    // check if the jwt token is created with the same algorithm as our jwt token
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256);

                    if (!result)
                    {
                        return null;
                    }

                    // check the expiry date of the token
                    var utcExpiryDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                    // convert to date to check
                    var expDate = UnixTimeStampToDateTime(utcExpiryDate);

                    // checking if the jwt token has expired
                    if (expDate > DateTime.UtcNow)
                    {
                        return new AuthResult { 
                            Success = false,
                            Errors = new List<string>
                            {
                                "Jwt token has not expired"
                            }
                        };
                    }

                    // check if the refresh token exists
                    var refreshTokenExist = await _unitOfWork.RefreshTokens.GetByRefreshToken(tokenRequestDto.RefreshToken);

                    if (refreshTokenExist == null)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Invalid refresh token 1"
                            }
                        };
                    }

                    // check the expiry date of the refresh token 
                    if (refreshTokenExist.ExpiryDate < DateTime.UtcNow)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Refresh token has expired, please login again"
                            }
                        };
                    }

                    // check if the refresh token has been used or not
                    if (refreshTokenExist.IsUsed)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Refresh token has been used, it cannot be reused"
                            }
                        };
                    }

                    // check if refresh token has been revoked
                    if (refreshTokenExist.IsRevoked)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Refresh token has been revoked, it cannot be reused"
                            }
                        };
                    }

                    var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                    if (refreshTokenExist.JwtId != jti)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Refresh token does not match the jwt token"
                            }
                        };
                    }

                    // Start processing and get a new token
                    refreshTokenExist.IsUsed = true;

                    var updateResult = await _unitOfWork.RefreshTokens.MarkRefreshTokenAsUsed(refreshTokenExist);
                    if (!updateResult) {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Error processing request"
                            }
                        };
                    };
                    await _unitOfWork.CompletedAsync();
                    // Get the user to generate a new jwt token
                    var dbUser = await _userManager.FindByIdAsync(refreshTokenExist.UserId);

                    if (dbUser == null)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>
                            {
                                "Error processing request"
                            }
                        };
                    }

                    // generate a new jwt token if the user exists
                    var tokens = await GenerateJwtToken(dbUser);

                    return new AuthResult { 
                        Success = true,
                        Token = tokens.JwtToken,
                        RefreshToken = tokens.RefreshToken 
                    };
                }

                return null;
            } catch(Exception ex)
            {
                // TODO: Add better error handling and add a logger
                var message = ex.Message;
                Console.WriteLine(ex);
                return null;
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixDate)
        {
            // sets date time to 1, Jan, 1970
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

          return dateTime.AddSeconds(unixDate).ToUniversalTime();
        }

        private async Task<TokenData> GenerateJwtToken(IdentityUser user)
        {
            // the handler is going to be responsible for creating the token
            var jwtHandler = new JwtSecurityTokenHandler();

            // generate the security key
            // Converts our secret from a regular string to a byte array of the ASCII equivalent of each character
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            // this contains all the configuration or setting that our JWT token will have
            // although we have defined them in the startup class, it will inherit from those settings
            // and also add more stuffs to it.
            // Basically it will have information about who the token belongs to, it will add claims
            // and some other security information.
            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email), // refers to a unique identifier for each user, since the email is unique for is user, it can serve as the identifier
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // creates a unique identifier for this particular token, so each token can be uniquely identified
                    // the JTI is utilized with a refresh token which allows us to get a new token without forcing the user to log in
                    // again and generate one
                }),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame), // todo update the expiration time to minutes
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature // algorithm used to generate the token
                )
            };

            // generate the security token object
            var token = jwtHandler.CreateToken(tokenDescription);

            // convert security token object into a string that we can actually return
            var jwtToken = jwtHandler.WriteToken(token);

            // generate refresh token
            var refreshToken = new RefreshToken
            {
                AddedDate = DateTime.UtcNow,
                Token = $"{RandomStringGenerator(25)}{Guid.NewGuid()}", // generate a random string and attach a certain guid
                UserId = user.Id,
                IsRevoked = false,
                IsUsed = false,
                Status = 1,
                JwtId = token.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await _unitOfWork.RefreshTokens.Add(refreshToken);
            await _unitOfWork.CompletedAsync();

            var tokenData = new TokenData
            {
                JwtToken = jwtToken,
                RefreshToken = refreshToken.Token
            };

            return tokenData;
        }

        private string RandomStringGenerator(int length)
        {
            var random = new Random();
            const string stringChar = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

            return new string(Enumerable.Repeat(stringChar, length)
                                .Select(s => s[random.Next(s.Length)])
                                .ToArray()
            );
        }
    }
}
