
using ApiProjeKampi.WebUI.Dtos.ChefDtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class ChefController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChefController> _logger;

        private const string ApiUrl =
            "https://localhost:7020/api/Chefs";

        public ChefController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            ILogger<ChefController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
        }

        // =====================================
        // 1. ŞEF LİSTESİ
        // =====================================
        [HttpGet]
        public async Task<IActionResult> ChefList()
        {
            var chefs = new List<ResultChefDto>();

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response = await client.GetAsync(ApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json =
                        await response.Content.ReadAsStringAsync();

                    chefs = JsonConvert.DeserializeObject<List<ResultChefDto>>(json)
                            ?? new List<ResultChefDto>();
                }
                else
                {
                    TempData["ChefError"] =
                        "Şef listesi alınamadı. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şef listesi yüklenemedi.");

                TempData["ChefError"] =
                    "Şef servisine bağlanılamadı.";
            }

            return View(chefs);
        }

        // =====================================
        // 2. ŞEF EKLE - GET
        // =====================================
        [HttpGet]
        public IActionResult CreateChef()
        {
            return View(new CreateChefDto());
        }

        // =====================================
        // 3. ŞEF EKLE - POST
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreateChef(
            CreateChefDto createChefDto,
            IFormFile? imageFile)
        {
            // ImageUrl değeri dosya kaydedildikten sonra oluşturulacak.
            ModelState.Remove(nameof(CreateChefDto.ImageUrl));

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Lütfen bir şef fotoğrafı seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                return View(createChefDto);
            }

            string? newImageUrl = null;

            try
            {
                // Fotoğrafı sunucuya kaydet.
                newImageUrl = await SaveChefImageAsync(imageFile!);

                // Fotoğraf yolunu API'ye gönderilecek DTO'ya ekle.
                createChefDto.ImageUrl = newImageUrl;

                var client = _httpClientFactory.CreateClient();

                var json = JsonConvert.SerializeObject(createChefDto);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PostAsync(
                    ApiUrl,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ChefSuccess"] =
                        "Şef ve fotoğrafı başarıyla eklendi.";

                    return RedirectToAction(nameof(ChefList));
                }

                // API'den gelen gerçek hata mesajını oku.
                var errorBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Şef ekleme API hatası: HTTP {StatusCode}, {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);

                // Geliştirme ortamında ayrıntıyı göster.
                var errorMessage =
                    "Şef kaydedilemedi. API hata kodu: " +
                    (int)response.StatusCode;

                if (_environment.IsDevelopment())
                {
                    errorMessage += " - " + errorBody;
                }

                ModelState.AddModelError("", errorMessage);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Şef oluşturulurken hata oluştu.");

                ModelState.AddModelError(
                    "",
                    "Şef eklenirken beklenmeyen bir hata oluştu.");
            }

            // API kaydetmediyse kullanılmayan fotoğrafı temizle.
            DeleteUploadedImage(newImageUrl);

            return View(createChefDto);
        }

        // =====================================
        // 4. ŞEF GÜNCELLE - GET
        // =====================================
        [HttpGet]
        public async Task<IActionResult> UpdateChef(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response = await client.GetAsync(
                    ApiUrl + "/GetChef?id=" + id);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ChefError"] =
                        "Şef bilgileri alınamadı.";

                    return RedirectToAction(nameof(ChefList));
                }

                var json = await response.Content.ReadAsStringAsync();

                var chef = JsonConvert.DeserializeObject<GetChefByIdDto>(
                    json);

                if (chef == null)
                {
                    return NotFound();
                }

                return View(chef);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Şef bilgileri alınırken hata oluştu.");

                TempData["ChefError"] =
                    "Şef bilgileri yüklenemedi.";

                return RedirectToAction(nameof(ChefList));
            }
        }

        // =====================================
        // 5. ŞEF GÜNCELLE - POST
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UpdateChef(
            GetChefByIdDto chefDto,
            IFormFile? imageFile)
        {
            if (chefDto.ChefId <= 0)
            {
                return BadRequest();
            }

            ModelState.Remove(nameof(GetChefByIdDto.ImageUrl));

            if (!ModelState.IsValid)
            {
                return View(chefDto);
            }

            string? newImageUrl = null;

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Veritabanındaki mevcut şef bilgilerini getir.
                using var getResponse = await client.GetAsync(
                    ApiUrl + "/GetChef?id=" + chefDto.ChefId);

                if (!getResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        "Mevcut şef bilgileri getirilemedi.");

                    return View(chefDto);
                }

                var existingJson =
                    await getResponse.Content.ReadAsStringAsync();

                var existingChef =
                    JsonConvert.DeserializeObject<GetChefByIdDto>(
                        existingJson);

                if (existingChef == null)
                {
                    return NotFound();
                }

                // Fotoğraf seçilmemişse eski fotoğrafı koru.
                var finalImageUrl = existingChef.ImageUrl;

                if (imageFile != null && imageFile.Length > 0)
                {
                    newImageUrl = await SaveChefImageAsync(imageFile);
                    finalImageUrl = newImageUrl;
                }

                var updateDto = new UpdateChefDto
                {
                    ChefId = chefDto.ChefId,
                    NameSurname = chefDto.NameSurname,
                    Title = chefDto.Title,
                    Description = chefDto.Description,
                    ImageUrl = finalImageUrl
                };

                var json =
                    JsonConvert.SerializeObject(updateDto);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PutAsync(
                    ApiUrl,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ChefSuccess"] =
                        "Şef bilgileri ve fotoğrafı başarıyla güncellendi.";

                    return RedirectToAction(nameof(ChefList));
                }

                var errorBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Şef güncelleme API hatası: HTTP {StatusCode}, {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);

                var errorMessage =
                    "Şef güncellenemedi. API hata kodu: " +
                    (int)response.StatusCode;

                if (_environment.IsDevelopment())
                {
                    errorMessage += " - " + errorBody;
                }

                ModelState.AddModelError("", errorMessage);

                // Hata olursa ekranda mevcut fotoğraf gösterilsin.
                chefDto.ImageUrl = existingChef.ImageUrl;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Şef güncellenirken hata oluştu.");

                ModelState.AddModelError(
                    "",
                    "Şef güncellenirken beklenmeyen bir hata oluştu.");
            }

            DeleteUploadedImage(newImageUrl);

            return View(chefDto);
        }

        // =====================================
        // 6. ŞEF SİL
        // =====================================
        [HttpGet]
        public async Task<IActionResult> DeleteChef(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response = await client.DeleteAsync(
                    ApiUrl + "?id=" + id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ChefSuccess"] =
                        "Şef başarıyla silindi.";
                }
                else
                {
                    TempData["ChefError"] =
                        "Şef silinemedi. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şef silinirken hata oluştu.");

                TempData["ChefError"] =
                    "Şef silinirken hata oluştu.";
            }

            return RedirectToAction(nameof(ChefList));
        }

        // =====================================
        // 7. FOTOĞRAF KAYDETME METODU
        // =====================================
        private async Task<string> SaveChefImageAsync(
            IFormFile imageFile)
        {
            const long maxSize = 5 * 1024 * 1024;

            if (imageFile.Length == 0 ||
                imageFile.Length > maxSize)
            {
                throw new InvalidOperationException(
                    "Fotoğraf boş veya 5 MB'dan büyük olamaz.");
            }

            var extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");
            }

            var contentType = imageFile.ContentType.ToLowerInvariant();

            byte[] signature = new byte[12];

            using (var stream = imageFile.OpenReadStream())
            {
                int read = await stream.ReadAsync(
                    signature, 0, signature.Length);

                bool jpeg =
                    read >= 3 &&
                    signature[0] == 0xFF &&
                    signature[1] == 0xD8 &&
                    signature[2] == 0xFF;

                bool png =
                    read >= 8 &&
                    signature[0] == 0x89 &&
                    signature[1] == 0x50 &&
                    signature[2] == 0x4E &&
                    signature[3] == 0x47 &&
                    signature[4] == 0x0D &&
                    signature[5] == 0x0A &&
                    signature[6] == 0x1A &&
                    signature[7] == 0x0A;

                bool webp =
                    read >= 12 &&
                    Encoding.ASCII.GetString(
                        signature, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(
                        signature, 8, 4) == "WEBP";

                bool valid =
                    (jpeg &&
                     (extension == ".jpg" || extension == ".jpeg") &&
                     contentType == "image/jpeg")
                    ||
                    (png &&
                     extension == ".png" &&
                     contentType == "image/png")
                    ||
                    (webp &&
                     extension == ".webp" &&
                     contentType == "image/webp");

                if (!valid)
                {
                    throw new InvalidOperationException(
                        "Geçersiz fotoğraf dosyası.");
                }
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var folder = Path.Combine(
                webRoot,
                "uploads",
                "chefs");

            Directory.CreateDirectory(folder);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/uploads/chefs/" + fileName;
        }

        // =====================================
        // 8. KAYDEDİLEMEYEN FOTOĞRAFI TEMİZLE
        // =====================================
        private void DeleteUploadedImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) ||
                !imageUrl.StartsWith(
                    "/uploads/chefs/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var webRoot = _environment.WebRootPath
                    ?? Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");

                var fileName = Path.GetFileName(imageUrl);

                var filePath = Path.Combine(
                    webRoot,
                    "uploads",
                    "chefs",
                    fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Yüklenen fotoğraf temizlenemedi.");
            }
        }
    }
}
