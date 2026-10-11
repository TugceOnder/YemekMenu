
using ApiProjeKampi.WebUI.Dtos.YummyEventDtos;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

public class YummyEventController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public YummyEventController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress =
            new Uri("https://localhost:7020/");
        return client;
    }

    private async Task<string?> UploadImage(
        HttpClient client, IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        using var content = new MultipartFormDataContent();
        using var stream = imageFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                imageFile.ContentType);

        content.Add(
            fileContent, "imageFile", imageFile.FileName);

        var response = await client.PostAsync(
            "api/YummyEvents/upload-image", content);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                await response.Content.ReadAsStringAsync());

        var result = await response.Content
            .ReadFromJsonAsync<ImageUploadResponse>();

        return result?.ImageUrl;
    }

    private class ImageUploadResponse
    {
        public string ImageUrl { get; set; } = "";
    }

    [HttpGet]
    public IActionResult CreateYummyEvent()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateYummyEvent(
        CreateYummyEventDto model,
        IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError(
                "imageFile", "Lütfen fotoğraf seçiniz.");

            return View(model);
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var client = CreateApiClient();

            model.ImageUrl =
                await UploadImage(client, imageFile) ?? "";

            var response = await client.PostAsJsonAsync(
                "api/YummyEvents", model);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("YummyEventList");

            ModelState.AddModelError(
                "", "Organizasyon kaydedilemedi.");
        }
        catch (Exception)
        {
            ModelState.AddModelError(
                "", "Fotoğraf yükleme veya kayıt sırasında hata oluştu.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UpdateYummyEvent(int id)
    {
        var client = CreateApiClient();

        var model = await client.GetFromJsonAsync<GetYummyEventByIdDto>(
            $"api/YummyEvents/GetYummyEvent?id={id}");

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateYummyEvent(
        GetYummyEventByIdDto model,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var client = CreateApiClient();

            // Eski resmi sunucudan oku.
            var existing = await client.GetFromJsonAsync<GetYummyEventByIdDto>(
                $"api/YummyEvents/GetYummyEvent?id={model.YummyEventId}");

            if (existing == null)
                return NotFound();

            // Yeni resim varsa yükle, yoksa eskisini koru.
            model.ImageUrl = imageFile != null
                ? await UploadImage(client, imageFile)
                    ?? existing.ImageUrl
                : existing.ImageUrl;

            var response = await client.PutAsJsonAsync(
                "api/YummyEvents", model);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("YummyEventList");

            ModelState.AddModelError(
                "", "Organizasyon güncellenemedi.");
        }
        catch (Exception)
        {
            ModelState.AddModelError(
                "", "Güncelleme sırasında hata oluştu.");
        }

        return View(model);
    }
}
