namespace employee_management_api.Models
{
    public class UpdateEmployeeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string HireDate { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}