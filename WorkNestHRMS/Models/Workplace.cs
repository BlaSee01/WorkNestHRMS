using System.ComponentModel.DataAnnotations.Schema;

namespace WorkNestHRMS.Models;

public class Workplace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public string Description { get; set; } = string.Empty; 
    public int OwnerId { get; set; } // do usera wiązać, jak powiążem z employee to może być lipa
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;


    //[NotMapped]   JUZ NEIUZYWANE PROBLEM ROZWIAZANY

    public User Owner { get; set; }

    public ICollection<WorkGroup> WorkGroups { get; set; } = new List<WorkGroup>();
    public List<UserWorkplace> UserWorkplaces { get; set; } = new List<UserWorkplace>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
