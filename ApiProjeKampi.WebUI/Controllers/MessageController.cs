
using ApiProjeKampi.WebUI.Dtos.MessageDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using static ApiProjeKampi.WebUI.Controllers.AIController;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class MessageController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MessageController> _logger;

        private const string MessageApiUrl =
            "https://localhost:7020/api/Messages";

        public MessageController(
            IHttpClientFactory httpClientFactory,
            ILogger<MessageController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ======================================
        // 1. MESAJ LİSTESİ
        // ======================================

        [HttpGet]
        public async Task<IActionResult> MessageList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response =
                    await client.GetAsync(MessageApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData =
                        await response.Content.ReadAsStringAsync();

                    var messages =
                        JsonConvert.DeserializeObject<List<ResultMessageDto>>(
                            jsonData);

                    return View(
                        messages ?? new List<ResultMessageDto>());
                }

                _logger.LogWarning(
                    "Mesaj listesi alınamadı. HTTP {StatusCode}",
                    (int)response.StatusCode);

                TempData["MessageError"] =
                    "Mesaj listesi alınamadı. API hata kodu: " +
                    (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj listesi getirilirken hata oluştu");

                TempData["MessageError"] =
                    "Mesaj servisine bağlanılamadı.";
            }

            return View(new List<ResultMessageDto>());
        }

        // ======================================
        // 2. MESAJ OLUŞTUR - GET
        // ======================================

        [HttpGet]
        public IActionResult CreateMessage()
        {
            return View(new CreateMessageDto());
        }

        // ======================================
        // 3. MESAJ OLUŞTUR - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(
            CreateMessageDto createMessageDto)
        {
            createMessageDto.Status = "Mesaj Alındı";

            ModelState.Remove(nameof(CreateMessageDto.Status));

            if (!ModelState.IsValid)
            {
                return View(createMessageDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var jsonData =
                    JsonConvert.SerializeObject(createMessageDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                using var response =
                    await client.PostAsync(MessageApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["MessageSuccess"] =
                        "Mesaj başarıyla oluşturuldu.";

                    return RedirectToAction(nameof(MessageList));
                }

                var errorBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Mesaj oluşturma hatası: HTTP {StatusCode}, {Body}",
                    (int)response.StatusCode,
                    errorBody);

                ModelState.AddModelError(
                    "",
                    "Mesaj oluşturulamadı. API hata kodu: " +
                    (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj oluşturulurken hata");

                ModelState.AddModelError(
                    "",
                    "Mesaj servisine bağlanılamadı.");
            }

            return View(createMessageDto);
        }

        // ======================================
        // 4. MESAJ SİL
        // ======================================

        public async Task<IActionResult> DeleteMessage(int id)
        {
            if (id <= 0)
            {
                TempData["MessageError"] =
                    "Geçersiz mesaj numarası.";

                return RedirectToAction(nameof(MessageList));
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response =
                    await client.DeleteAsync(
                        MessageApiUrl + "?id=" + id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["MessageSuccess"] =
                        "Mesaj başarıyla silindi.";
                }
                else
                {
                    TempData["MessageError"] =
                        "Mesaj silinemedi. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj silinirken hata");

                TempData["MessageError"] =
                    "Mesaj silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(MessageList));
        }

        // ======================================
        // 5. MESAJ DÜZENLE - GET
        // ======================================

        [HttpGet]
        public async Task<IActionResult> UpdateMessage(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response = await client.GetAsync(
                    MessageApiUrl + "/GetMessage?id=" + id);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["MessageError"] =
                        "Mesaj bilgileri alınamadı.";

                    return RedirectToAction(nameof(MessageList));
                }

                var jsonData =
                    await response.Content.ReadAsStringAsync();

                var value =
                    JsonConvert.DeserializeObject<GetMessageByIdDto>(
                        jsonData);

                if (value == null)
                {
                    return NotFound();
                }

                return View(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj düzenleme bilgileri alınırken hata");

                TempData["MessageError"] =
                    "Mesaj bilgileri alınırken hata oluştu.";

                return RedirectToAction(nameof(MessageList));
            }
        }

        // ======================================
        // 6. MESAJ DÜZENLE - POST
        // ======================================

        [HttpPost]
        public async Task<IActionResult> UpdateMessage(
            UpdateMessageDto updateMessageDto)
        {
            if (!ModelState.IsValid)
            {
                // Mevcut güncelleme View'ının model tipine
                // göre bu dönüş ayrıca düzenlenebilir.
                TempData["MessageError"] =
                    "Güncelleme bilgileri geçersiz.";

                return RedirectToAction(nameof(MessageList));
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var jsonData =
                    JsonConvert.SerializeObject(updateMessageDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                using var response =
                    await client.PutAsync(MessageApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["MessageSuccess"] =
                        "Mesaj başarıyla güncellendi.";
                }
                else
                {
                    TempData["MessageError"] =
                        "Mesaj güncellenemedi. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj güncellenirken hata");

                TempData["MessageError"] =
                    "Mesaj güncellenirken hata oluştu.";
            }

            return RedirectToAction(nameof(MessageList));
        }

        // ======================================
        // 7. AI İLE MESAJI AÇ
        // ======================================

        [HttpGet]
        public async Task<IActionResult> AsnwerMessageWithOpenAI(
            int id,
            string? prompt)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient();

            GetMessageByIdDto? message;

            try
            {
                using var response = await client.GetAsync(
                    MessageApiUrl + "/GetMessage?id=" + id);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["MessageError"] =
                        "Mesaj bilgileri alınamadı.";

                    return RedirectToAction(nameof(MessageList));
                }

                var jsonData =
                    await response.Content.ReadAsStringAsync();

                message =
                    JsonConvert.DeserializeObject<GetMessageByIdDto>(
                        jsonData);

                if (message == null)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI için mesaj bilgileri getirilemedi");

                TempData["MessageError"] =
                    "Mesaj bilgileri getirilemedi.";

                return RedirectToAction(nameof(MessageList));
            }

            // AI anahtarı yoksa sayfa yine açılır.
            // Anahtarı kaynak koda yazmak yerine
            // User Secrets / yapılandırma üzerinden kullan.
            var apiKey = Environment.GetEnvironmentVariable(
                "OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.answerAI =
                    "AI yanıtı için OPENAI_API_KEY yapılandırılmamış.";

                return View(message);
            }

            try
            {
                var client2 = _httpClientFactory.CreateClient();

                client2.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey);

                var requestData = new
                {
                    model = "gpt-4o-mini",

                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content =
                                "Sen bir restoranın müşteri hizmetleri " +
                                "asistanısın. Kullanıcı mesajlarına " +
                                "nazik, açıklayıcı ve yardımcı " +
                                "yanıt taslakları oluştur."
                        },
                        new
                        {
                            role = "user",
                            content = message.MessageDetails ?? ""
                        }
                    },

                    temperature = 0.5
                };

                using var response =
                    await client2.PostAsJsonAsync(
                        "https://api.openai.com/v1/chat/completions",
                        requestData);

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<
                            OpenAIResponse>();

                    var answer =
                        result?.choices?.FirstOrDefault()
                            ?.message?.content;

                    ViewBag.answerAI =
                        string.IsNullOrWhiteSpace(answer)
                        ? "AI yanıtı oluşturulamadı."
                        : answer;
                }
                else
                {
                    ViewBag.answerAI =
                        "AI servisi hata döndürdü. HTTP " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI mesaj yanıtı oluşturulurken hata");

                ViewBag.answerAI =
                    "AI yanıtı oluşturulurken hata oluştu.";
            }

            return View(message);
        }

        // ======================================
        // 8. İLETİŞİM FORMU - GET
        // ======================================

        [HttpGet]
        public PartialViewResult SendMessage()
        {
            return PartialView(new CreateMessageDto());
        }

        // ======================================
        // 9. İLETİŞİM FORMU - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            CreateMessageDto createMessageDto)
        {
            // İletişim formunda bu alan bulunmadığı için
            // durumu sunucuda otomatik belirliyoruz.
            createMessageDto.Status = "Mesaj Alındı";

            ModelState.Remove(nameof(CreateMessageDto.Status));

            // Form bilgilerini kontrol et.
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value != null &&
                                x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(
                        e => $"{x.Key}: {e.ErrorMessage}"));

                var errorText =
                    string.Join(" | ", validationErrors);

                _logger.LogWarning(
                    "Mesaj formu doğrulama hatası: {Errors}",
                    errorText);

                TempData["MessageError"] =
                    "Lütfen mesaj bilgilerinizi kontrol edin. " +
                    errorText;

                return Redirect("/Default/Index#contact");
            }

            try
            {
                // Mesaj kaydı, çeviri veya AI servisine
                // bağlı olmadan doğrudan API'ye yapılır.
                var client = _httpClientFactory.CreateClient();

                var jsonData =
                    JsonConvert.SerializeObject(createMessageDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                using var response =
                    await client.PostAsync(MessageApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["MessageSuccess"] =
                        "Mesajınız başarıyla gönderildi. " +
                        "En kısa sürede sizinle iletişime geçeceğiz.";
                }
                else
                {
                    var errorBody =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogWarning(
                        "Mesaj API hatası. HTTP {StatusCode}: {Body}",
                        (int)response.StatusCode,
                        errorBody);

                    TempData["MessageError"] =
                        "Mesajınız gönderilemedi. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj servisi bağlantı hatası");

                TempData["MessageError"] =
                    "Mesaj servisine bağlanılamadı. " +
                    "WebApi projesinin çalıştığını kontrol edin.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj gönderme isteği zaman aşımı");

                TempData["MessageError"] =
                    "Mesaj gönderimi zaman aşımına uğradı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mesaj gönderilirken beklenmeyen hata");

                TempData["MessageError"] =
                    "Mesaj gönderilirken bir hata oluştu.";
            }

            return Redirect("/Default/Index#contact");
        }
    }
}
