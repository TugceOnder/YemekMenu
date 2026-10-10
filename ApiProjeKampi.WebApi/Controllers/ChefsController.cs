
using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Entities;
using ApiProjeKampi.WebUI.Dtos.ChefDtos;
using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChefsController : ControllerBase
    {
        private readonly ApiContext _context;

        public ChefsController(ApiContext context)
        {
            _context = context;
        }

        // ŞEF LİSTESİ
        [HttpGet]
        public IActionResult CehfList()
        {
            var values = _context.Chefs.ToList();

            return Ok(values);
        }

        // YENİ ŞEF EKLE
        [HttpPost]
        public IActionResult CreateChef(
            [FromBody] CreateChefDto createChefDto)
        {
            if (string.IsNullOrWhiteSpace(createChefDto.NameSurname) ||
                string.IsNullOrWhiteSpace(createChefDto.Title) ||
                string.IsNullOrWhiteSpace(createChefDto.ImageUrl))
            {
                return BadRequest(
                    "Şef adı, unvanı ve fotoğrafı zorunludur.");
            }

            var chef = new Chef
            {
                NameSurname = createChefDto.NameSurname,
                Title = createChefDto.Title,
                Description = createChefDto.Description,
                ImageUrl = createChefDto.ImageUrl
            };

            _context.Chefs.Add(chef);
            _context.SaveChanges();

            return Ok("Şef sisteme başarıyla eklendi.");
        }

        // ŞEF SİL
        [HttpDelete]
        public IActionResult DeleteChef(int id)
        {
            var chef = _context.Chefs.Find(id);

            if (chef == null)
            {
                return NotFound("Şef bulunamadı.");
            }

            _context.Chefs.Remove(chef);
            _context.SaveChanges();

            return Ok("Şef sistemden silindi.");
        }

        // ŞEF DETAY
        [HttpGet("GetChef")]
        public IActionResult GetChef(int id)
        {
            var chef = _context.Chefs.Find(id);

            if (chef == null)
            {
                return NotFound("Şef bulunamadı.");
            }

            return Ok(chef);
        }

        // ŞEF GÜNCELLE
        [HttpPut]
        public IActionResult UpdateChef(
            [FromBody] UpdateChefDto updateChefDto)
        {
            var chef = _context.Chefs.Find(updateChefDto.ChefId);

            if (chef == null)
            {
                return NotFound("Şef bulunamadı.");
            }

            chef.NameSurname = updateChefDto.NameSurname;
            chef.Title = updateChefDto.Title;
            chef.Description = updateChefDto.Description;
            chef.ImageUrl = updateChefDto.ImageUrl;

            _context.SaveChanges();

            return Ok("Şef güncelleme işlemi başarılı.");
        }
    }
}
