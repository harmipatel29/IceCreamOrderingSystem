using IceCreamOrderingSystem.Models;
using IceCreamOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamOrderingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // LOGIN - GET
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return await RedirectByRole();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // LOGIN - POST
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email address or password.");

                return View(model);
            }

            // Check the user's actual Identity roles.
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");

            // Only registered Admin or Customer accounts are allowed.
            if (!isAdmin && !isCustomer)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account does not have an assigned role. Please contact the administrator.");

                return View(model);
            }

            // Sign out any previously authenticated account first.
            await _signInManager.SignOutAsync();

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email address or password.");

                return View(model);
            }

            // Redirect only to a local return URL if supplied.
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Admin goes to Admin Dashboard.
            if (isAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            // Customer goes to the Shop.
            return RedirectToAction("Shop", "Home");
        }

        // REGISTER - GET
       [AllowAnonymous]
[HttpGet]
public async Task<IActionResult> Register()
{
    if (User.Identity?.IsAuthenticated == true)
    {
        return await RedirectByRole();
    }

    return View(new RegisterViewModel());
}

        // REGISTER - POST
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber,
                Role = "Customer",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Shop", "Home");
        }

        // LOGOUT - POST
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            // Prevent the previous user's account from being treated
            // as the currently authenticated account.
            return RedirectToAction("Index", "Home");
        }

        // ACCESS DENIED
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // FORGOT PASSWORD - GET
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // FORGOT PASSWORD - POST
        // Demo flow: the reset token is kept in TempData.
        // A production application should send a secure reset link by email.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Message = "Please enter your registered email address.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());

            if (user == null)
            {
                ViewBag.Message = "No account was found with this email address.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            TempData["ResetEmail"] = user.Email;
            TempData["ResetToken"] = token;

            return RedirectToAction(nameof(ResetPassword));
        }

        // RESET PASSWORD - GET
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"]?.ToString();
            var token = TempData["ResetToken"]?.ToString();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewBag.Email = email;
            ViewBag.Token = token;

            TempData.Keep("ResetEmail");
            TempData.Keep("ResetToken");

            return View();
        }

        // RESET PASSWORD - POST
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string email,
            string token,
            string newPassword,
            string confirmPassword)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Message = "Please fill in all fields.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Message = "Passwords do not match.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());

            if (user == null)
            {
                ViewBag.Message = "Account not found.";
                return View();
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Password reset successfully. Please log in.";

                return RedirectToAction(nameof(Login));
            }

            ViewBag.Message = string.Join(
                " ",
                result.Errors.Select(error => error.Description));

            return View();
        }

        // ROLE-BASED REDIRECT
        private async Task<IActionResult> RedirectByRole()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "Customer"))
            {
                return RedirectToAction("Shop", "Home");
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}