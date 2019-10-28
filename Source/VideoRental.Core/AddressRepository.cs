using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VideoRental.Core
{
    public interface IAddressRepository
    {
        Task<IEnumerable<Address>> GetAllAsync();
    }

    public class AddressRepository : IAddressRepository
    {
        private readonly BlurayRentalHttpClient _httpClient;

        public AddressRepository(BlurayRentalHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Address>> GetAllAsync()
        {
            return await _httpClient.GetShippingAddressesAsync();
        }
    }
}
