
using ApiProjeKampi.WebUI.Dtos.ReservationDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class DefaultController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DefaultController> _logger;

        private const string ReservationApiUrl =
            "https://localhost:7020/api/Reservations";

        public DefaultController(
            IHttpClientFactory httpClientFactory,
            ILogger<DefaultController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new CreateReservationDto
            {
                ReservationDate = DateTime.Today,
                ReservationStatus = "Onay Bekliyor"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            CreateReservationDto createReservationDto)
        {
            // Rezervasyon durumu sunucuda belirlenir.
            createReservationDto.ReservationStatus = "Onay Bekliyor";

            // Mesaj boş bırakılabilir.
            createReservationDto.Message ??= string.Empty;

            // Formdan gelmeyen alanların doğrulamasını yenile.
            ModelState.Remove(
                nameof(CreateReservationDto.ReservationStatus));

            ModelState.Remove(
                nameof(CreateReservationDto.Message));

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value != null &&
                                x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e =>
                        $"{x.Key}: {e.ErrorMessage}"));

                var errorText = string.Join(" | ", errors);

                _logger.LogWarning(
                    "Rezervasyon doğrulama hatası: {Errors}",
                    errorText);

                ViewBag.ReservationError =
                    "Form doğrulama hatası: " + errorText;

                return View(createReservationDto);
            }

            // Geçmiş tarihlere rezervasyon oluşturulmasın.
            if (createReservationDto.ReservationDate.Date < DateTime.Today)
            {
                ViewBag.ReservationError =
                    "Geçmiş tarihler için rezervasyon oluşturamazsınız.";

                return View(createReservationDto);
            }

            if (createReservationDto.CountofPeople < 1)
            {
                ViewBag.ReservationError =
                    "Kişi sayısı en az 1 olmalıdır.";

                return View(createReservationDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var jsonData = JsonConvert.SerializeObject(
                    createReservationDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PostAsync(
                    ReservationApiUrl,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ReservationSuccess"] =
                        "Rezervasyon talebiniz başarıyla alınmıştır. " +
                        "Ekibimiz en kısa sürede sizinle iletişime geçecektir.";

                    return RedirectToAction(nameof(Index));
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Rezervasyon API hatası. HTTP: {StatusCode}, Yanıt: {Body}",
                    (int)response.StatusCode,
                    responseBody);

                ViewBag.ReservationError =
                    $"Rezervasyon kaydedilemedi. " +
                    $"API hata kodu: {(int)response.StatusCode}. " +
                    "Ayrıntılar uygulama loglarında.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon API bağlantı hatası");

                ViewBag.ReservationError =
                    "Rezervasyon servisine bağlanılamadı. " +
                    "WebApi projesinin çalıştığından emin olun.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon isteği zaman aşımı");

                ViewBag.ReservationError =
                    "Rezervasyon isteği zaman aşımına uğradı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon oluşturulurken hata");

                ViewBag.ReservationError =
                    "Rezervasyon işlemi sırasında bir hata oluştu.";
            }

            return View(createReservationDto);
        }
    }
}
