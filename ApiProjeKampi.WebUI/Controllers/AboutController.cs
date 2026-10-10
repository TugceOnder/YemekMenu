
using ApiProjeKampi.WebUI.Dtos.AboutDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class AboutController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        private const string ApiUrl =
            "https://localhost:7020/api/Abouts";

        public AboutController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        // FOTOĞRAF YÜKLE
        private async Task<string> SaveAboutImageAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
                .Contains(extension))
            {
                throw new InvalidOperationException(
                    "Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");
            }

            if (file.Length == 0 ||
                file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Fotoğraf en fazla 5 MB olabilir.");
            }

            byte[] header = new byte[12];

            using (var stream = file.OpenReadStream())
            {
                int read = await stream.ReadAsync(
                    header, 0, header.Length);

                bool jpeg = read >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF;

                bool png = read >= 8 &&
                    header.Take(8).SequenceEqual(
                        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

                bool webp = read >= 12 &&
                    Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

                bool valid = extension switch
                {
                    ".jpg" or ".jpeg" => jpeg,
                    ".png" => png,
                    ".webp" => webp,
                    _ => false
                };

                if (!valid)
                {
                    throw new InvalidOperationException(
                        "Geçersiz görsel dosyası.");
                }
            }

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath, "wwwroot");

            string folder = Path.Combine(
                webRoot, "uploads", "abouts");

            Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return $"{Request.Scheme}://{Request.Host}" +
                   $"/uploads/abouts/{fileName}";
        }

        // ZORUNLU ALANLARI KONTROL ET
        private void ValidateAbout(string? title, string? description)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "Title", "Başlık boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError(
                    "Description", "Açıklama boş bırakılamaz.");
            }
        }

        // HAKKIMIZDA LİSTESİ
        [HttpGet]
        public async Task<IActionResult> AboutList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Hakkımızda verileri alınamadı.";
                    return View(new List<ResultAboutDto>());
                }

                var json = await response.Content.ReadAsStringAsync();

                var values =
                    JsonConvert.DeserializeObject<List<ResultAboutDto>>(
                        json) ?? new List<ResultAboutDto>();

                return View(values);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "WebApi bağlantısı kurulamadı.";
                return View(new List<ResultAboutDto>());
            }
        }

        // EKLEME GET
        [HttpGet]
        public IActionResult CreateAbout()
        {
            return View(new CreateAboutDto());
        }

        // EKLEME POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAbout(
            CreateAboutDto createAboutDto,
            IFormFile? imageFile)
        {
            // Görsel ve video alanları formdan gelmiyor
            ModelState.Remove(nameof(CreateAboutDto.ImageUrl));
            ModelState.Remove(nameof(CreateAboutDto.VideoUrl));
            ModelState.Remove(nameof(CreateAboutDto.VideoCoverImageUrl));

            ValidateAbout(
                createAboutDto.Title,
                createAboutDto.Description);

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "imageFile", "Lütfen fotoğraf seçiniz.");
            }

            if (!ModelState.IsValid)
                return View(createAboutDto);

            try
            {
                createAboutDto.ImageUrl =
                    await SaveAboutImageAsync(imageFile!);

                // Video yönetimden kaldırıldı
                createAboutDto.VideoUrl = "";
                createAboutDto.VideoCoverImageUrl = "";

                var client = _httpClientFactory.CreateClient();

                var json =
                    JsonConvert.SerializeObject(createAboutDto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Hakkımızda alanı başarıyla eklendi.";

                    return RedirectToAction(nameof(AboutList));
                }

                ModelState.AddModelError(
                    "",
                    "Kayıt başarısız: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "", "Fotoğraf kaydedilemedi.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "WebApi bağlantısı kurulamadı.");
            }

            return View(createAboutDto);
        }

        // SİLME
        [HttpGet]
        public async Task<IActionResult> DeleteAbout(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.DeleteAsync(
                    ApiUrl + "?id=" + id);

                TempData[
                    response.IsSuccessStatusCode
                        ? "SuccessMessage"
                        : "ErrorMessage"
                ] = response.IsSuccessStatusCode
                    ? "Hakkımızda kaydı silindi."
                    : "Hakkımızda kaydı silinemedi.";
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "WebApi bağlantısı kurulamadı.";
            }

            return RedirectToAction(nameof(AboutList));
        }

        // GÜNCELLEME GET
        [HttpGet]
        public async Task<IActionResult> UpdateAbout(int id)
        {
            if (id <= 0)
                return BadRequest("Geçersiz kayıt numarası.");

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(
                    ApiUrl + "/GetAbout?id=" + id);

                if (!response.IsSuccessStatusCode)
                    return NotFound("Hakkımızda kaydı bulunamadı.");

                var json = await response.Content.ReadAsStringAsync();

                var value =
                    JsonConvert.DeserializeObject<GetAboutByIdDto>(json);

                return value == null ? NotFound() : View(value);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503, "WebApi bağlantısı kurulamadı.");
            }
        }

        // GÜNCELLEME POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAbout(
            UpdateAboutDto updateAboutDto,
            IFormFile? imageFile,
            bool removeImage)
        {
            ModelState.Remove(nameof(UpdateAboutDto.ImageUrl));
            ModelState.Remove(nameof(UpdateAboutDto.VideoUrl));
            ModelState.Remove(nameof(UpdateAboutDto.VideoCoverImageUrl));

            ValidateAbout(
                updateAboutDto.Title,
                updateAboutDto.Description);

            if (updateAboutDto.AboutId <= 0)
            {
                ModelState.AddModelError(
                    "AboutId", "Geçersiz kayıt numarası.");
            }

            if (!ModelState.IsValid)
                return View("UpdateAbout", ToViewModel(updateAboutDto));

            try
            {
                var client = _httpClientFactory.CreateClient();

                var getResponse = await client.GetAsync(
                    ApiUrl + "/GetAbout?id=" + updateAboutDto.AboutId);

                if (!getResponse.IsSuccessStatusCode)
                    return NotFound("Hakkımızda kaydı bulunamadı.");

                var currentJson =
                    await getResponse.Content.ReadAsStringAsync();

                var current =
                    JsonConvert.DeserializeObject<GetAboutByIdDto>(
                        currentJson);

                if (current == null)
                    return NotFound();

                bool hasNewImage =
                    imageFile != null && imageFile.Length > 0;

                bool hasExistingImage =
                    !string.IsNullOrWhiteSpace(current.ImageUrl);

                // Görsel boş bırakılamaz
                if (!hasNewImage &&
                    (removeImage || !hasExistingImage))
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Fotoğraf zorunludur. Yeni fotoğraf seçiniz " +
                        "veya Geri Al'a basınız.");

                    updateAboutDto.ImageUrl =
                        removeImage ? "" : current.ImageUrl;

                    return View(
                        "UpdateAbout",
                        ToViewModel(updateAboutDto));
                }

                // Mevcut fotoğrafı koru
                updateAboutDto.ImageUrl = current.ImageUrl;

                // Yeni fotoğraf varsa değiştir
                if (hasNewImage)
                {
                    updateAboutDto.ImageUrl =
                        await SaveAboutImageAsync(imageFile!);
                }

                // Video alanı formdan kaldırıldı.
                // Eski verileri kazara silmiyoruz.
                updateAboutDto.VideoUrl = current.VideoUrl;
                updateAboutDto.VideoCoverImageUrl =
                    current.VideoCoverImageUrl;

                var json = JsonConvert.SerializeObject(updateAboutDto);

                using var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Hakkımızda alanı güncellendi.";

                    return RedirectToAction(nameof(AboutList));
                }

                ModelState.AddModelError(
                    "",
                    "Güncelleme başarısız: " +
                    await response.Content.ReadAsStringAsync());

                return View(
                    "UpdateAbout",
                    ToViewModel(updateAboutDto));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "", "Fotoğraf kaydedilemedi.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "WebApi bağlantısı kurulamadı.");
            }

            return View("UpdateAbout", ToViewModel(updateAboutDto));
        }

        // GET DTO ile GÜNCELLEME SAYFASINI UYUMLU TUT
        private GetAboutByIdDto ToViewModel(UpdateAboutDto dto)
        {
            return new GetAboutByIdDto
            {
                AboutId = dto.AboutId,
                Title = dto.Title,
                ReservationNumber = dto.ReservationNumber,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl,
                VideoCoverImageUrl = dto.VideoCoverImageUrl
            };
        }
    }
}
