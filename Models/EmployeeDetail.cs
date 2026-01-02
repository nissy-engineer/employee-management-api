using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace employee_management_api.Models
{
    [Table("employee_details")]
    public class EmployeeDetail
    {
        [Key] // 主キー
        [Column("id")]
        public int Id { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }

        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("emergency_contact")]
        public string? EmergencyContact { get; set; }

        [Column("employment_type")]
        public string? EmploymentType { get; set; }

        [Column("manager_id")]
        public int? ManagerId { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_valid")]
        public bool IsValid { get; set; } = true;
    }
}