using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;
using Tag = nia_api.Models.Tag;

namespace nia_api.Controllers
{
    [ApiController]
    [Route("public")]
    public class PublicController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<NewsReceiver> _newsReceiver;
        private readonly IMongoCollection<Gallery> _gallery;
        private readonly IMongoCollection<Questions> _questions;
        private readonly IMongoCollection<Tag> _tags;
        private readonly IMongoCollection<Design> _designs;
        private readonly IMongoCollection<StoreSettings> _storeSettings;
        
        private readonly PasswordService _service;
        private readonly JwtTokenService _token;
        private readonly IEmailSender _emailSender;

        public PublicController(NiaDbContext context, PasswordService service, JwtTokenService token, IEmailSender emailSender)
        {
            _users = context.Users;
            _products = context.Products;
            _newsReceiver = context.NewsReceivers;
            _gallery = context.Gallery;
            _questions = context.Questions;
            _tags = context.Tags;
            _designs = context.Designs;
            _storeSettings = context.StoreSettings;
            
            _emailSender = emailSender;
            _service = service;
            _token = token;
        }

        [HttpGet("all-products")]
        public async Task<IActionResult> AllProducts()
        {
            var dbProducts = await _products
                .Find(_ => true)
                .ToListAsync();
            
            if (dbProducts == null || dbProducts.Count == 0)
                return NotFound(new { error = "No products found!" });
            
            return Ok(dbProducts);
        }
        
