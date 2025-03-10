namespace WorkNestHRMS.Models
{
    public class WorkGroupRequests
    {
        public class CreateWorkGroupRequest
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class AddUserToWorkGroupRequest
        {
            public int UserId { get; set; }
        }
        public class RemoveUserFromWorkGroupRequest
        {
            public int UserId { get; set; }
        }
    }
}
