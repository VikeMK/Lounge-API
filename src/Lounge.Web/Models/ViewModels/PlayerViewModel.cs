namespace Lounge.Web.Models.ViewModels
{
    public class PlayerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int MKCId { get; set; }
        public string? DiscordId { get; set; }

        public int? Mmr { get; set; }
        public int? MaxMmr { get; set; }
    }
}
