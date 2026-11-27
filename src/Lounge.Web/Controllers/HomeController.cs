using Lounge.Web.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Lounge.Web.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly TableImageManager _tableImageManager;

        public HomeController(TableImageManager tableImageManager)
        {
            _tableImageManager = tableImageManager;
        }

        // Images are served from the image bucket (Cloudflare R2 in production), never downloaded by the app:
        // this redirects to the bucket URL, or returns the PNG directly only when it had to be generated now.
        // The short cache lets Cloudflare answer repeat redirects for a few minutes. The image URL itself never
        // changes; when an image is re-uploaded, the app purges it from Cloudflare's cache.
        [Route("TableImage/{id}.png")]
        public async Task<IActionResult> TableImage(int id)
        {
            if (!await _tableImageManager.TableExistsAsync(id))
                return NotFound();

            var result = await _tableImageManager.GetImageForRequestAsync(id);

            Response.Headers.CacheControl = "public, max-age=300";

            if (result.RedirectUrl is not null)
                return Redirect(result.RedirectUrl); // 302

            return File(result.Image!, "image/png");
        }

        [HttpPost]
        [Route("/SetLanguage")]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}
