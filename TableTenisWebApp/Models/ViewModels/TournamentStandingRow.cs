namespace TableTenisWebApp.Models.ViewModels;

public record TournamentStandingRow
(
    int PlayerId,
    string Name,
    int Played,
    int Won,
    int Lost,
    int SetsPlus,
    int SetsMinus,
    int Points         // 2 pkt za wygraną
);
