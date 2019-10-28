using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public interface IVideoRepository
    {
        IEnumerable<Video> GetAll(int category, int pageIndex, int sortBy, int maxResults);
    }
}
