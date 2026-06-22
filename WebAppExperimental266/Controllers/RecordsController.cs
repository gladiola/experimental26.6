using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Controllers
{
    [Authorize(Policy = "AuthenticatedUser")]
    public class RecordsController : Controller
    {
        private readonly CrudDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<RecordsController> _logger;

        public RecordsController(
            CrudDbContext dbContext,
            IAuthorizationService authorizationService,
            ILogger<RecordsController> logger)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Index");
            var ownerId = UserIdentityHelper.GetStableUserId(User);
            var records = await _dbContext.CrudRecords
                .Where(record => record.OwnerId == ownerId)
                .OrderByDescending(record => record.UpdatedUtc)
                .ToListAsync();

            return View(records);
        }

        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> Details(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Details");
            var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Owner, "Details");
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpGet("/upload")]
        public IActionResult Create()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Create");
            ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;
            return View();
        }

        [HttpPost("/upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,CardType,IsPublic,UploadPermissionConfirmed")] CrudRecord input, IFormFile? uploadFile)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.CreatePost");
            ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;

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

            _logger.LogInformation("Created CRUD record {RecordId} for user {UserId}", record.Id, LoggingHelper.HashPii(record.OwnerId));

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> Download(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Download");
            CrudRecord? record;
            if (User.Identity?.IsAuthenticated == true)
            {
                record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Read, "Download");
            }
            else
            {
                record = await _dbContext.CrudRecords.FirstOrDefaultAsync(x => x.Id == id && x.IsPublic);
            }

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

        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> Edit(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Edit");
            var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Edit, "Edit");
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Title,Description")] CrudRecord input)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.EditPost");
            if (id != input.Id)
            {
                return NotFound();
            }

            var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Edit, "EditPost");
            if (record == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                input.OwnerId = record.OwnerId;
                input.OwnerDisplayName = record.OwnerDisplayName;
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

        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> Delete(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Delete");
            var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Delete, "Delete");
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("RecordIdOperations")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.DeleteConfirmed");
            var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Delete, "DeleteConfirmed");
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

        private async Task<CrudRecord?> FindAuthorizedRecordAsync(
            string id,
            OperationAuthorizationRequirement requirement,
            string actionName)
        {
            var record = await _dbContext.CrudRecords.FirstOrDefaultAsync(entry => entry.Id == id);
            if (record == null)
            {
                return null;
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, record, requirement);
            if (authorizationResult.Succeeded)
            {
                return record;
            }

            LogOwnershipAuthorizationFailure(actionName, id, record);
            return null;
        }

        private void LogOwnershipAuthorizationFailure(string actionName, string recordId, CrudRecord record)
        {
            string userIdForLog;
            try
            {
                userIdForLog = UserIdentityHelper.GetStableUserId(User);
            }
            catch (InvalidOperationException)
            {
                userIdForLog = "unknown-authenticated-user";
            }

            var hashedUserId = LoggingHelper.HashPii(userIdForLog);
            _logger.LogWarning(
                "Ownership authorization failed for action {Action} on record {RecordId}. UserIdHash={UserIdHash} OwnerIdHash={OwnerIdHash}",
                SanitizeForLog(actionName),
                SanitizeForLog(recordId),
                hashedUserId,
                LoggingHelper.HashPii(record.OwnerId));
        }

        private static string SanitizeForLog(string value)
        {
            return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }
    }
}
