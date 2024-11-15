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
        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest user)
        {
            if (user == null)
                return BadRequest(new { error = "User is empty!" });
            
            var existingUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest(new { error = "Email is already registered!"});
            
            if (user.FirstName == null)
                return BadRequest(new { error = "First name is empty!" });

            if (user.LastName == null)
                return BadRequest(new { error = "Last name is empty!" });

            if (user.Password.Length < 6)
                return BadRequest(new { error = "Password is too short! Min 6 Length" });

            if (!user.Password.Any(char.IsUpper))
                return BadRequest(new { error = "Password must contain at least one uppercase letter!" });

            if (!user.Password.Any(char.IsLower))
                return BadRequest(new { error = "Password must contain at least one lowercase letter!" });

            if (user.Password != user.RepeatPassword)
                return BadRequest(new { error = "Password's are not same!"});
            
            var hashedPassword = _service.HashPassword(user.Password);

            var verificationCode = GenerateVerificationCode();
            
            var newUser = new User()
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Password = hashedPassword,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailConfirmed = false,
                VerificationCode = verificationCode,
                CreatedAt = LocalTimeService.LocalTime()
            };
            
            await _emailSender.SendEmailAsync(
                newUser.Email,
                "Potvrdenie registrácie",
                newUser.FirstName,
                newUser.LastName,
                verificationCode.ToString(),
                "Registration"
                );
        
            ScheduleVerificationCodeDeletion(newUser);
            
            await _users.InsertOneAsync(newUser);
            return Ok(new { message = "Register successful and verification email sent successfully!", email = newUser.Id});
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest user)
        {
            if (user == null)
                return BadRequest(new { error = "User Request is null!"});
            
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});

            var hashPassword = _service.HashPassword(user.Password);

            if (dbUser.Password != hashPassword)
                return Unauthorized(new { error = "Password's do not match!" });
            
            var token = _token.GenerateToken(
                dbUser.Id,
                dbUser.Email,
                dbUser.FirstName,
                dbUser.LastName
            );

            if (dbUser.IsAdmin)
                return Ok(new { token, role = "admin", email_confirmation = dbUser.IsEmailConfirmed });
            
            return Ok(new {token, email_confirmation = dbUser.IsEmailConfirmed});
        }

        [HttpPut("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var dbUser = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            
            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});

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
            
            return Ok( new { message = "Verification code for Forgot Password sent successfully.!"});
        }

        [HttpPost("verification-code")]
        public async Task<IActionResult> VerificateCode([FromBody] VerificateCodeRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});

            if (user.VerificationCode <= 100000 & user.VerificationCode >= 1000000)
                return BadRequest(new { error = "Wrong verification code!"});

            if (user.VerificationCode != dbUser.VerificationCode)
                return BadRequest(new {error = "You entered bad code"});
            
            
            return Ok(new {message = "You entered good code! Write new password!" });
        }

        [HttpPost("new-verification-code")]
        public async Task<IActionResult> NewVerificationCode([FromBody] EmailRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            
            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});
            
            var generateNewVerificationCode = GenerateVerificationCode();
            var update = Builders<User>.Update.Set(u => u.VerificationCode, generateNewVerificationCode);
            
            await _users.FindOneAndUpdateAsync(
                u => u.Id == dbUser.Id,
                update 
            );
            
            ScheduleVerificationCodeDeletion(dbUser);
            
            return Ok(new {message = "You should receive new verification code!" });
        }

        [HttpPut("new-password")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (user.NewPassword.Length < 6)
                return BadRequest(new { error = "Password is too short! Min 6 Lenght" });

            if (!user.NewPassword.Any(char.IsUpper))
                return BadRequest(new { error = "Password must contain at least one uppercase letter!" });

            if (!user.NewPassword.Any(char.IsLower))
                return BadRequest(new { error = "Password must contain at least one lowercase letter!" });

            if (user.NewPassword != user.RepeatNewPassword)
                return BadRequest(new { error = "Password's are not same!"});

            var hashedNewPassword = _service.HashPassword(dbUser.Password);

            if (hashedNewPassword == dbUser.Password)
                return BadRequest(new { error = "You entered old password!"});

            var update = Builders<User>.Update.Set(u => u.Password, hashedNewPassword);
            
            await _users.FindOneAndUpdateAsync(
                u => u.Id == dbUser.Id,
                update 
            );
            
            return Ok(new {message = "You have new password!"});
        }
        
        private int GenerateVerificationCode()
        {
            var random = new Random();
            var verificationCode = random.Next(100000, 999999);
            return verificationCode;
        }

        private void ScheduleVerificationCodeDeletion(User user)
        {
            Task.Run((Func<Task>)(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10));

                var lcUser = await _users.Find(u => u.Id == user.Id).FirstOrDefaultAsync();
                if (lcUser != null && !lcUser.IsEmailConfirmed)
                {
                    lcUser.VerificationCode = 0;
                    await _users.ReplaceOneAsync(u => u.Id == lcUser.Id, lcUser);
                }
            }));
        }
    }
}
