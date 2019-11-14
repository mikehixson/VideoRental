using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public class Order
    {
        public Address ShippingAddress { get; set; }
        public OrderItem[] Items { get; set; }        
    }   
}
