using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Repositories;
using TaskManagement.Web.ViewModels;

namespace TaskManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Tasks");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _userRepository.GetByEmail(model.Email);

            // Kiểm tra BCrypt hash — nếu mật khẩu chưa được hash thì so sánh plaintext
            bool passwordValid = false;
            if (user != null)
            {
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                }
                catch
                {
                    // Mật khẩu chưa hash (plaintext cũ) — so sánh trực tiếp và tự động hash lại
                    if (user.Password == model.Password)
                    {
                        passwordValid = true;
                        user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        _userRepository.Update(user);
                        _userRepository.Save();
                    }
                }
            }

            if (user == null || !passwordValid)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Tasks");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied() => View();

        // ── ĐĂNG KÝ ───────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Tasks");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View();
            }

            // Kiểm tra email đã tồn tại chưa
            if (_userRepository.GetByEmail(email) != null)
            {
                TempData["ErrorMessage"] = "Email này đã được sử dụng!";
                return View();
            }

            var user = new TaskManagement.Web.Models.User
            {
                FullName = fullName,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "User",
                CreatedAt = DateTime.Now
            };

            _userRepository.Add(user);
            _userRepository.Save();

            // Tự động đăng nhập sau khi đăng ký
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = $"Chào mừng {fullName}! Tài khoản đã được tạo thành công.";
            return RedirectToAction("Index", "Tasks");
        }

        // ── ĐỔI MẬT KHẨU ──────────────────────────────────────────────────
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận không khớp!";
                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = _userRepository.GetById(userId);
            if (user == null) return NotFound();

            // Kiểm tra mật khẩu hiện tại (hỗ trợ cả plaintext cũ và BCrypt)
            bool currentValid = false;
            try
            {
                currentValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.Password);
            }
            catch
            {
                currentValid = user.Password == currentPassword;
            }

            if (!currentValid)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                return View();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            _userRepository.Save();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return View();
        }
    }
}