using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VideoRental.Core
{
    public interface IAuthenticationService
    {
        Task<string> GetToken(string emailAddress, string password);
    }
}
