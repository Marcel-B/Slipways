using System;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Contracts
{
    public interface ITokenService<DTO> where DTO : class
    {
        Task<DTO> InsertAsync(DTO item);
        Task<DTO> DeleteAsync(Guid id);
        Task<DTO> UpdateAsync(Guid id, DTO item);
    }
}
