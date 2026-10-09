using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Dtos.ProductDtos;
using ApiProjeKampi.WebApi.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IValidator<Product> _validator;
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public ProductsController(IValidator<Product> validator, ApiContext context, IMapper mapper)
        {
            _validator = validator;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult ProductList()
        {
            var values = _context.Products.ToList();
            return Ok(values);
        }

        [HttpPost]
        public IActionResult CreateProduct(Product product)
        {
            var validationResult = _validator.Validate(product);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(x => x.ErrorMessage));
            }

            _context.Products.Add(product);
            _context.SaveChanges();
            return Ok("Ürün ekleme işlemi başarılı");
        }

        [HttpDelete]
        public IActionResult DeleteProduct(int id)
        {
            var value = _context.Products.Find(id);
            if (value == null) return NotFound("Silinecek ürün bulunamadı.");

            _context.Products.Remove(value);
            _context.SaveChanges();
            return Ok("Silme işlemi başarılı");
        }

        [HttpGet("GetProduct")]
        public IActionResult GetProduct(int id)
        {
            var product = _context.Products.FirstOrDefault(x => x.ProductId == id);
            if (product == null) return NotFound("Ürün bulunamadı.");

            return Ok(product);
        }

        [HttpPut]
        public IActionResult UpdateProduct([FromBody] Product product)
        {
            if (product == null) return BadRequest("Ürün bilgileri boş olamaz.");

            var existingProduct = _context.Products.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (existingProduct == null) return NotFound("Güncellenecek ürün bulunamadı.");

            var validationResult = _validator.Validate(product);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            existingProduct.ProductName = product.ProductName;
            existingProduct.ProductDescription = product.ProductDescription;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.CategoryId = product.CategoryId;

            _context.SaveChanges();
            return Ok("Ürün başarıyla güncellendi.");
        }

        [HttpPost("CreateProductWithCategory")]
        public IActionResult CreateProductWithCategory(CreateProductDto createProductDto)
        {
            var value = _mapper.Map<Product>(createProductDto);
            _context.Products.Add(value);
            _context.SaveChanges();
            return Ok("Ekleme işlemi başarılı");
        }

        [HttpGet("ProductListWithCategory")]
        public IActionResult ProductListWithCategory()
        {
            var value = _context.Products.Include(x => x.Category).ToList();
            return Ok(_mapper.Map<List<ResultProductWithCategoryDto>>(value));
        }

        [HttpPut("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(
            [FromBody] List<ProductOrderDto> orders)
        {
            if (orders == null || orders.Count == 0)
                return BadRequest("Ürün sıralaması boş.");

            var ids = orders.Select(x => x.ProductId).ToList();

            if (ids.Distinct().Count() != ids.Count)
                return BadRequest("Tekrarlanan ürün var.");

            if (orders.Any(x => x.DisplayOrder <= 0) ||
                orders.Select(x => x.DisplayOrder).Distinct().Count() != orders.Count)
            {
                return BadRequest("Geçersiz sıralama değerleri.");
            }

            var products = await _context.Products
                .Where(x => ids.Contains(x.ProductId))
                .ToListAsync();

            if (products.Count != ids.Count)
                return BadRequest("Bazı ürünler bulunamadı.");

            var orderLookup = orders.ToDictionary(
                x => x.ProductId,
                x => x.DisplayOrder);

            foreach (var product in products)
            {
                product.DisplayOrder = orderLookup[product.ProductId];
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Ürün sıralaması kaydedildi."
            });
        }

    }
}