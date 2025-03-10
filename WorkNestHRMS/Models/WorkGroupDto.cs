namespace WorkNestHRMS.Models
{
    public class WorkGroupDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<MemberDTO> Members { get; set; } = new();
    }

    public class MemberDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

}
