using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Controllers
{
    [Authorize(Policy = "AdminCertificate")]
    public class AdminRecordsController : Controller
    {
        private readonly CrudDbContext _dbContext;
        private readonly IAdminCertificateAuditService _auditService;

        public AdminRecordsController(
            CrudDbContext dbContext,
            IAdminCertificateAuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.Index");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.Index");
            var records = await _dbContext.CrudRecords
                .OrderBy(record => record.OwnerDisplayName)
                .ThenBy(record => record.Title)
                .ToListAsync();

            return View(records);
        }

        public async Task<IActionResult> Details(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.Details");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.Details");
            var record = await FindRecordAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        public async Task<IActionResult> Edit(string id)
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.Edit");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.Edit");
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
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.EditPost");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.EditPost");

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
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.Delete");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.Delete");
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
            LoggingHelper.TrackFunctionCall(HttpContext, "AdminRecordsController.DeleteConfirmed");
            _auditService.LogPageAccess(HttpContext, User, "AdminRecords.DeleteConfirmed");
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
            return await _dbContext.CrudRecords.FirstOrDefaultAsync(record => record.Id == id);
        }
    }
}
