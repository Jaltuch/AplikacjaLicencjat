using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TableTenisWebApp.Models.ViewModels
{
    public class MatchViewModel
    {
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public DateTime DatePlayed { get; set; } = DateTime.Now;
        public int NumberOfSets { get; set; } = 5; // domyślnie 5 setów

        public List<SetInput> Sets { get; set; } = new List<SetInput>();
    }

    public class SetInput
    {
        
        public int? Score1 { get; set; }
       
        public int? Score2 { get; set; }
    }
}
