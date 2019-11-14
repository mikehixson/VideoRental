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
using VideoRental.Core;

namespace VideoRental.Tdbr
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly TdbrHttpClient _http;

        public AuthenticationService(TdbrHttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<string> GetToken(Credentials credentials)
        {
            return await _http.LoginAsync(credentials.Username, credentials.Password);
        }         
    }
}
