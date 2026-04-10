using EnviroWatch.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnviroWatch.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // LOGIN PAGE (GET)
        public IActionResult Login(string returnUrl = "/")
        {
            // Already logged in? Head straight to the app
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // REGISTER PAGE (GET)
        public IActionResult Register()
        {
            // Already logged in? Head straight to the app
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // REGISTER PAGE (POST)
        [HttpPost]
        public async Task<IActionResult> Register(
            string fullName, string email, string password, string confirmPassword,
            string? phone, int? residenceCityId)
        {
            if (password != confirmPassword)
            {
                ViewData["Error"] = "Passwords do not match.";
                return View();
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName ?? "",
                PhoneNumber = phone,
                ResidenceCityId = residenceCityId,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Add FullName claim so the nav can display it
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", user.FullName));

                await _signInManager.SignInAsync(user, isPersistent: true);
                TempData["Success"] = "Account created successfully! Welcome to EnviroWatch.";
                return RedirectToAction("Index", "Home");
            }

            // Aggregate identity errors and show them
            ViewData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return View();
        }

        // EMAIL LOGIN
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = "/")
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ViewData["Error"] = "Invalid login";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, true, false);

            if (result.Succeeded)
            {
                // Use RedirectToAction so a clean new GET request starts,
                // ensuring the auth cookie is fully applied before the layout
                // reads User.Identity — eliminates the hot-reload requirement.
                var safeUrl = (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//"))
                    ? returnUrl : "/";
                return LocalRedirect(safeUrl);
            }

            ViewData["Error"] = "Invalid email or password.";
            return View();
        }


        // ===== GITHUB LOGIN =====

        public IActionResult GitHubLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GitHubCallback", "Account", new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                "GitHub",
                redirectUrl);

            return Challenge(properties, "GitHub");
        }


        public async Task<IActionResult> GitHubCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
                return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                true);

            if (result.Succeeded)
            {
                // Add custom claims for the existing user
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null)
                {
                    await AddGitHubClaims(existingUser, info);
                    await _signInManager.SignInAsync(existingUser, true);
                }
                return Redirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                // GitHub sometimes does not return email
            if (string.IsNullOrEmpty(email))
            {
                email = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) + "@github.com";
}

var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name ?? "",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userManager.CreateAsync(user);
            }

            await _userManager.AddLoginAsync(user, info);
            await AddGitHubClaims(user, info);
            await _signInManager.SignInAsync(user, true);

            return Redirect(returnUrl);
        }

        private async Task AddGitHubClaims(AppUser user, ExternalLoginInfo info)
        {
            // Remove old custom claims if they exist
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var oldFullName = existingClaims.FirstOrDefault(c => c.Type == "FullName");
            var oldAvatar = existingClaims.FirstOrDefault(c => c.Type == "AvatarUrl");
            if (oldFullName != null) await _userManager.RemoveClaimAsync(user, oldFullName);
            if (oldAvatar != null) await _userManager.RemoveClaimAsync(user, oldAvatar);

            // Add FullName claim
            var displayName = user.FullName;
            if (string.IsNullOrEmpty(displayName))
                displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? user.Email ?? "";
            await _userManager.AddClaimAsync(user, new Claim("FullName", displayName));

            // Add AvatarUrl claim — GitHub provides avatar via urn:github:avatar or construct it
            var avatarUrl = info.Principal.FindFirstValue("urn:github:avatar")
                         ?? info.Principal.FindFirstValue("urn:github:url");
            if (string.IsNullOrEmpty(avatarUrl))
            {
                var githubLogin = info.Principal.FindFirstValue("urn:github:login")
                              ?? info.Principal.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(githubLogin))
                    avatarUrl = $"https://github.com/{githubLogin}.png?size=80";
            }
            if (!string.IsNullOrEmpty(avatarUrl))
                await _userManager.AddClaimAsync(user, new Claim("AvatarUrl", avatarUrl));
        }


        // ===== GOOGLE LOGIN =====

        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleCallback", "Account", new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                "Google",
                redirectUrl);

            return Challenge(properties, "Google");
        }

        public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
                return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                true);

            if (result.Succeeded)
            {
                // Add custom claims for the existing user
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null)
                {
                    await AddGoogleClaims(existingUser, info);
                    await _signInManager.SignInAsync(existingUser, true);
                }
                return Redirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ViewData["Error"] = "Google did not return an email address.";
                return View("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name ?? "",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userManager.CreateAsync(user);
            }

            await _userManager.AddLoginAsync(user, info);
            await AddGoogleClaims(user, info);
            await _signInManager.SignInAsync(user, true);

            return Redirect(returnUrl);
        }

        private async Task AddGoogleClaims(AppUser user, ExternalLoginInfo info)
        {
            // Remove old custom claims if they exist
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var oldFullName = existingClaims.FirstOrDefault(c => c.Type == "FullName");
            var oldAvatar = existingClaims.FirstOrDefault(c => c.Type == "AvatarUrl");
            if (oldFullName != null) await _userManager.RemoveClaimAsync(user, oldFullName);
            if (oldAvatar != null) await _userManager.RemoveClaimAsync(user, oldAvatar);

            // Add FullName claim
            var displayName = user.FullName;
            if (string.IsNullOrEmpty(displayName))
                displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? user.Email ?? "";
            await _userManager.AddClaimAsync(user, new Claim("FullName", displayName));

            // Add AvatarUrl claim — Google provides picture claim
            var avatarUrl = info.Principal.FindFirstValue("urn:google:picture")
                         ?? info.Principal.FindFirstValue("picture");
            if (!string.IsNullOrEmpty(avatarUrl))
                await _userManager.AddClaimAsync(user, new Claim("AvatarUrl", avatarUrl));
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
