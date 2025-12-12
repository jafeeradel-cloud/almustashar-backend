/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateApi.Data;
using RealEstateApi.Dtos;
using RealEstateApi.Models;
using RealEstateApi.Services;

namespace RealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PropertiesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public PropertiesController(AppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private async Task<int> GetNextPropertyNumberAsync()
        {
            var max = await _db.Properties.MaxAsync(p => (int?)p.PropertyNumber) ?? 0;
            return max + 1;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyListItemDto>>> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? location,
            [FromQuery] double? minArea,
            [FromQuery] double? maxArea,
            [FromQuery] double? minPrice,
            [FromQuery] double? maxPrice)
        {
            var userId = _currentUser.UserId;

            var query = _db.Properties
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(p => p.Type == type);

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(p => p.Location.Contains(location));

            if (minArea.HasValue)
                query = query.Where(p => p.Area >= minArea.Value);

            if (maxArea.HasValue)
                query = query.Where(p => p.Area <= maxArea.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            var result = await query
                .OrderBy(p => p.PropertyNumber)
                .Select(p => new PropertyListItemDto
                {
                    Id = p.Id,
                    PropertyNumber = p.PropertyNumber,
                    Type = p.Type,
                    Location = p.Location,
                    Area = p.Area,
                    Price = p.Price,
                    CanEdit = p.CreatedByUserId == userId || _currentUser.IsAdmin
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PropertyDetailsDto>> GetById(int id)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;

            var prop = await _db.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (prop == null) return NotFound(new { message = "العقار غير موجود" });

            var dto = new PropertyDetailsDto
            {
                Id = prop.Id,
                PropertyNumber = prop.PropertyNumber,
                Type = prop.Type,
                Location = prop.Location,
                Area = prop.Area,
                Price = prop.Price,
                Description = prop.Description,
                CanEdit = prop.CreatedByUserId == userId || isAdmin,
                Images = prop.Images.Select(i => new PropertyImageDto
                {
                    Id = i.Id,
                    Url = i.ImageUrl
                }).ToList()
            };

            if (isAdmin)
            {
                dto.OwnerName = prop.OwnerName;
                dto.OwnerPhone = prop.OwnerPhone;
            }

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult> Create(PropertyCreateDto dto)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;
            var now = DateTime.UtcNow;

            var prop = new Property
            {
                PropertyNumber = await GetNextPropertyNumberAsync(),
                Type = dto.Type,
                Location = dto.Location,
                Area = dto.Area,
                Price = dto.Price,
                Description = dto.Description,
                CreatedByUserId = userId,
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false,
                OwnerName = isAdmin ? (dto.OwnerName ?? "غير محدد") : "غير محدد",
                OwnerPhone = isAdmin ? (dto.OwnerPhone ?? "") : ""
            };

            _db.Properties.Add(prop);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = prop.Id }, new { prop.Id, prop.PropertyNumber });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, PropertyUpdateDto dto)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;

            var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (prop == null) return NotFound(new { message = "العقار غير موجود" });

            if (prop.CreatedByUserId != userId && !isAdmin)
                return Forbid("لا تملك صلاحية تعديل هذا العقار");

            prop.Type = dto.Type;
            prop.Location = dto.Location;
            prop.Area = dto.Area;
            prop.Price = dto.Price;
            prop.Description = dto.Description;
            prop.LastModified = DateTime.UtcNow;

            if (isAdmin)
            {
                if (!string.IsNullOrWhiteSpace(dto.OwnerName))
                    prop.OwnerName = dto.OwnerName;
                if (!string.IsNullOrWhiteSpace(dto.OwnerPhone))
                    prop.OwnerPhone = dto.OwnerPhone;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;

            var prop = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (prop == null) return NotFound(new { message = "العقار غير موجود" });

            if (prop.CreatedByUserId != userId && !isAdmin)
                return Forbid("لا تملك صلاحية حذف هذا العقار");

            prop.IsDeleted = true;
            prop.LastModified = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest(new { message = "لا توجد ملفات مرفوعة" });

            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;

            var property = await _db.Properties
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            if (property.CreatedByUserId != userId && !isAdmin)
                return Forbid("لا تملك صلاحية تعديل هذا العقار");

            var uploadsRootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties");
            Directory.CreateDirectory(uploadsRootFolder);

            var savedImages = new List<string>();

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRootFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/images/properties/{fileName}";

                var img = new PropertyImage
                {
                    PropertyId = id,
                    ImageUrl = url
                };

                _db.PropertyImages.Add(img);
                savedImages.Add(url);
            }

            await _db.SaveChangesAsync();

            return Ok(new { images = savedImages });
        }

        [HttpDelete("images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;

            var img = await _db.PropertyImages
                .Include(i => i.Property)
                .FirstOrDefaultAsync(i => i.Id == imageId);

            if (img == null)
                return NotFound(new { message = "الصورة غير موجودة" });

            if (img.Property.CreatedByUserId != userId && !isAdmin)
                return Forbid("لا تملك صلاحية حذف هذه الصورة");

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _db.PropertyImages.Remove(img);
            await _db.SaveChangesAsync();

            return NoContent();
            */
        }
    }
}
