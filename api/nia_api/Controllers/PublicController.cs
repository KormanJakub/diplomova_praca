using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;

namespace nia_api.Controllers
{
    [ApiController]
    [Route("public")]
    public class PublicController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly PasswordService _service;
        private readonly JwtTokenService _token;
        private readonly IEmailSender _emailSender;

        public PublicController(NiaDbContext context, PasswordService service, JwtTokenService token, IEmailSender emailSender)
        {
            _users = context.Users;
            _service = service;
            _token = token;
            _emailSender = emailSender;
        }
        
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest user)
        {
            if (user == null)
                return BadRequest("User is empty!");

            if (_users.FindAsync(user.Email) == null)
                return BadRequest("Email is already registered!");
            
            if (user.FirstName == null)
                return BadRequest(new { Message = "First name is empty!" });

            if (user.LastName == null)
                return BadRequest(new { Message = "Last name is empty!" });

            if (user.Password.Length < 6)
                return BadRequest(new { Message = "Password is too short! Min 6 Lenght" });

            if (!user.Password.Any(char.IsUpper))
                return BadRequest(new { Message = "Password must contain at least one uppercase letter!" });

            if (!user.Password.Any(char.IsLower))
                return BadRequest(new { Message = "Password must contain at least one lowercase letter!" });

            if (user.Password != user.RepeatPassword)
                return BadRequest("Password's are not same!");
            
            var hashedPassword = _service.HashPassword(user.Password);
            
            var newUser = new User()
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Password = hashedPassword,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsAdmin = false,
                CreatedAt = DateTime.Now
            };

            await _users.InsertOneAsync(newUser);
            return Ok("Register successful!");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest user)
        {
            if (user == null)
                return BadRequest("User Request is null!");
            
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized("User is not registered!");

            var hashPassword = _service.HashPassword(user.Password);

            if (dbUser.Password != hashPassword)
                return Unauthorized("Password's do not match!");
            
            var token = _token.GenerateToken(
                dbUser.Id,
                dbUser.Email,
                dbUser.FirstName,
                dbUser.LastName
            );

            if (dbUser.IsAdmin)
                return Ok(new { token, role = "admin" });
            
            return Ok(new {token});
        }

        [HttpPost("email")]
        public async Task<IActionResult> Email()
        {
            var receiver = "kjakub@atlas.sk";
            var firstName = "Jakub";
            var lastName = "Korman";
            var subject = "Registration Confirmation";
            var code = "1592634";

            await _emailSender.SendEmailAsync(receiver, subject, firstName, lastName, code);
            return Ok("Verification email sent successfully.");
        }
    }
}
