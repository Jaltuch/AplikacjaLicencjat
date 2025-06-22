namespace TableTenisWebApp.Models
{
    public class TournamentPlayer
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
    }

}
