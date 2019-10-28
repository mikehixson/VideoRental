using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public class Order
    {
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
        public OrderItem[] Items { get; set; }        
    }   

    public class OrderItem
    {
        public string Id { get; set; }
    }
}
