using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentsController(
            IDocumentRepository documentRepository,
            IUserRepository userRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _documentRepository = documentRepository;
            _userRepository = userRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // ── DANH SÁCH TÀI LIỆU ────────────────────────────────────────────
        public IActionResult Index(string? search, string? fileType, string? uploadedBy)
        {
            var documents = _documentRepository.GetAll().ToList();

            // Tìm kiếm theo tên tài liệu hoặc mô tả
            if (!string.IsNullOrWhiteSpace(search))
                documents = documents.Where(d =>
                    (d.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (d.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (d.FileName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

            // Lọc theo loại file
            if (!string.IsNullOrWhiteSpace(fileType))
                documents = documents.Where(d => {
                    var fn = d.FileName ?? "";
                    var dot = fn.LastIndexOf('.');
                    var ext = dot >= 0 ? fn.Substring(dot).ToLower() : "";
                    return fileType switch
                    {
                        "word" => ext == ".doc" || ext == ".docx",
                        "excel" => ext == ".xls" || ext == ".xlsx",
                        "pdf" => ext == ".pdf",
                        "zip" => ext == ".zip" || ext == ".rar",
                        _ => true
                    };
                }).ToList();

            // Map tên người upload
            var allUsers = _userRepository.GetAll().ToList();
            var uploaderNames = allUsers.ToDictionary(u => u.UserId, u => u.FullName ?? "?");

            // Lọc theo người upload
            if (!string.IsNullOrWhiteSpace(uploadedBy) && int.TryParse(uploadedBy, out int uid))
                documents = documents.Where(d => d.UploadedBy == uid).ToList();

            ViewBag.UploaderNames = uploaderNames;
            ViewBag.AllUsers = allUsers;
            ViewBag.Search = search;
            ViewBag.FileType = fileType;
            ViewBag.UploadedBy = uploadedBy;
            return View(documents);
        }

        // ── LỊCH SỬ PHIÊN BẢN ─────────────────────────────────────────────
        public IActionResult Versions(int groupId)
        {
            var versions = _documentRepository.GetVersions(groupId).ToList();
            if (!versions.Any()) return NotFound();

            var uploaderIds = versions.Select(d => d.UploadedBy).Distinct().ToList();
            var uploaderNames = uploaderIds.ToDictionary(
                uid => uid,
                uid => _userRepository.GetById(uid)?.FullName ?? "Không xác định"
            );

            ViewBag.UploaderNames = uploaderNames;
            ViewBag.GroupId = groupId;
            ViewBag.DocumentTitle = versions.First().Title;
            return View(versions);
        }

        // ── UPLOAD TÀI LIỆU MỚI ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string title, string? description)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn file.";
                    return RedirectToAction(nameof(Index));
                }

                if (file.Length > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File không được vượt quá 20MB.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập tên tài liệu.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                // Tạo thư mục lưu
                var webRoot = _webHostEnvironment.WebRootPath
                              ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadFolder = System.IO.Path.Combine(webRoot, "uploads", "documents");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var uniqueFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{System.IO.Path.GetFileName(file.FileName)}";
                var filePath = System.IO.Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Tạo GroupId mới
                var newGroupId = _documentRepository.GetMaxGroupId() + 1;

                var document = new Document
                {
                    Title = title,
                    Description = description,
                    FileName = file.FileName,
                    FilePath = $"/uploads/documents/{uniqueFileName}",
                    FileSize = file.Length,
                    UploadedBy = currentUserId,
                    UploadedAt = DateTime.Now,
                    Version = 1,
                    GroupId = newGroupId
                };

                _documentRepository.Add(document);
                _documentRepository.Save();

                TempData["SuccessMessage"] = "Upload tài liệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ── CẬP NHẬT PHIÊN BẢN MỚI ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVersion(IFormFile file, int groupId, string title, string? description)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn file.";
                    return RedirectToAction(nameof(Index));
                }

                if (file.Length > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File không được vượt quá 20MB.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                // Lấy version hiện tại cao nhất
                var versions = _documentRepository.GetVersions(groupId).ToList();
                var latestVersion = versions.Any() ? versions.Max(d => d.Version) : 0;

                var webRoot = _webHostEnvironment.WebRootPath
                              ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadFolder = System.IO.Path.Combine(webRoot, "uploads", "documents");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var uniqueFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{System.IO.Path.GetFileName(file.FileName)}";
                var filePath = System.IO.Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Title = title,
                    Description = description,
                    FileName = file.FileName,
                    FilePath = $"/uploads/documents/{uniqueFileName}",
                    FileSize = file.Length,
                    UploadedBy = currentUserId,
                    UploadedAt = DateTime.Now,
                    Version = latestVersion + 1,
                    GroupId = groupId
                };

                _documentRepository.Add(document);
                _documentRepository.Save();

                TempData["SuccessMessage"] = $"Đã cập nhật lên phiên bản v{latestVersion + 1}!";
                return RedirectToAction(nameof(Versions), new { groupId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ── XÓA TÀI LIỆU ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int documentId)
        {
            try
            {
                var doc = _documentRepository.GetById(documentId);
                if (doc == null) return NotFound();

                var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                // Chỉ Admin hoặc người upload mới được xóa
                if (!User.IsInRole("Admin") && doc.UploadedBy != currentUserId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa tài liệu này.";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa file vật lý
                var webRoot = _webHostEnvironment.WebRootPath
                              ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var physicalPath = System.IO.Path.Combine(webRoot, doc.FilePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);

                var groupId = doc.GroupId;
                _documentRepository.Delete(documentId);
                _documentRepository.Save();

                TempData["SuccessMessage"] = "Đã xóa tài liệu thành công.";

                // Nếu đang ở trang versions thì quay về Index
                if (groupId.HasValue && _documentRepository.GetVersions(groupId.Value).Any())
                    return RedirectToAction(nameof(Versions), new { groupId });

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}