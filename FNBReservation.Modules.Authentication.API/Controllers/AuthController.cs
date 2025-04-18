﻿using System;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResult = await _authService.AuthenticateAsync(loginDto);

            if (!authResult.Success)
                return Unauthorized(new { message = authResult.ErrorMessage });

            // Return minimal information to the client, now including outletId when applicable
            var response = new
            {
                success = true,
                role = authResult.Role,
                username = authResult.Username
            };

            // If the user is staff, include the outletId
            if (authResult.Role == "OutletStaff" && authResult.OutletId.HasValue)
            {
                return Ok(new
                {
                    success = true,
                    role = authResult.Role,
                    username = authResult.Username,
                    outletId = authResult.OutletId
                });
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var tokenResult = await _tokenService.RefreshTokenAsync();

            if (!tokenResult.Success)
                return Unauthorized(new { message = tokenResult.ErrorMessage });

            // If the user is staff, include the outletId
            if (tokenResult.Role == "OutletStaff" && tokenResult.OutletId.HasValue)
            {
                return Ok(new
                {
                    success = true,
                    role = tokenResult.Role,
                    username = tokenResult.Username,
                    outletId = tokenResult.OutletId
                });
            }

            // Return minimal information as tokens are in cookies
            return Ok(new
            {
                success = true,
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
            try
            {
                // Get the user ID from the claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User identifier not found in token" });
                }

                // Process the logout - this will revoke tokens and clear cookies
                await _authService.LogoutAsync(userId);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process");
                return StatusCode(500, new { message = "Error during logout process" });
            }
        }
    }
}