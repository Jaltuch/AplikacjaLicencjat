using System.ComponentModel.DataAnnotations;

namespace TableTenisWebApp.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public bool IsApproved { get; set; } = false;      // Organizer/Admin potwierdził
        public string? EnteredByUserId { get; set; }          // kto wpisał



        [Required]
        [Display(Name = "Zawodnik 1")]
        public int Player1Id { get; set; }

        public Player? Player1 { get; set; } = null!;

        [Required]
        [Display(Name = "Zawodnik 2")]
        public int Player2Id { get; set; }

        public Player? Player2 { get; set; } = null!;

        [Required]
        [Range(0, 100, ErrorMessage = "Wynik musi być w przedziale 0-100")]
        [Display(Name = "Wynik zawodnika 1")]
        public int Score1 { get; set; } = 0;

        [Required]
        [Range(0, 100, ErrorMessage = "Wynik musi być w przedziale 0-100")]
        [Display(Name = "Wynik zawodnika 2")]
        public int Score2 { get; set; } = 0;

        public string? SetScores { get; set; } // np. "11:8;8:11;11:9"


        [Required(ErrorMessage = "Data rozegrania meczu jest wymagana")]
        [DataType(DataType.Date)]
        [Display(Name = "Data meczu")]
        public DateTime DatePlayed { get; set; } = DateTime.Now;
    }
}
