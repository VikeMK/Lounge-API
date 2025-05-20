using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public interface IMkcRegistryApi
    {
        Task<MkcRegistryData> GetPlayerRegistryDataAsync(int registryId);
    }
}