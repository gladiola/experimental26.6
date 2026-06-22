using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Controllers
{
    [Authorize]
    public class RecordsController : Controller
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private readonly CrudDbContext _dbContext;
        private readonly ILogger<RecordsController> _logger;

        public RecordsController(
            CrudDbContext dbContext,
            ILogger<RecordsController> logger)
        {
            _dbContext = dbContext;
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

        public async Task<IActionResult> Details(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Details");
            var record = await FindOwnedRecordAsync(id);
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
            return View();
        }

        [HttpPost("/upload")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description")] CrudRecord input, IFormFile? uploadFile)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.CreatePost");
            if (uploadFile is null && string.IsNullOrWhiteSpace(input.Title) && string.IsNullOrWhiteSpace(input.Description))
            {
                ModelState.AddModelError(string.Empty, "Provide text or select a file to upload.");
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
                else if (uploadFile.Length > MaxUploadBytes)
                {
                    ModelState.AddModelError(string.Empty, $"Files larger than {MaxUploadBytes / (1024 * 1024)} MB are not allowed.");
                }
                else
                {
                    await using var memoryStream = new MemoryStream();
                    await uploadFile.CopyToAsync(memoryStream);
                    uploadedBytes = memoryStream.ToArray();
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

        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Download");
            var record = await FindOwnedRecordAsync(id);
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

        public async Task<IActionResult> Edit(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Edit");
            var record = await FindOwnedRecordAsync(id);
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
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.EditPost");
            if (id != input.Id)
            {
                return NotFound();
            }

            var record = await FindOwnedRecordAsync(id);
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

        public async Task<IActionResult> Delete(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.Delete");
            var record = await FindOwnedRecordAsync(id);
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
            LoggingHelper.TrackFunctionCall(HttpContext, "RecordsController.DeleteConfirmed");
            var record = await FindOwnedRecordAsync(id);
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

        private async Task<CrudRecord?> FindOwnedRecordAsync(string id)
        {
            var ownerId = UserIdentityHelper.GetStableUserId(User);
            return await _dbContext.CrudRecords
                .FirstOrDefaultAsync(record => record.Id == id && record.OwnerId == ownerId);
        }
    }
}
