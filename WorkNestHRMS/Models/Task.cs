namespace WorkNestHRMS.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? AssignedUserId { get; set; }
        public int? AssignedWorkGroupId { get; set; }
        public DateTime? CompletionDate { get; set; }
        public List<string> Attachments { get; set; } = new List<string>(); // powrót do stringa?
        public User AssignedUser { get; set; }
        public WorkGroup AssignedWorkGroup { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }
        public int WorkplaceId { get; set; }
        public Workplace Workplace { get; set; }
    }

}
