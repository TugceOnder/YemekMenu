
using ApiProjeKampi.WebUI.Dtos.ReservationDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class ReservationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private const string ApiUrl =
            "https://localhost:7020/api/Reservations";

        public ReservationController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> ReservationList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(ApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData =
                        await response.Content.ReadAsStringAsync();

                    var values = JsonConvert.DeserializeObject<
                        List<ResultReservationDto>>(jsonData);

                    return View(values ?? new List<ResultReservationDto>());
                }

                TempData["ReservationError"] =
                    "Rezervasyon listesi alınamadı.";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                TempData["ReservationError"] =
                    "Rezervasyon servisine bağlanılamadı.";
            }

            return View(new List<ResultReservationDto>());
        }

   

        [HttpGet]
        public IActionResult CreateReservation()
        {
            var model = new CreateReservationDto
            {
                ReservationDate = DateTime.Today,
                ReservationStatus = "Onay Bekliyor"
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(
            CreateReservationDto createReservationDto)
        {
            createReservationDto.ReservationStatus = "Onay Bekliyor";
            createReservationDto.Message ??= string.Empty;

            ModelState.Remove(
                nameof(CreateReservationDto.ReservationStatus));

            ModelState.Remove(
                nameof(CreateReservationDto.Message));

            if (!ModelState.IsValid)
            {
                return View(createReservationDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var jsonData =
                    JsonConvert.SerializeObject(createReservationDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    ApiUrl,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ReservationSuccess"] =
                        "Yeni rezervasyon başarıyla oluşturuldu.";

                    return RedirectToAction(nameof(ReservationList));
                }

                ModelState.AddModelError(
                    "",
                    "Rezervasyon kaydedilemedi. API hata kodu: " +
                    (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                ModelState.AddModelError(
                    "",
                    "Rezervasyon servisine bağlanılamadı.");
            }

            return View(createReservationDto);
        }

        // ==========================================
        // 4. REZERVASYON ONAYLA
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> AcceptReservation(int id)
        {
            return await ChangeReservationStatus(
                id,
                "Onaylandı",
                "Rezervasyon başarıyla onaylandı.");
        }



        [HttpGet]
        public async Task<IActionResult> WaitReservation(int id)
        {
            return await ChangeReservationStatus(
                id,
                "Onay Bekliyor",
                "Rezervasyon beklemeye alındı.");
        }


        [HttpGet]
        public async Task<IActionResult> CancelReservation(int id)
        {
            return await ChangeReservationStatus(
                id,
                "İptal Edildi",
                "Rezervasyon iptal edildi.");
        }

        

        private async Task<IActionResult> ChangeReservationStatus(
            int id,
            string newStatus,
            string successMessage)
        {
            if (id <= 0)
            {
                TempData["ReservationError"] =
                    "Geçersiz rezervasyon numarası.";

                return RedirectToAction(nameof(ReservationList));
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Önce mevcut rezervasyonu getir.
                var getResponse = await client.GetAsync(
                    ApiUrl + "/GetReservation?id=" + id);

                if (!getResponse.IsSuccessStatusCode)
                {
                    TempData["ReservationError"] =
                        "Rezervasyon bilgileri getirilemedi.";

                    return RedirectToAction(nameof(ReservationList));
                }

                var jsonData =
                    await getResponse.Content.ReadAsStringAsync();

                var reservation =
                    JsonConvert.DeserializeObject<UpdateReservationDto>(
                        jsonData);

                if (reservation == null)
                {
                    TempData["ReservationError"] =
                        "Rezervasyon bulunamadı.";

                    return RedirectToAction(nameof(ReservationList));
                }

                // Sadece rezervasyon durumunu değiştir.
                // Diğer müşteri bilgileri korunur.
                reservation.ReservationStatus = newStatus;

                var updateJson =
                    JsonConvert.SerializeObject(reservation);

                using var content = new StringContent(
                    updateJson,
                    Encoding.UTF8,
                    "application/json");

                var putResponse = await client.PutAsync(
                    ApiUrl,
                    content);

                if (putResponse.IsSuccessStatusCode)
                {
                    TempData["ReservationSuccess"] =
                        successMessage;
                }
                else
                {
                    var errorBody =
                        await putResponse.Content.ReadAsStringAsync();

                    Console.Error.WriteLine(
                        "Rezervasyon güncelleme hatası: " +
                        errorBody);

                    TempData["ReservationError"] =
                        "Rezervasyon durumu değiştirilemedi. " +
                        "API hata kodu: " +
                        (int)putResponse.StatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                TempData["ReservationError"] =
                    "Rezervasyon güncellenirken hata oluştu.";
            }

            return RedirectToAction(nameof(ReservationList));
        }


        [HttpGet]
        public async Task<IActionResult> UpdateReservation(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync(
                    ApiUrl + "/GetReservation?id=" + id);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ReservationError"] =
                        "Rezervasyon bilgileri alınamadı.";

                    return RedirectToAction(nameof(ReservationList));
                }

                var jsonData =
                    await response.Content.ReadAsStringAsync();

                // UpdateReservation.cshtml ile aynı model
                var value =
                    JsonConvert.DeserializeObject<UpdateReservationDto>(
                        jsonData);

                if (value == null)
                {
                    return NotFound();
                }

                return View(value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                TempData["ReservationError"] =
                    "Düzenleme sayfası açılırken hata oluştu.";

                return RedirectToAction(nameof(ReservationList));
            }
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReservation(
            UpdateReservationDto updateReservationDto)
        {
            // Dropdown'dan yalnızca geçerli durumlar seçilsin.
            var allowedStatuses = new[]
            {
                "Onay Bekliyor",
                "Onaylandı",
                "İptal Edildi"
            };

            if (!allowedStatuses.Contains(
                    updateReservationDto.ReservationStatus))
            {
                ModelState.AddModelError(
                    nameof(UpdateReservationDto.ReservationStatus),
                    "Geçersiz rezervasyon durumu.");
            }

            updateReservationDto.Message ??= string.Empty;

            ModelState.Remove(
                nameof(UpdateReservationDto.Message));

            if (!ModelState.IsValid)
            {
                return View(updateReservationDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var jsonData =
                    JsonConvert.SerializeObject(updateReservationDto);

                using var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PutAsync(
                    ApiUrl,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ReservationSuccess"] =
                        "Rezervasyon başarıyla güncellendi.";

                    return RedirectToAction(nameof(ReservationList));
                }

                ModelState.AddModelError(
                    "",
                    "Rezervasyon güncellenemedi. API hata kodu: " +
                    (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                ModelState.AddModelError(
                    "",
                    "Rezervasyon güncellenirken hata oluştu.");
            }

            return View(updateReservationDto);
        }

        // ==========================================
        // 10. REZERVASYON SİL
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            if (id <= 0)
            {
                TempData["ReservationError"] =
                    "Geçersiz rezervasyon numarası.";

                return RedirectToAction(nameof(ReservationList));
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.DeleteAsync(
                    ApiUrl + "?id=" + id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ReservationSuccess"] =
                        "Rezervasyon başarıyla silindi.";
                }
                else
                {
                    TempData["ReservationError"] =
                        "Rezervasyon silinemedi. API hata kodu: " +
                        (int)response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                TempData["ReservationError"] =
                    "Rezervasyon silinirken hata oluştu.";
            }

            return RedirectToAction(nameof(ReservationList));
        }
    }
}
