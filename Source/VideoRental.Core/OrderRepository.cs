using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VideoRental.Core
{
    public interface IOrderRepository
    {
        Task InsertAsync(Order order);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly BlurayRentalHttpClient _httpClient;

        public OrderRepository(BlurayRentalHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InsertAsync(Order order)
        {
            //await _httpClient.GetShippingAddressesAsync();

            await _httpClient.ClearCart();

            string cartId = null;

            foreach(var item in order.Items)
                cartId = await _httpClient.AddToCart(item.Id);

            //var y = await _httpClient.GetOrderTotals(cartId);


            // ajaxcart.asp has product and subtotal values > 0 when subscription limit exceeeded? how about when its not exceeded? this si the simplest way to see if we will have to pay.


            // We could also try https://www.store-3d-blurayrental.com/one-page-checkout.asp?ShippingSpeedChoice=ShippingSpeedChoice
            // which is called by the checkout page.. posts all 

            // Posting order shoul dnot require a payment method selection if its zero dollar?

            await _httpClient.PlaceOrder(cartId, order);

            // asking for a credit card number from this one could also iducate non zero-dollar

        }
    }
}
