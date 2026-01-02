using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using employee_management_api.Models;

namespace employee_management_api.Controllers
{

    // Web API用のコントローラー
    [ApiController]
    [Route("api/[controller]")]

    public class EmployeesController : ControllerBase
    {
        // コンストラクタでデータベース接続を受け取る
        private readonly AppDbContext _context;
        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // データベースから全社員データを取得
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees
                .Where(e => e.IsValid == true)
                .ToListAsync();
            return Ok(employees);
        }

        // 社員を検索
        [HttpGet("search")]
        public async Task<IActionResult> SearchEmployees([FromQuery] string? keyword)
        {
            // キーワードが空の場合は全件取得
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var allEmployees = await _context.Employees
                    .Where(e => e.IsValid == true)
                    .ToListAsync();
                return Ok(allEmployees);
            }

            // キーワードで検索（部分一致）
            var employees = await _context.Employees
                .Where(e => e.IsValid == true &&
                    (e.Name.Contains(keyword) ||
                     e.Department.Contains(keyword) ||
                     e.Position.Contains(keyword) ||
                     e.Email.Contains(keyword) ||
                     e.Phone.Contains(keyword)))
                .ToListAsync();

            return Ok(employees);
        }

        // 特定社員の詳細データを取得（employeesとemployee_detailsを結合）
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeDetail(int id)
        {
            // employeesテーブルとemployee_detailsテーブルを結合して取得
            var employee = await _context.Employees
                .Where(e => e.Id == id && e.IsValid == true)
                .Select(e => new
                {
                    // 基本情報（employeesテーブル）
                    e.Id,
                    e.Name,
                    e.Department,
                    e.Position,
                    e.HireDate,
                    e.Email,
                    e.Phone,

                    // 詳細情報（employee_detailsテーブル）
                    Details = _context.EmployeeDetails
                        .Where(ed => ed.EmployeeId == e.Id)
                        .Select(ed => new
                        {
                            ed.PhotoUrl,
                            ed.BirthDate,
                            ed.Gender,
                            ed.Address,
                            ed.EmergencyContact,
                            ed.EmploymentType,
                            ed.ManagerId,
                            ed.Notes,
                            // 上司の名前も取得
                            ManagerName = _context.Employees
                                .Where(m => m.Id == ed.ManagerId)
                                .Select(m => m.Name)
                                .FirstOrDefault()
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            // 社員が見つからない場合
            if (employee == null)
            {
                return NotFound(new { message = "社員が見つかりません" });
            }

            return Ok(employee);
        }


        // 社員のプロフィール写真をアップロード
        [HttpPost("{id}/upload-photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
        {

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "社員が見つかりません" });
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new { message = "ファイルが選択されていません" });
            }

            // ファイル形式チェック（画像のみ許可）
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(photo.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "画像ファイル（jpg, png, gif）のみアップロード可能です" });
            }

            // ファイルサイズチェック（5MB以下）
            if (photo.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "ファイルサイズは5MB以下にしてください" });
            }

            try
            {
                // ファイル名を生成（employee_{id}.拡張子）
                var fileName = $"employee_{id}{extension}";

                // 保存先のフルパスを作成
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                var filePath = Path.Combine(uploadsFolder, fileName);

                // uploadsフォルダが存在しない場合は作成
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 既存のファイルがあれば削除
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // ファイルを保存
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                // データベースに画像URLを保存
                var photoUrl = $"/uploads/{fileName}";

                // employee_detailsレコードを取得または作成
                var employeeDetail = await _context.EmployeeDetails
                    .FirstOrDefaultAsync(ed => ed.EmployeeId == id);

                if (employeeDetail == null)
                {
                    // 詳細レコードが存在しない場合は新規作成
                    employeeDetail = new EmployeeDetail
                    {
                        EmployeeId = id,
                        PhotoUrl = photoUrl
                    };
                    _context.EmployeeDetails.Add(employeeDetail);
                }
                else
                {
                    // 既存レコードを更新
                    employeeDetail.PhotoUrl = photoUrl;
                    employeeDetail.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "画像をアップロードしました",
                    photoUrl = photoUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "画像のアップロードに失敗しました", error = ex.Message });
            }
        }

        // 社員情報を更新
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
        {
            // 社員が存在するか確認
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "社員が見つかりません" });
            }

            try
            {
                // 基本情報を更新
                employee.Name = request.Name;
                employee.Department = request.Department;
                employee.Position = request.Position;
                employee.HireDate = DateTime.Parse(request.HireDate);
                employee.Email = request.Email;
                employee.Phone = request.Phone;

                // 詳細情報を更新
                var employeeDetail = await _context.EmployeeDetails
                    .FirstOrDefaultAsync(ed => ed.EmployeeId == id);

                if (employeeDetail != null)
                {
                    employeeDetail.EmploymentType = request.EmploymentType;
                    employeeDetail.Notes = request.Notes;
                    employeeDetail.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "社員情報を更新しました" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新に失敗しました", error = ex.Message });
            }
        }

        // 社員を論理削除
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            // 社員が存在するか確認
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "社員が見つかりません" });
            }

            // 既に削除済みか確認
            if (!employee.IsValid)
            {
                return BadRequest(new { message = "この社員は既に削除されています" });
            }

            try
            {
                // employeesテーブルのis_validをFALSEに
                employee.IsValid = false;

                // employee_detailsテーブルのis_validもFALSEに
                var employeeDetail = await _context.EmployeeDetails
                    .FirstOrDefaultAsync(ed => ed.EmployeeId == id);

                if (employeeDetail != null)
                {
                    employeeDetail.IsValid = false;
                    employeeDetail.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "社員を削除しました" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "削除に失敗しました", error = ex.Message });
            }
        }



    }
}