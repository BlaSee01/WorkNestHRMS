namespace WorkNestHRMS.Models;

public class UserWorkplace
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int WorkplaceId { get; set; }
    public Workplace Workplace { get; set; }

    public string Role { get; set; } = "member"; // owner / member / manager
}
