using ApiProjeKampi.WebUI.Dtos.YummyEventDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class YummyEventController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string BaseApiUrl = "https://localhost:7020/api/YummyEvents";

        public YummyEventController(IHttpClientFactory httpClientFactory, IWebHostEnvironment webHostEnvironment)
        {
            _httpClientFactory = httpClientFactory;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> YummyEventList()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(BaseApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultYummyEventDto>>(json);
                return View(values);
            }
            return View();
        }

        [HttpGet]
        public IActionResult CreateYummyEvent()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateYummyEvent(CreateYummyEventDto dto, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var root = _webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
                var folder = Path.Combine(root, "images", "events");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var extension = Path.GetExtension(imageFile.FileName);
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                dto.ImageUrl = "/images/events/" + fileName;
            }

            var client = _httpClientFactory.CreateClient();
            var jsonData = JsonConvert.SerializeObject(dto);
            var stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(BaseApiUrl, stringContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("YummyEventList");
            }

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateYummyEvent(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseApiUrl}/GetYummyEvent?id={id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var value = JsonConvert.DeserializeObject<GetYummyEventByIdDto>(json);
                return View(value);
            }
            return RedirectToAction("YummyEventList");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateYummyEvent(GetYummyEventByIdDto dto, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var root = _webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
                var folder = Path.Combine(root, "images", "events");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var extension = Path.GetExtension(imageFile.FileName);
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                dto.ImageUrl = "/images/events/" + fileName;
            }

            var client = _httpClientFactory.CreateClient();
            var jsonData = JsonConvert.SerializeObject(dto);
            var stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.PutAsync(BaseApiUrl, stringContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("YummyEventList");
            }

            return View(dto);
        }

        public async Task<IActionResult> DeleteYummyEvent(int id)
        {
            var client = _httpClientFactory.CreateClient();
            await client.DeleteAsync($"{BaseApiUrl}?id={id}");
            return RedirectToAction("YummyEventList");
        }
    }
}