using ApiProjeKampi.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View(model);
            }

            var client = _httpClientFactory.CreateClient();

            var jsonData = JsonConvert.SerializeObject(model);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "https://localhost:7020/api/Account/Register",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Hesabınız başarıyla oluşturuldu. Giriş yapabilirsiniz.";

                return RedirectToAction("Login");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();

            ViewBag.Error = errorMessage;

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var jsonData = JsonConvert.SerializeObject(model);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "https://localhost:7020/api/Account/Login",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.UserName)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal
                );

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }
    }
}