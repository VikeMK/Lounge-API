namespace Lounge.Web.Models.ViewModels
{
    public class PlayerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int MKCId { get; set; }
        public int? RegistryId { get; set; }
        public string? DiscordId { get; set; }
        public string? CountryCode { get; set; }
        public string? SwitchFc { get; set; }
        public bool IsHidden { get; set; }

        public int? Mmr { get; set; }
        public int? MaxMmr { get; set; }
    }
}
