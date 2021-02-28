using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BlazorDualMode.Server.Controllers
{
    [Route("_blazorMode")]
    public class BlazorModeController:ControllerBase
    {
        public static string CookieName { get; set; } = "_ssb_";

        [HttpGet("{IsServerSideBlazor}")]
        public IActionResult Switch(bool isServerSideBlazor, string redirectTo = "")
        {
            if(isServerSideBlazor != IsServerSideBlazor(HttpContext))
            {
                var response = HttpContext.Response;
                response.Cookies.Append(CookieName, Convert.ToInt32(isServerSideBlazor).ToString());
            }
            if (string.IsNullOrEmpty(redirectTo))
            {
                redirectTo = "~/";
            }
            return Redirect(redirectTo);
        }

        public static bool IsServerSideBlazor(HttpContext httpContext)
        {
            var cookies = httpContext.Request.Cookies;
            var isSsb = cookies.TryGetValue(CookieName, out var v) ? v : "";
            if(!int.TryParse(isSsb, out var isSsbInt))
            {
                return true;
            }
            return isSsbInt != 0;
        }
    }
}
