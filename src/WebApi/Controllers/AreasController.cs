using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreasController : ControllerBase
{
    private readonly AdleDbContext _db;

    public AreasController(AdleDbContext db) => _db = db;

    public record AreaDto(int Id, string? Name, double Width, double Height, int? ParentAreaId, int? AreaTypeId);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AreaDto>>> GetAll(CancellationToken ct)
    {
        var areas = await _db.Areas
            .AsNoTracking()
            .Where(a => a.DeletedDate == null)
            .OrderBy(a => a.ID)
            .Select(a => new AreaDto(a.ID, a.Name, a.Width, a.Height, a.AreaID, a.AreaTypeID))
            .ToListAsync(ct);

        return Ok(areas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AreaDto>> GetById(int id, CancellationToken ct)
    {
        var area = await _db.Areas
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(a => new AreaDto(a.ID, a.Name, a.Width, a.Height, a.AreaID, a.AreaTypeID))
            .FirstOrDefaultAsync(ct);

        return area is null ? NotFound() : Ok(area);
    }
}
