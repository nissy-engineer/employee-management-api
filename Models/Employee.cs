using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace employee_management_api.Models
{
    [Table("employees")]
    public class Employee
    {
        [Key] // 主キー
        [Column("id")] // データベースの "id" カラムと紐付ける
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("department")]
        public string Department { get; set; } = string.Empty;

        [Column("position")]
        public string Position { get; set; } = string.Empty;

        [Column("hire_date")]
        public DateTime HireDate { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [Column("is_valid")]
        public bool IsValid { get; set; } = true;
    }
}