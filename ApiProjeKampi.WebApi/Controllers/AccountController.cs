using ApiProjeKampi.WebApi.Dtos;
using ApiProjeKampi.WebApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return BadRequest("Şifreler eşleşmiyor.");
            }

            var existingUser = await _userManager.FindByNameAsync(registerDto.UserName);

            if (existingUser != null)
            {
                return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");
            }

            var appUser = new AppUser
            {
                NameSurname = registerDto.NameSurname,
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(
                appUser,
                registerDto.Password
            );

            if (result.Succeeded)
            {
                return Ok("Kullanıcı başarıyla oluşturuldu.");
            }

            var errors = result.Errors
                .Select(x => x.Description)
                .ToList();

            return BadRequest(errors);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);

            if (user == null)
            {
                return BadRequest("Kullanıcı adı veya şifre hatalı.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                false
            );

            if (result.Succeeded)
            {
                return Ok("Giriş başarılı.");
            }

            return BadRequest("Kullanıcı adı veya şifre hatalı.");
        }
    }
}