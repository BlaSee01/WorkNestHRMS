using System.ComponentModel.DataAnnotations;

namespace WorkNestHRMS.Models
{
    public class Employee
    {
     
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // 1:1 z User
        public int UserId { get; set; }
        public User? User { get; set; } = null!;
    }
}
