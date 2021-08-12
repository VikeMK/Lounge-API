using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public interface IMkcRegistryApi
    {
        Task<IReadOnlyList<SimplePlayerRegistryData>> GetAllPlayersRegistryDataAsync();
        Task<int?> GetRegistryIdAsync(int mkcId);
    }
}