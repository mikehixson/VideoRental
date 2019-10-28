using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using VideoRental.Core;

namespace VideoRental.Web
{
    public class CookieTokenProviderHttpContext : ICookieTokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieTokenProviderHttpContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext.User.Claims.Single(c => c.Type == "CookieToken").Value;
        }
    }
}
