using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly CrudDbContext _dbContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            CrudDbContext dbContext,
            ILogger<HomeController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "HomeController.Index");
            var publicRecords = await _dbContext.CrudRecords
                .Where(record => record.IsPublic)
                .OrderByDescending(record => record.UpdatedUtc)
                .Take(50)
                .ToListAsync();
            return View(publicRecords);
        }

        [AllowAnonymous]
        [Route("Privacy")]
        [Route("Home/Privacy")]
        public IActionResult Privacy()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "HomeController.Privacy");
            return View();
        }

        [AllowAnonymous]
        [Route("AboutUs")]
        [Route("Home/AboutUs")]
        public IActionResult AboutUs()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "HomeController.AboutUs");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                }
            );

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            LoggingHelper.TrackFunctionCall(HttpContext, "HomeController.Error");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
