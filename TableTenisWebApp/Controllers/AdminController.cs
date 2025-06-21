using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly RoleManager<IdentityRole> _rm;
    public AdminController(UserManager<ApplicationUser> um, RoleManager<IdentityRole> rm)
    {
        _um = um;
        _rm = rm;
    }

    // GET /Admin
    public IActionResult Index() => View(_um.Users);

    // POST /Admin/GrantOrganizer?id=GUID
    [HttpPost]
    public async Task<IActionResult> GrantOrganizer(string id)
    {
        var user = await _um.FindByIdAsync(id);
        if (user != null && !await _um.IsInRoleAsync(user, "Organizer"))
            await _um.AddToRoleAsync(user, "Organizer");
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/RevokeOrganizer?id=GUID
    [HttpPost]
    public async Task<IActionResult> RevokeOrganizer(string id)
    {
        var user = await _um.FindByIdAsync(id);
        if (user != null && await _um.IsInRoleAsync(user, "Organizer"))
            await _um.RemoveFromRoleAsync(user, "Organizer");
        return RedirectToAction(nameof(Index));
    }
}
