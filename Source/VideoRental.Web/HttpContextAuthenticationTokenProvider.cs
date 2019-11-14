using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using VideoRental.Core;

namespace VideoRental.Web
{
    public class HttpContextAuthenticationTokenProvider : IAuthenticationTokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextAuthenticationTokenProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext.User.Claims.SingleOrDefault(c => c.Type == "CookieToken")?.Value;
        }
    }
}
