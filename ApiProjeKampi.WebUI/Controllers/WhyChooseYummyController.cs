
using ApiProjeKampi.WebUI.Dtos.WhyChooseYummyDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class WhyChooseYummyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        // Mevcut projen Service kayıtlarını kullanıyor.
        private const string ApiUrl =
            "https://localhost:7020/api/Services";

        public WhyChooseYummyController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
                .Contains(extension))
                throw new InvalidOperationException(
                    "Sadece JPG, PNG ve WEBP yükleyebilirsiniz.");

            if (file.Length == 0 ||
                file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException(
                    "Görsel 5 MB sınırını aşamaz.");

            byte[] header = new byte[12];
            using (var input = file.OpenReadStream())
            {
                int count = await input.ReadAsync(
                    header, 0, header.Length);

                bool jpg = count >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF;

                bool png = count >= 8 &&
                    header.Take(8).SequenceEqual(
                        new byte[] {
                            137, 80, 78, 71, 13, 10, 26, 10
                        });

                bool webp = count >= 12 &&
                    Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

                if (!(extension == ".jpg" || extension == ".jpeg"
                        ? jpg
                        : extension == ".png" ? png : webp))
                    throw new InvalidOperationException(
                        "Geçersiz görsel dosyası.");
            }

            var root = _environment.WebRootPath ??
                Path.Combine(_environment.ContentRootPath, "wwwroot");

            var folder = Path.Combine(root, "uploads", "whychoose");
            Directory.CreateDirectory(folder);

            var name = Guid.NewGuid().ToString("N") + extension;
            var path = Path.Combine(folder, name);

            await using (var output = new FileStream(
                path, FileMode.CreateNew))
            {
                await file.CopyToAsync(output);
            }

            return $"{Request.Scheme}://{Request.Host}" +
                   $"/uploads/whychoose/{name}";
        }

        private static GetWhyChooseYummyByIdDto ToViewModel(
            UpdateWhyChooseYummyDto dto)
        {
            return new GetWhyChooseYummyByIdDto
            {
                ServiceId = dto.ServiceId,
                Title = dto.Title,
                Description = dto.Description,
                IconUrl = dto.IconUrl
            };
        }

        [HttpGet]
        public async Task<IActionResult> WhyChooseYummyList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Kayıtlar alınamadı.";
                    return View(new List<ResultWhyChooseYummyDto>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<
                    List<ResultWhyChooseYummyDto>>(json);

                return View(values ?? new List<ResultWhyChooseYummyDto>());
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "WebApi bağlantısı kurulamadı.";
                return View(new List<ResultWhyChooseYummyDto>());
            }
        }

        [HttpGet]
        public IActionResult CreateWhyChooseYummy()
        {
            return View(new CreateWhyChooseYummyDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWhyChooseYummy(
            CreateWhyChooseYummyDto dto,
            IFormFile? imageFile)
        {
            ModelState.Remove(nameof(CreateWhyChooseYummyDto.IconUrl));

            if (string.IsNullOrWhiteSpace(dto.Title))
                ModelState.AddModelError("Title", "Başlık zorunludur.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                ModelState.AddModelError(
                    "Description", "Açıklama zorunludur.");

            if (imageFile == null || imageFile.Length == 0)
                ModelState.AddModelError(
                    "imageFile", "Görsel seçmek zorunludur.");

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                dto.IconUrl = await SaveImageAsync(imageFile!);

                var client = _httpClientFactory.CreateClient();
                var json = JsonConvert.SerializeObject(dto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(WhyChooseYummyList));

                ModelState.AddModelError("",
                    "Ekleme başarısız: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "API bağlantısı kurulamadı.");
            }
            catch (IOException)
            {
                ModelState.AddModelError("", "Görsel kaydedilemedi.");
            }

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateWhyChooseYummy(int id)
        {
            if (id <= 0) return BadRequest();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(
                    ApiUrl + "/GetService?id=" + id);

                if (!response.IsSuccessStatusCode)
                    return NotFound("Kayıt bulunamadı.");

                var json = await response.Content.ReadAsStringAsync();
                var value = JsonConvert.DeserializeObject<
                    GetWhyChooseYummyByIdDto>(json);

                return value == null ? NotFound() : View(value);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "API bağlantısı kurulamadı.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWhyChooseYummy(
            UpdateWhyChooseYummyDto dto,
            IFormFile? imageFile,
            bool removeImage)
        {
            ModelState.Remove(nameof(UpdateWhyChooseYummyDto.IconUrl));

            if (string.IsNullOrWhiteSpace(dto.Title))
                ModelState.AddModelError("Title", "Başlık zorunludur.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                ModelState.AddModelError(
                    "Description", "Açıklama zorunludur.");

            if (dto.ServiceId <= 0)
                ModelState.AddModelError("ServiceId", "Geçersiz kayıt.");

            if (!ModelState.IsValid)
                return View("UpdateWhyChooseYummy", ToViewModel(dto));

            try
            {
                var client = _httpClientFactory.CreateClient();

                var currentResponse = await client.GetAsync(
                    ApiUrl + "/GetService?id=" + dto.ServiceId);

                if (!currentResponse.IsSuccessStatusCode)
                    return NotFound("Kayıt bulunamadı.");

                var currentJson =
                    await currentResponse.Content.ReadAsStringAsync();

                var current = JsonConvert.DeserializeObject<
                    GetWhyChooseYummyByIdDto>(currentJson);

                if (current == null) return NotFound();

                bool hasNewImage =
                    imageFile != null && imageFile.Length > 0;

                dto.IconUrl = current.IconUrl;

                if (!hasNewImage &&
                    (removeImage ||
                     string.IsNullOrWhiteSpace(current.IconUrl)))
                {
                    ModelState.AddModelError("imageFile",
                        "Görsel boş bırakılamaz. Yeni görsel seçin " +
                        "veya Geri Al'a basın.");

                    return View(
                        "UpdateWhyChooseYummy", ToViewModel(dto));
                }

                if (hasNewImage)
                    dto.IconUrl = await SaveImageAsync(imageFile!);

                var json = JsonConvert.SerializeObject(dto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(WhyChooseYummyList));

                ModelState.AddModelError("",
                    "Güncelleme başarısız: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "API bağlantısı kurulamadı.");
            }
            catch (IOException)
            {
                ModelState.AddModelError("", "Görsel kaydedilemedi.");
            }

            return View("UpdateWhyChooseYummy", ToViewModel(dto));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteWhyChooseYummy(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.DeleteAsync(
                    ApiUrl + "?id=" + id);

                TempData[response.IsSuccessStatusCode
                    ? "SuccessMessage" : "ErrorMessage"] =
                    response.IsSuccessStatusCode
                    ? "Kayıt silindi." : "Kayıt silinemedi.";
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "API bağlantısı kurulamadı.";
            }

            return RedirectToAction(nameof(WhyChooseYummyList));
        }
    }
}
