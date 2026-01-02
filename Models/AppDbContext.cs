
using Microsoft.EntityFrameworkCore;

namespace employee_management_api.Models
{
    public class AppDbContext : DbContext
    {
        // Program.csから渡されたDB接続設定を、親クラス(DbContext)に渡す
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // employeesテーブルへのアクセス窓口
        public DbSet<Employee> Employees { get; set; }

        // employee_detailsテーブルへのアクセス窓口
        public DbSet<EmployeeDetail> EmployeeDetails { get; set; }

    }
}