using System.ComponentModel.DataAnnotations.Schema;

namespace WorkNestHRMS.Models;

public class Workplace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Nazwa miejsca pracy
    public string Description { get; set; } = string.Empty; // Opcjonalny opis
    public int OwnerId { get; set; } // ID właściciela (usera)

    // Relacja: Właściciel miejsca pracy
    //[NotMapped]   JUZ NEIUZYWANE PROBLEM ROZWIAZANY
    public User Owner { get; set; }

    // Relacja: Lista pracowników (użytkowników należących do miejsca pracy)
    public List<UserWorkplace> UserWorkplaces { get; set; } = new List<UserWorkplace>();
}
