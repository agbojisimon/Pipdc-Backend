using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIPDC.API.Extensions;
using PIPDC.Application.Auth;
using PIPDC.Application.Blog;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/blog/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await categoryService.GetAllAsync(ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.CreateAsync(request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await categoryService.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
