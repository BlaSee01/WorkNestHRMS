namespace WorkNestHRMS.Models
{
    public class TaskRequests
    {
        public class CreateTaskRequest
        {
            public string Content { get; set; }
            public DateTime DueDate { get; set; }
            public string Priority { get; set; }
            public int? AssignedUserId { get; set; }
            public int? AssignedWorkGroupId { get; set; }
        }
        public class UpdateTaskStatusRequest
        {
            public string Status { get; set; }
        }
    }
}
