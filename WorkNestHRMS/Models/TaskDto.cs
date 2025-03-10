namespace WorkNestHRMS.Models
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? AssignedUserId { get; set; }
        public int? AssignedWorkGroupId { get; set; }
        public DateTime? CompletionDate { get; set; }
        public List<string> Attachments { get; set; }
        public int CreatedByUserId { get; set; }
        public int WorkplaceId { get; set; }
        public UserDto AssignedUser { get; set; }
        public WorkGroupDTO AssignedWorkGroup { get; set; }
    }
}
