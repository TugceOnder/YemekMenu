
using ApiProjeKampi.WebUI.Dtos.FeatureDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class FeatureController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        private const string ApiUrl =
            "https://localhost:7020/api/Features";

        public FeatureController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        // FOTOĞRAF YÜKLEME
        private async Task<string> SaveFeatureImageAsync(
            IFormFile file)
        {
            string extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Yalnızca JPG, PNG veya WEBP fotoğraf yükleyebilirsiniz.");
            }

            if (file.Length == 0 ||
                file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Fotoğraf boş olamaz ve 5 MB boyutunu geçemez.");
            }

            // Dosyanın temel imzasını kontrol et
            byte[] header = new byte[12];

            using (var input = file.OpenReadStream())
            {
                int read = await input.ReadAsync(
                    header, 0, header.Length);

                bool jpeg = read >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF;

                bool png = read >= 8 &&
                    header.Take(8).SequenceEqual(
                        new byte[]
                        {
                            137, 80, 78, 71, 13, 10, 26, 10
                        });

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
                        "Geçersiz fotoğraf dosyası.");
                }
            }

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath, "wwwroot");

            string folder = Path.Combine(
                webRoot, "uploads", "features");

            Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(folder, fileName);

            using (var output = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(output);
            }

            return $"{Request.Scheme}://{Request.Host}" +
                   $"/uploads/features/{fileName}";
        }

        // ORTAK ZORUNLU ALAN KONTROLÜ
        private void ValidateFeatureFields(
            string? title,
            string? subTitle,
            string? description)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "Title", "Başlık boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(subTitle))
            {
                ModelState.AddModelError(
                    "SubTitle", "Alt başlık boş bırakılamaz.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError(
                    "Description", "Açıklama boş bırakılamaz.");
            }
        }

        // ÖNE ÇIKAN ALANLAR LİSTESİ
        [HttpGet]
        public async Task<IActionResult> FeatureList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Öne çıkan alanlar getirilemedi.";

                    return View(new List<ResultFeatureDto>());
                }

                var json = await response.Content.ReadAsStringAsync();

                var values =
                    JsonConvert.DeserializeObject<List<ResultFeatureDto>>(
                        json) ?? new List<ResultFeatureDto>();

                return View(values);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "WebApi bağlantısı kurulamadı.";

                return View(new List<ResultFeatureDto>());
            }
        }

        // EKLEME - GET
        [HttpGet]
        public IActionResult CreateFeature()
        {
            return View(new CreateFeatureDto());
        }

        // EKLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeature(
            CreateFeatureDto createFeatureDto,
            IFormFile? imageFile)
        {
            // Görsel URL ve video alanı formdan gelmiyor.
            ModelState.Remove(nameof(CreateFeatureDto.ImageUrl));
            ModelState.Remove(nameof(CreateFeatureDto.VideoUrl));

            ValidateFeatureFields(
                createFeatureDto.Title,
                createFeatureDto.SubTitle,
                createFeatureDto.Description);

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Lütfen bir fotoğraf seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                return View(createFeatureDto);
            }

            try
            {
                // Fotoğraf zorunlu.
                createFeatureDto.ImageUrl =
                    await SaveFeatureImageAsync(imageFile!);

                // Video alanını kaldırdık.
                createFeatureDto.VideoUrl = "";

                var client = _httpClientFactory.CreateClient();

                var json =
                    JsonConvert.SerializeObject(createFeatureDto);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Öne çıkan alan başarıyla eklendi.";

                    return RedirectToAction(nameof(FeatureList));
                }

                ModelState.AddModelError(
                    "",
                    "Kayıt başarısız: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    "imageFile", ex.Message);
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "", "Fotoğraf sunucuya kaydedilemedi.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "WebApi bağlantısı kurulamadı.");
            }

            return View(createFeatureDto);
        }

        // SİLME
        [HttpGet]
        public async Task<IActionResult> DeleteFeature(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.DeleteAsync(
                    ApiUrl + "?id=" + id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Öne çıkan alan silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Öne çıkan alan silinemedi: " +
                        await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "WebApi bağlantısı kurulamadı.";
            }

            return RedirectToAction(nameof(FeatureList));
        }

        // GÜNCELLEME - GET
        [HttpGet]
        public async Task<IActionResult> UpdateFeature(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Geçersiz kayıt numarası.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(
                    ApiUrl + "/GetFeature?id=" + id);

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound(
                        "Öne çıkan alan bulunamadı.");
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                var feature =
                    JsonConvert.DeserializeObject<GetFeatureByIdDto>(
                        json);

                if (feature == null)
                    return NotFound();

                return View(feature);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "WebApi bağlantısı kurulamadı.");
            }
        }

        // GÜNCELLEME - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFeature(
            UpdateFeatureDto updateFeatureDto,
            IFormFile? imageFile,
            bool removeImage)
        {
            // Görsel URL'si formdan gelmiyor.
            ModelState.Remove(nameof(UpdateFeatureDto.ImageUrl));

            // Video alanı kaldırıldı; mevcut video verisi
            // aşağıda API'den alınarak korunacak.
            ModelState.Remove(nameof(UpdateFeatureDto.VideoUrl));

            ValidateFeatureFields(
                updateFeatureDto.Title,
                updateFeatureDto.SubTitle,
                updateFeatureDto.Description);

            if (updateFeatureDto.FeatureId <= 0)
            {
                ModelState.AddModelError(
                    "FeatureId",
                    "Geçersiz kayıt numarası.");
            }

            // Form hatalıysa API'ye güncelleme gönderme.
            if (!ModelState.IsValid)
            {
                return View(
                    "UpdateFeature",
                    CreateUpdateViewModel(updateFeatureDto));
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Kayıtlı Feature bilgilerini getir.
                var getResponse = await client.GetAsync(
                    ApiUrl + "/GetFeature?id=" +
                    updateFeatureDto.FeatureId);

                if (!getResponse.IsSuccessStatusCode)
                {
                    return NotFound(
                        "Öne çıkan alan bulunamadı.");
                }

                var currentJson =
                    await getResponse.Content.ReadAsStringAsync();

                var current =
                    JsonConvert.DeserializeObject<GetFeatureByIdDto>(
                        currentJson);

                if (current == null)
                {
                    return NotFound(
                        "Öne çıkan alan bulunamadı.");
                }

                bool hasNewImage =
                    imageFile != null && imageFile.Length > 0;

                bool hasExistingImage =
                    !string.IsNullOrWhiteSpace(current.ImageUrl);

                // Fotoğraf boş kalamaz.
                if (!hasNewImage &&
                    (removeImage || !hasExistingImage))
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Fotoğraf boş bırakılamaz. " +
                        "Lütfen fotoğraf seçiniz veya Geri Al'a basınız.");

                    updateFeatureDto.ImageUrl = removeImage
                        ? ""
                        : current.ImageUrl;

                    updateFeatureDto.VideoUrl = current.VideoUrl;

                    return View(
                        "UpdateFeature",
                        CreateUpdateViewModel(updateFeatureDto));
                }

                // Önce mevcut fotoğrafı koru.
                updateFeatureDto.ImageUrl =
                    current.ImageUrl;

                // Kullanıcı yeni fotoğraf seçtiyse değiştir.
                if (hasNewImage)
                {
                    updateFeatureDto.ImageUrl =
                        await SaveFeatureImageAsync(imageFile!);
                }

                // Video formdan kaldırıldı.
                // Eski veriyi yanlışlıkla silmemek için koruyoruz.
                updateFeatureDto.VideoUrl =
                    current.VideoUrl;

                var json =
                    JsonConvert.SerializeObject(updateFeatureDto);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PutAsync(
                    ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Öne çıkan alan başarıyla güncellendi.";

                    return RedirectToAction(nameof(FeatureList));
                }

                ModelState.AddModelError(
                    "",
                    "Güncelleme başarısız: " +
                    await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    "imageFile", ex.Message);
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    "", "Fotoğraf sunucuya kaydedilemedi.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "WebApi bağlantısı kurulamadı.");
            }

            return View(
                "UpdateFeature",
                CreateUpdateViewModel(updateFeatureDto));
        }

        // GÜNCELLEME VIEW MODELİNİ OLUŞTUR
        private GetFeatureByIdDto CreateUpdateViewModel(
            UpdateFeatureDto dto)
        {
            return new GetFeatureByIdDto
            {
                FeatureId = dto.FeatureId,
                Title = dto.Title,
                SubTitle = dto.SubTitle,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                VideoUrl = dto.VideoUrl
            };
        }
    }
}
