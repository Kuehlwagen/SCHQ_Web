using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SCHQ_Blazor.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller {

  public IActionResult Set(string culture, string redirectUri) {
    if (culture != null) {
      var requestCulrure = new RequestCulture(culture, culture);
      var cookieName = CookieRequestCultureProvider.DefaultCookieName;
      var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulrure);

      HttpContext.Response.Cookies.Append(cookieName, cookieValue);
    }

    return LocalRedirect(redirectUri);
  }

}
