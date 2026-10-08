
using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Dtos.CategoryDtos;
using ApiProjeKampi.WebApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApiContext _context;

        public CategoriesController(ApiContext context)
        {
            _context = context;
        }

        // KATEGORİ LİSTESİ
        [HttpGet]
        public async Task<IActionResult> CategoryList()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder == 0
                    ? int.MaxValue
                    : x.DisplayOrder)
                .ThenBy(x => x.CategoryId)
                .Select(x => new
                {
                    x.CategoryId,
                    x.CategoryName,
                    x.DisplayOrder
                })
                .ToListAsync();

            return Ok(categories);
        }

        // KATEGORİ DETAYI
        [HttpGet("GetCategory")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Where(x => x.CategoryId == id)
                .Select(x => new
                {
                    x.CategoryId,
                    x.CategoryName
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // KATEGORİ EKLEME
        [HttpPost]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CreateCategoryDto dto)
        {
            var maxOrder = await _context.Categories
                .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var category = new Category
            {
                CategoryName = dto.CategoryName,
                DisplayOrder = maxOrder + 1
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // KATEGORİ GÜNCELLEME
        [HttpPut]
        public async Task<IActionResult> UpdateCategory(
            [FromBody] UpdateCategoryDto dto)
        {
            var category = await _context.Categories
                .FindAsync(dto.CategoryId);

            if (category == null)
                return NotFound();

            category.CategoryName = dto.CategoryName;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // KATEGORİ SİLME
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // SÜRÜKLE-BIRAK SIRALAMASINI KAYDET
        [HttpPut("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(
            [FromBody] List<CategoryOrderDto> orders)
        {
            if (orders == null || orders.Count == 0)
                return BadRequest("Kategori sıralaması boş.");

            var ids = orders.Select(x => x.CategoryId).ToList();

            if (ids.Distinct().Count() != ids.Count)
                return BadRequest("Tekrarlanan kategori var.");

            var categories = await _context.Categories
                .Where(x => ids.Contains(x.CategoryId))
                .ToListAsync();

            if (categories.Count != ids.Count)
                return BadRequest("Bazı kategoriler bulunamadı.");

            foreach (var category in categories)
            {
                var order = orders.First(
                    x => x.CategoryId == category.CategoryId);

                category.DisplayOrder = order.DisplayOrder;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Sıralama kaydedildi."
            });
        }
    }
}