        [HttpGet("filter-products")]
        public async Task<IActionResult> AllProducts([FromQuery] string? tagName)
        {
            List<Product> dbProducts;

            if (tagName == null)
            {
                dbProducts = await _products.Find(_ => true).ToListAsync();
            }
            else
            {
                dbProducts = await _products.Find(p => p.TagName == tagName).ToListAsync();
            }
    
            if (dbProducts == null || dbProducts.Count == 0)
                return NotFound(new { error = "No products found!" });
    
            return Ok(dbProducts);
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProduct(Guid id, [FromQuery] string color)
        {
            var dbProduct = await _products
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (dbProduct == null)
                return NotFound("No product found!");

            var dbProductColors = dbProduct.Colors
                .Where(c => string.Equals(c.Name, color, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var productWithFilteredColor = new
            {
                dbProduct.Id,
                dbProduct.TagId,
                dbProduct.TagName,
                dbProduct.Name,
                dbProduct.Description,
                Colors = dbProductColors,
                dbProduct.Price,
                dbProduct.CreatedAt,
                dbProduct.UpdatedAt
            };
            
            return Ok(productWithFilteredColor);
        }

        [HttpGet("all-designs")]
        public async Task<IActionResult> GetDesigns()
        {
            var dbDesigns = await _designs
                .Find(_ => true)
                .ToListAsync();

            if (dbDesigns == null)
                return NotFound("No designs found!");
            
            return Ok(dbDesigns);
        }
        
        [HttpGet("best-three-products")]
        public async Task<IActionResult> BestThreeProducts()
        {
            var dbProducts = await _products
                .Aggregate()
                .Sample(3)
                .ToListAsync();

            if (dbProducts == null || dbProducts.Count == 0)
                return NotFound(new { error = "No products found!" });

            return Ok(dbProducts);
        }

        [HttpGet("all-tags")]
        public async Task<IActionResult> AllTags()
        {
            var dbTags = await _tags
                .Find(_ => true)
                .ToListAsync();

            if (dbTags == null || dbTags.Count == 0)
                return NotFound(new { error = "No tags found!" });
            
            return Ok(dbTags);
        }
        
        [HttpGet("all-gallery")]
        public async Task<IActionResult> AllGallery()
        {
            var dbGallery = await _gallery
                .Find(_ => true)
                .ToListAsync();
            
            if (dbGallery == null || dbGallery.Count == 0)
                return NotFound(new { error = "No products found!" });
            
            return Ok(dbGallery);
        }

        [HttpPost("give-question")]
        public async Task<IActionResult> GiveQuestion([FromBody] QuestionRequest request)
        {
            var newQuestion = new Questions()
            {
                Email = request.Email,
                Name = request.Name,
                PathOfFile = request.PathOfUrl,
                FileId = request.FileId,
                CreatedAt = LocalTimeService.LocalTime()
            };

            await _questions.InsertOneAsync(newQuestion);

            return Ok(new { message = "You sent a request!" });
        }


        [HttpPost("control")]
        public async Task<IActionResult> ControlData()
        {
            return Ok(new { message = "IDE TO!" });
        }

        [HttpPost("receive-news")]
        public async Task<IActionResult> ReceiveNews([FromBody] EmailDto request)
        {
            var dbNewsReceivers = await _newsReceiver
                .Find(n => n.Email == request.Email)
                .FirstOrDefaultAsync();

            if (dbNewsReceivers != null)
                return BadRequest(new { message = "User already receive!" });

            var lcNewReceiver = new NewsReceiver()
            {
                Email = request.Email,
                CreatedAt = LocalTimeService.LocalTime()
            };

            await _newsReceiver.InsertOneAsync(lcNewReceiver);
            
            return Ok(new {message = "Now you will receive news!"});
        }
        
        [HttpPost("register")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest user)
        {
            if (user == null)
                return BadRequest(new { error = "User is empty!" });
            
            var existingUser = await _users
                .Find(u => u.Email == user.Email)
                .FirstOrDefaultAsync();
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
                VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = LocalTimeService.LocalTime()
            };
            
            await _emailSender.SendEmailAsync(
                newUser.Email,
                "Potvrdenie registrácie",
                newUser.FirstName,
                newUser.LastName,
                verificationCode.ToString(),
                EEmail.REGISTRACION
                );
        
            await _users.InsertOneAsync(newUser);
            return Ok(new { message = "Register successful and verification email sent successfully!", email = newUser.Id});
        }

        [HttpPost("login")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> Login([FromBody] LoginRequest user)
        {
            if (user == null)
                return BadRequest(new { error = "User Request is null!"});
            
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized(new { error = "Invalid credentials." });

            if (!_service.VerifyPassword(user.Password, dbUser.Password))
                return Unauthorized(new { error = "Invalid credentials." });

            if (!dbUser.IsEmailConfirmed)
                return StatusCode(403, new { error = "Email is not confirmed." });

            if (_service.IsLegacyHash(dbUser.Password))
            {
                await _users.UpdateOneAsync(u => u.Id == dbUser.Id,
                    Builders<User>.Update.Set(u => u.Password, _service.HashPassword(user.Password)));
            }
            
            string token;
            if (dbUser.IsAdmin)
            {
                token = _token.GenerateToken(dbUser.Id, dbUser.Email, dbUser.FirstName, dbUser.LastName, "admin");
                return Ok(new { token, role = "admin", email_confirmation = dbUser.IsEmailConfirmed });
            }
            else
            {
                token = _token.GenerateToken(dbUser.Id, dbUser.Email, dbUser.FirstName, dbUser.LastName, null);
                return Ok(new { token, email_confirmation = dbUser.IsEmailConfirmed });
            }
        }

        [HttpPut("forgot-password")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var dbUser = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            
            if (dbUser == null)
                return Ok(new { message = "Ak účet existuje, poslali sme pokyny na obnovu hesla." });

            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashResetToken(token);
            var update = Builders<User>.Update
                .Set(u => u.PasswordResetTokenHash, tokenHash)
                .Set(u => u.PasswordResetExpiresAt, DateTime.UtcNow.AddMinutes(15));
            await _users.UpdateOneAsync(u => u.Id == dbUser.Id, update);

            await _emailSender.SendEmailAsync(dbUser.Email, "Obnova hesla",
                dbUser.FirstName, dbUser.LastName, token, EEmail.VERIFICATION);

            return Ok(new { message = "Ak účet existuje, poslali sme pokyny na obnovu hesla." });
        }

        [HttpPost("verification-code")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> VerificateCode([FromBody] VerificateCodeRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});

            if (user.VerificationCode < 100000 || user.VerificationCode > 999999)
                return BadRequest(new { error = "Wrong verification code!"});

            if (user.VerificationCode != dbUser.VerificationCode ||
                dbUser.VerificationCodeExpiresAt == null ||
                dbUser.VerificationCodeExpiresAt <= DateTime.UtcNow)
                return BadRequest(new {error = "You entered bad code"});

            var verificationFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, dbUser.Id),
                Builders<User>.Filter.Eq(u => u.VerificationCode, user.VerificationCode),
                Builders<User>.Filter.Gt(u => u.VerificationCodeExpiresAt, DateTime.UtcNow));
            var verificationUpdate = Builders<User>.Update
                .Set(u => u.IsEmailConfirmed, true)
                .Set(u => u.VerificationCode, 0)
                .Unset(u => u.VerificationCodeExpiresAt);
            if ((await _users.UpdateOneAsync(verificationFilter, verificationUpdate)).MatchedCount == 0)
                return BadRequest(new { error = "Code expired." });
            
            
            return Ok(new {message = "You entered good code! Write new password!" });
        }

