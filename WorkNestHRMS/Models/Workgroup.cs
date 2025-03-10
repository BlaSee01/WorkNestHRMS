namespace WorkNestHRMS.Models
{
    public class WorkGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int WorkplaceId { get; set; }
        public Workplace Workplace { get; set; }
        public ICollection<UserWorkGroup> UserWorkGroups { get; set; }
        public ICollection<Task> Tasks { get; set; } = new List<Task>();

    }

    public class UserWorkGroup
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int WorkGroupId { get; set; }
        public WorkGroup WorkGroup { get; set; }
    }

}
