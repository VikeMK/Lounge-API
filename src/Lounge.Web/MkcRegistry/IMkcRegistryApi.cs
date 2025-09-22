using Lounge.Web.MkcRegistry.Contracts;
using System.Threading.Tasks;

namespace Lounge.Web.MkcRegistry;

public interface IMkcRegistryApi
{
    Task<MkcRegistryData> GetPlayerRegistryDataAsync(int registryId);
}