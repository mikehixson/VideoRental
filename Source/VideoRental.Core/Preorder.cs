using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public class Preorder
    {
        public DateTime ReleaseDate { get; }

        public Preorder(DateTime releaseDate)
        {
            ReleaseDate = releaseDate;
        }
    }
}