        [HttpPost("new-verification-code")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> NewVerificationCode([FromBody] EmailRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            
            if (dbUser == null)
                return Unauthorized(new { error = "User is not registered!"});
            
            var generateNewVerificationCode = GenerateVerificationCode();
            var update = Builders<User>.Update
                .Set(u => u.VerificationCode, generateNewVerificationCode)
                .Set(u => u.VerificationCodeExpiresAt, DateTime.UtcNow.AddMinutes(10));
            
            await _users.FindOneAndUpdateAsync(
                u => u.Id == dbUser.Id,
                update 
            );
            
            await _emailSender.SendEmailAsync(dbUser.Email, "Nový overovací kód",
                dbUser.FirstName, dbUser.LastName, generateNewVerificationCode.ToString(), EEmail.REGISTRACION);
            
            return Ok(new {message = "You should receive new verification code!" });
        }

        [HttpPut("new-password")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordRequest user)
        {
            var dbUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            if (dbUser == null || string.IsNullOrWhiteSpace(user.Token))
                return BadRequest(new { error = "Invalid reset request." });

            if (user.NewPassword.Length < 6)
                return BadRequest(new { error = "Password is too short! Min 6 Lenght" });

            if (!user.NewPassword.Any(char.IsUpper))
                return BadRequest(new { error = "Password must contain at least one uppercase letter!" });

            if (!user.NewPassword.Any(char.IsLower))
                return BadRequest(new { error = "Password must contain at least one lowercase letter!" });

            if (user.NewPassword != user.RepeatNewPassword)
                return BadRequest(new { error = "Password's are not same!"});

            var hashedNewPassword = _service.HashPassword(user.NewPassword);

            var resetFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, dbUser.Id),
                Builders<User>.Filter.Eq(u => u.PasswordResetTokenHash, HashResetToken(user.Token)),
                Builders<User>.Filter.Gt(u => u.PasswordResetExpiresAt, DateTime.UtcNow));
            var update = Builders<User>.Update
                .Set(u => u.Password, hashedNewPassword)
                .Unset(u => u.PasswordResetTokenHash)
                .Unset(u => u.PasswordResetExpiresAt);
            if ((await _users.UpdateOneAsync(resetFilter, update)).MatchedCount == 0)
                return BadRequest(new { error = "Invalid or expired reset token." });
            
            return Ok(new {message = "You have new password!"});
        }
        
        [HttpGet("store-settings")]
        public async Task<IActionResult> GetStoreSettings()
        {
            var settings = await _storeSettings.Find(s => s.Id == "store_settings").FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings { Id = "store_settings", CashOnDeliveryFee = 1.00m };
            }
            return Ok(settings);
        }

        private int GenerateVerificationCode()
        {
            var verificationCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000);
            return verificationCode;
        }

        private static string HashResetToken(string token) => Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    }
    
    public class EmailDto
    {
        public string Email { get; set; }
    }
}
