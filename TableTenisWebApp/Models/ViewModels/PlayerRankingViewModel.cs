using TableTenisWebApp.Models;

namespace TableTenisWebApp.Models.ViewModels
{
    public class PlayerRankingViewModel
    {
        public Player Player { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public int SetsWon { get; set; }
        public int SetsLost { get; set; }
    }
}
