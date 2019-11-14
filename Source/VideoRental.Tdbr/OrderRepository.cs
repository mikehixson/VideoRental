using System;
using System.Threading.Tasks;
using VideoRental.Core;

namespace VideoRental.Tdbr
{
    public class OrderRepository : IOrderRepository
    {
        private readonly TdbrHttpClient _httpClient;

        public OrderRepository(TdbrHttpClient httpClient)
        {
            _httpClient = httpClient;            
        }

        public async Task<long> InsertAsync(Order order)
        {
            await _httpClient.ClearCart();

            foreach (var item in order.Items)
                await _httpClient.AddToCart(item.Id);

            await _httpClient.GetOrderPage();
            await _httpClient.GetShippingOptions(order);
            await _httpClient.GetOrderTotal(order);

            var cart = await _httpClient.GetCart();

            foreach (var product in cart.Products)
            {
                if (product.ProductPrice != "$0.00") //todo: lets use a decimal
                    throw new Exception($"Cart contains items not included in subscription.");
            }

            return await _httpClient.PostOrderPage(order);
        }
    }
}
