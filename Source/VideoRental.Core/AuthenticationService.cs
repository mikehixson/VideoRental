using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Headers;

namespace VideoRental.Core
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly BlurayRentalHttpClient _http;

        public AuthenticationService(BlurayRentalHttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<string> GetToken(string emailAddress, string password)
        {
            return await _http.LoginAsync(emailAddress, password);
        }         
    }
}
