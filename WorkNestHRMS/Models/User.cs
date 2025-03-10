namespace WorkNestHRMS.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";  // user / admin

    public List<UserWorkplace> UserWorkplaces { get; set; } = new List<UserWorkplace>();
    public ICollection<UserWorkGroup> UserWorkGroups { get; set; } = new List<UserWorkGroup>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();

    // 1:1 z Employee
    public Employee? Employee { get; set; } = null!;
}
