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

            var verificationCode = GenerateVerificationCode();
            
            var newUser = new User()
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Password = hashedPassword,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsAdmin = false,
                VerificationCode = verificationCode,
                CreatedAt = DateTime.Now
            };

            await _emailSender.SendEmailAsync(
                newUser.Email,
                "Potvrdenie registrácie",
                newUser.FirstName,
                newUser.LastName,
                verificationCode.ToString(),
                "Registration"
                );

            await _users.InsertOneAsync(newUser);
            return Ok("Register successful and verification email sent successfully.!");
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

        [AllowAnonymous]
        [HttpPost("verification-code")]
        public async Task<IActionResult> VerificationCode([FromBody] SendingVerificationRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            
            if (dbUser == null)
                return Unauthorized("User is not registered!");

            var generateNewVerificationCode = GenerateVerificationCode();
            
            var update = Builders<User>.Update.Set(u => u.VerificationCode, generateNewVerificationCode);

            await _users.FindOneAndUpdateAsync(
                u => u.Id == dbUser.Id,
                update 
            );
            
            await _emailSender.SendEmailAsync(
                dbUser.Email,
                "Zabudnute heslo!",
                dbUser.FirstName,
                dbUser.LastName,
                dbUser.ToString(),
                "Verification"
            );
            
            return Ok("Verification code for Forgot Password sent successfully.!");
        }
        
        private int GenerateVerificationCode()
        {
            var random = new Random();
            var verificationCode = random.Next(100000, 999999);
            return verificationCode;
        }
    }
}
