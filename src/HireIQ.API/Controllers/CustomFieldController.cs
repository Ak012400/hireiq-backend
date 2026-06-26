using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/resume-fields")]
[Authorize]
public class CustomFieldController : BaseController
{
    private readonly AppDbContext _db;

    public CustomFieldController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var fields = await _db.CustomResumeFields
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Order)
            .Select(f => new CustomFieldResponseDTO
            {
                Id = f.Id,
                FieldName = f.FieldName,
                FieldValue = f.FieldValue,
                FieldType = f.FieldType,
                Order = f.Order
            })
            .ToListAsync();
        return Ok(fields);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomFieldDTO dto)
    {
        var userId = GetCurrentUserId();
        var field = new CustomResumeField
        {
            UserId = userId,
            FieldName = dto.FieldName,
            FieldValue = dto.FieldValue,
            FieldType = dto.FieldType,
            Order = dto.Order
        };
        _db.CustomResumeFields.Add(field);
        await _db.SaveChangesAsync();
        return Ok(new CustomFieldResponseDTO
        {
            Id = field.Id,
            FieldName = field.FieldName,
            FieldValue = field.FieldValue,
            FieldType = field.FieldType,
            Order = field.Order
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomFieldDTO dto)
    {
        var userId = GetCurrentUserId();
        var field = await _db.CustomResumeFields
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (field == null)
            return NotFound(new { error = "Field not found!" });
        field.FieldValue = dto.FieldValue;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var field = await _db.CustomResumeFields
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (field == null)
            return NotFound(new { error = "Field not found!" });
        _db.CustomResumeFields.Remove(field);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted!" });
    }
}