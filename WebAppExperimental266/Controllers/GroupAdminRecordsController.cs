using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Controllers
{
    [Authorize(Policy = "GroupAdminCertificate")]
    public class GroupAdminRecordsController : Controller
    {
        private readonly CrudDbContext _dbContext;
        private readonly GroupAccessSettings _groupAccessSettings;
        private readonly IAdminCertificateAuditService _auditService;

        public GroupAdminRecordsController(
            CrudDbContext dbContext,
            GroupAccessSettings groupAccessSettings,
            IAdminCertificateAuditService auditService)
        {
            _dbContext = dbContext;
            _groupAccessSettings = groupAccessSettings;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Index");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Index");
            var groupId = GetCurrentGroupId();
            if (groupId == null)
            {
                return NotFound();
            }

            var records = await _dbContext.CrudRecords
                .Where(record => record.GroupId == groupId)
                .OrderBy(record => record.OwnerDisplayName)
                .ThenBy(record => record.Title)
                .ToListAsync();

            return View(records);
        }

        public IActionResult Create()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Create");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Create");
            ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,CardType,IsPublic,UploadPermissionConfirmed")] CrudRecord input, IFormFile? uploadFile)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.CreatePost");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.CreatePost");
            ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;

            var groupId = GetCurrentGroupId();
            if (groupId == null)
            {
                return NotFound();
            }

            if (uploadFile is null)
            {
                ModelState.AddModelError(string.Empty, "Please choose a JSON file to upload.");
            }
            else if (!string.Equals(Path.GetExtension(uploadFile.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Only .json files are supported.");
            }

            if (!input.UploadPermissionConfirmed)
            {
                ModelState.AddModelError(nameof(input.UploadPermissionConfirmed), "You must confirm upload permission.");
            }

            if (!UploadPolicy.IsSupportedCardType(input.CardType))
            {
                ModelState.AddModelError(nameof(input.CardType), "Select a supported card type.");
            }

            byte[]? uploadedBytes = null;
            string? uploadedFileName = null;
            string? uploadedContentType = null;
            long? uploadedFileSize = null;
            if (uploadFile is not null)
            {
                if (uploadFile.Length <= 0)
                {
                    ModelState.AddModelError(string.Empty, "The selected file is empty.");
                }
                else if (uploadFile.Length > UploadPolicy.MaxUploadBytes)
                {
                    ModelState.AddModelError(string.Empty, $"Files larger than {UploadPolicy.MaxUploadBytes / (1024 * 1024)} MB are not allowed.");
                }
                else
                {
                    uploadedBytes = await UploadPolicy.ReadFileBytesAsync(uploadFile, HttpContext.RequestAborted);
                    uploadedFileSize = uploadFile.Length;
                    uploadedFileName = Path.GetFileName(uploadFile.FileName);
                    uploadedContentType = string.IsNullOrWhiteSpace(uploadFile.ContentType)
                        ? "application/octet-stream"
                        : uploadFile.ContentType;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var now = DateTime.UtcNow;
            var trimmedTitle = input.Title?.Trim();
            var fallbackTitle = string.IsNullOrWhiteSpace(uploadedFileName) ? "Untitled Upload" : uploadedFileName;
            var record = new CrudRecord
            {
                Title = string.IsNullOrWhiteSpace(trimmedTitle) ? fallbackTitle : trimmedTitle,
                Description = input.Description ?? string.Empty,
                UploadedFileName = uploadedFileName,
                UploadedContentType = uploadedContentType,
                UploadedFileSizeBytes = uploadedFileSize,
                UploadedFileContent = uploadedBytes,
                CardType = string.IsNullOrWhiteSpace(input.CardType) ? "MIFARE Classic" : input.CardType,
                IsPublic = input.IsPublic,
                UploadPermissionConfirmed = input.UploadPermissionConfirmed,
                OwnerId = UserIdentityHelper.GetStableUserId(User),
                OwnerDisplayName = UserIdentityHelper.GetDisplayName(User),
                GroupId = groupId,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            _dbContext.CrudRecords.Add(record);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The record could not be saved. Please try again.");
                return View(input);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Details");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Details");
            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        public async Task<IActionResult> Edit(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Edit");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Edit");
            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Title,Description")] CrudRecord input)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.EditPost");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.EditPost");

            if (id != input.Id)
            {
                return NotFound();
            }

            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                input.OwnerId = record.OwnerId;
                input.OwnerDisplayName = record.OwnerDisplayName;
                input.GroupId = record.GroupId;
                input.CreatedUtc = record.CreatedUtc;
                input.UpdatedUtc = record.UpdatedUtc;
                return View(input);
            }

            record.Title = input.Title;
            record.Description = input.Description;
            record.UpdatedUtc = DateTime.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "This record was changed by another process. Reload and try again.");
                return View(record);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The record could not be saved. Please try again.");
                return View(record);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Download");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Download");
            var record = await FindRecordAsync(id);
            if (record?.UploadedFileContent == null || record.UploadedFileContent.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(record.UploadedContentType)
                ? "application/octet-stream"
                : record.UploadedContentType;
            var fileName = string.IsNullOrWhiteSpace(record.UploadedFileName)
                ? "upload.bin"
                : record.UploadedFileName;

            return File(record.UploadedFileContent, contentType, fileName);
        }

        public async Task<IActionResult> Delete(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.Delete");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.Delete");
            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "GroupAdminRecordsController.DeleteConfirmed");
            _auditService.LogPageAccess(HttpContext, User, "GroupAdminRecords.DeleteConfirmed");
            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            _dbContext.CrudRecords.Remove(record);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The record could not be deleted. Please try again.");
                return View("Delete", record);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<CrudRecord?> FindRecordAsync(string id)
        {
            var groupId = GetCurrentGroupId();
            if (groupId == null)
            {
                return null;
            }

            return await _dbContext.CrudRecords.FirstOrDefaultAsync(record =>
                record.Id == id
                && record.GroupId == groupId);
        }

        private string? GetCurrentGroupId()
        {
            var certificate = HttpContext.Connection.ClientCertificate;
            if (certificate == null)
            {
                return null;
            }

            var groupAdmin = _groupAccessSettings.FindAuthorizedGroupAdmin(
                User,
                certificate.Thumbprint ?? string.Empty,
                certificate.Issuer);
            return groupAdmin?.GroupId;
        }
    }
}
