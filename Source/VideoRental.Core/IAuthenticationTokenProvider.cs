using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public interface IAuthenticationTokenProvider
    {
        string GetToken();
    }
}
