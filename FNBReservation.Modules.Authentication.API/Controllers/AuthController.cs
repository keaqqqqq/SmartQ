using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Interfaces;

namespace FNBReservation.Modules.Authentication.API.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ITokenService tokenService, IEmailService emailService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize]
        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            _logger.LogInformation("TestAuth endpoint called");

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            _logger.LogInformation("Claims found: {ClaimsCount}", claims.Count);

            foreach (var claim in claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            return Ok(new
            {
                message = "Authentication is working correctly",
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userIdClaim = User.FindFirst("nameid")?.Value,
                customUserId = User.FindFirst("UserId")?.Value,
                username = User.FindFirst(ClaimTypes.Name)?.Value,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                allClaims = claims
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResult = await _authService.AuthenticateAsync(loginDto);

            if (!authResult.Success)
                return Unauthorized(new { message = authResult.ErrorMessage });

            return Ok(new
            {
                accessToken = authResult.AccessToken,
                refreshToken = authResult.RefreshToken,
                expiresIn = authResult.ExpiresIn,
                role = authResult.Role,
                username = authResult.Username
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tokenResult = await _tokenService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

            if (!tokenResult.Success)
                return Unauthorized(new { message = tokenResult.ErrorMessage });

            return Ok(new
            {
                accessToken = tokenResult.AccessToken,
                refreshToken = tokenResult.RefreshToken,
                expiresIn = tokenResult.ExpiresIn,
                role = tokenResult.Role,
                username = tokenResult.Username
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);

            // Always return OK to prevent email enumeration attacks
            return Ok(new { message = "If your email exists in our system, you will receive a password reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string decodedToken = System.Web.HttpUtility.UrlDecode(resetPasswordDto.Token);

            var result = await _authService.ResetPasswordAsync(decodedToken, resetPasswordDto.NewPassword);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Password has been reset successfully." });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Try to get the user ID from the NameIdentifier claim
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"Initial userId from NameIdentifier: {userId ?? "null"}");

            // If that fails, try to get it from the "nameid" claim directly
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst("nameid")?.Value;
                Console.WriteLine($"userId from 'nameid' claim: {userId ?? "null"}");
            }

            // If we still don't have a user ID, try the "UserId" custom claim
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst("UserId")?.Value;
                Console.WriteLine($"userId from 'UserId' claim: {userId ?? "null"}");
            }

            // Finally, try to use any available identifier
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst(ClaimTypes.Name)?.Value ??
                        User.FindFirst("unique_name")?.Value;
                Console.WriteLine($"userId from Name or unique_name: {userId ?? "null"}");
            }

            // Log all claims for debugging
            Console.WriteLine("All claims in the token:");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            // If we still don't have any identifier, return unauthorized
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("No valid user identifier found in token");
                return Unauthorized(new { message = "User identifier not found in token" });
            }

            Console.WriteLine($"Final userId being passed to LogoutAsync: {userId}");

            try
            {
                // Process the logout
                await _authService.LogoutAsync(userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in logout: {ex.Message}");
                return StatusCode(500, new { message = "Error during logout process", error = ex.Message });
            }
        }



        /// <summary>
        /// FOR DEVELOPMENT TESTING ONLY - DO NOT USE IN PRODUCTION
        /// </summary>
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmailService([FromBody] TestEmailDto testEmailDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Generate a test token
                var testToken = Guid.NewGuid().ToString();

                // Send test email
                await _emailService.SendPasswordResetEmailAsync(testEmailDto.Email, testToken);

                return Ok(new { message = $"Test email sent to {testEmailDto.Email}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send email: {ex.Message}" });
            }
        }

    }
}