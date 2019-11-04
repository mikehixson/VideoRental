using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VideoRental.Core
{
    public interface IOrderRepository
    {
        Task<long> InsertAsync(Order order);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly BlurayRentalHttpClient _httpClient;

        public OrderRepository(BlurayRentalHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<long> InsertAsync(Order order)
        {
            await _httpClient.ClearCart();

            string cartId = null;

            foreach (var item in order.Items)
                cartId = await _httpClient.AddToCart(item.Id);

            var cart = await _httpClient.GetCart();

            foreach (var product in cart.Products)
            {
                if (product.ProductPrice != "$0.00") //todo: lets use a decimal
                    throw new Exception($"Cart contains items not included in subscription.");
            }

            //TODO: this is failing on first try after login.

            return await _httpClient.PlaceOrder(cartId, order);
        }
    }
}
