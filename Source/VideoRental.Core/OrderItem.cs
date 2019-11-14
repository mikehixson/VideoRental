using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public class OrderItem
    {
        public string Id { get; }

        public OrderItem(string id)
        {
            Id = id;
        }
    }
}
