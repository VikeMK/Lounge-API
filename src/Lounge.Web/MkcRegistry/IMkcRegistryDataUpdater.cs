using System.Threading.Tasks;

namespace Lounge.Web.Stats
{
    public interface IMkcRegistryDataUpdater
    {
        Task UpdateRegistryDataAsync();
        Task UpdateRegistryIdsAsync();
    }
}