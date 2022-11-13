using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Simple_Auth_Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Simple_Auth_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
       // private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        public IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AccountsController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<LoginModel> logger, IConfiguration config)
        {
            _userManager = userManager;
           // _signInManager = signInManager;
            _logger = logger;
            _configuration = config;
            _roleManager = roleManager;
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegistrationModel model)
        {
            var userExists = await _userManager.FindByNameAsync(model.UserName);
            if (userExists != null)
                return BadRequest("User already exists !");

            if (string.IsNullOrEmpty(model.Role) || model.Role.Equals("Admin", StringComparison.InvariantCultureIgnoreCase) || model.Role.Equals("Root", StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest("Invalid role !");
            }

            var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var userModel = new UserModel
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                };

                result = await _userManager.AddToRoleAsync(user, model.Role);
                if (result.Succeeded)
                {
                    userModel.Role = model.Role;
                }

                return Ok(userModel);
            }
            else
            {
                return BadRequest("Failed to create user.");
            }

        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var userInfo = await _userManager.FindByNameAsync(model.UserName);
            if (userInfo != null && await _userManager.CheckPasswordAsync(userInfo, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(userInfo);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userInfo.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            var userInfo = await _userManager.FindByNameAsync(model.UserName);
            if (userInfo != null && await _userManager.CheckPasswordAsync(userInfo, model.CurrentPassword))
            {
                var result = await _userManager.ChangePasswordAsync(userInfo, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    return Ok("Password changed");

                }
            }
            return Unauthorized();
        }

    }
}
