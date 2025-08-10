namespace TableTenisWebApp.Models
{
    /// <summary>
    /// Typ struktury rozgrywek:
    /// • Knockout – drabinka pucharowa
    /// • League   – każdy z każdym
    /// • Groups   – faza grupowa + finały (przyszłość)
    /// </summary>
    public enum CompetitionType
    {
        Knockout = 0,
        League = 1
        
    }
}
