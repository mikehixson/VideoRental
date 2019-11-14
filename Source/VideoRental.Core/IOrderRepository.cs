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
}
