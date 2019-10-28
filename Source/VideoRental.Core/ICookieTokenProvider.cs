using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public interface ICookieTokenProvider
    {
        string GetToken();
    }
}
