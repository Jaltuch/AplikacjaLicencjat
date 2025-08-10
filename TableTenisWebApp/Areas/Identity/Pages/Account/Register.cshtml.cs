// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly AppIdentityDbContext _ctx;      // JEDYNY DbContext

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        AppIdentityDbContext ctx)          // ? wstrzykujemy tylko ten kontekst
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _ctx = ctx;
    }

    // ????????????????????????? View-model ?????????????????????????
    [BindProperty] public InputModel Input { get; set; }
    public string ReturnUrl { get; set; }
    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; }

        [Required, Display(Name = "Imiê")]
        public string FirstName { get; set; }

        [Required, Display(Name = "Nazwisko")]
        public string LastName { get; set; }

        [Required, Display(Name = "Rola")]
        public string Role { get; set; }  // "Player" lub "Organizer"


        [Required, StringLength(100, MinimumLength = 6),
         DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password), Display(Name = "Confirm password"),
         Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    // ????????????????????????? GET ?????????????????????????
    public async Task OnGetAsync(string returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    // ????????????????????????? POST ?????????????????????????
    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid) return Page();

        // 1) tworzymy u¿ytkownika
        var user = CreateUser();
        await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;


        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }
        //Role
        await _userManager.AddToRoleAsync(user, Input.Role);

        if (Input.Role == "Player")
        {
            _ctx.Players.Add(new Player
            {
                Name = $"{Input.FirstName} {Input.LastName}",
                ApplicationUserId = user.Id
            });
        }

        // 4) JEDEN commit – zapisuje AspNetUsers, AspNetUserRoles i Players
        await _ctx.SaveChangesAsync();

        // 5) potwierdzenie e-mail / auto-logowanie
        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            null,
            new { area = "Identity", userId, code, returnUrl },
            Request.Scheme);

        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        if (_userManager.Options.SignIn.RequireConfirmedAccount)
            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    // ????????????????????????? helpers ?????????????????????????
    private ApplicationUser CreateUser()
        => Activator.CreateInstance<ApplicationUser>()
           ?? throw new InvalidOperationException("Cannot create ApplicationUser.");

    private IUserEmailStore<ApplicationUser> GetEmailStore()
        => !_userManager.SupportsUserEmail
           ? throw new NotSupportedException("Email store required.")
           : (IUserEmailStore<ApplicationUser>)_userStore;
}
