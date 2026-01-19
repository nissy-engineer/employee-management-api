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
                employee.HireDate = DateTime.SpecifyKind(DateTime.Parse(request.HireDate), DateTimeKind.Utc);
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


        // 新規社員を登録
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] UpdateEmployeeRequest request)
        {
            // バリデーション
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Department) ||
                string.IsNullOrWhiteSpace(request.Position) ||
                string.IsNullOrWhiteSpace(request.HireDate) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { message = "必須項目を入力してください" });
            }

            try
            {
                // 新規社員データを作成
                var newEmployee = new Employee
                {
                    Name = request.Name,
                    Department = request.Department,
                    Position = request.Position,
                    HireDate = DateTime.SpecifyKind(DateTime.Parse(request.HireDate), DateTimeKind.Utc),
                    Email = request.Email,
                    Phone = request.Phone,
                    IsValid = true
                };

                // employeesテーブルに追加
                _context.Employees.Add(newEmployee);
                await _context.SaveChangesAsync();

                // 詳細情報がある場合は、employee_detailsテーブルにも登録
                if (!string.IsNullOrWhiteSpace(request.EmploymentType) ||
                    !string.IsNullOrWhiteSpace(request.Notes))
                {
                    var employeeDetail = new EmployeeDetail
                    {
                        EmployeeId = newEmployee.Id,
                        EmploymentType = request.EmploymentType,
                        Notes = request.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsValid = true
                    };

                    _context.EmployeeDetails.Add(employeeDetail);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "社員を登録しました",
                    employeeId = newEmployee.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "登録に失敗しました",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }


        // 社員データをCSVインポート
        [HttpPost("import")]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "ファイルが選択されていません" });
            }

            // ファイル形式チェック
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv")
            {
                return BadRequest(new { message = "CSVファイルのみアップロード可能です" });
            }

            int successCount = 0;
            int failureCount = 0;
            var errors = new List<string>();

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8))
                {
                    // ヘッダー行を読み込み
                    var header = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(header))
                    {
                        return BadRequest(new { message = "CSVファイルが空です" });
                    }

                    // 期待されるヘッダー
                    var expectedHeaders = new[]
                    {
                        "社員ID", "氏名", "所属部署", "役職", "入社日",
                        "メールアドレス", "電話番号", "生年月日", "性別",
                        "住所", "緊急連絡先", "雇用形態", "直属の上司", "備考"
                    };

                    // ヘッダーをパース
                    var actualHeaders = ParseCsvLine(header);

                    // ヘッダーの数をチェック
                    if (actualHeaders.Length < 7)
                    {
                        return BadRequest(new
                        {
                            message = "CSVファイルのヘッダーが不正です。必須項目（社員ID～電話番号）が不足しています。"
                        });
                    }

                    // ヘッダー項目を検証（最低7項目が一致するか）
                    bool headerValid = true;
                    for (int i = 0; i < Math.Min(7, actualHeaders.Length); i++)
                    {
                        if (actualHeaders[i].Trim() != expectedHeaders[i])
                        {
                            headerValid = false;
                            break;
                        }
                    }

                    if (!headerValid)
                    {
                        return BadRequest(new
                        {
                            message = "CSVファイルのヘッダー形式が正しくありません",
                            expected = string.Join(",", expectedHeaders),
                            actual = string.Join(",", actualHeaders)
                        });
                    }

                    int lineNumber = 1;
                    while (!reader.EndOfStream)
                    {
                        lineNumber++;
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            // CSV行をパース
                            var values = ParseCsvLine(line);

                            // 必須項目のバリデーション（社員IDを除く6項目）
                            if (values.Length < 7)
                            {
                                errors.Add($"行{lineNumber}: データが不足しています");
                                failureCount++;
                                continue;
                            }

                            // 必須項目の空チェック
                            if (string.IsNullOrWhiteSpace(values[1]) || // 氏名
                                string.IsNullOrWhiteSpace(values[2]) || // 所属部署
                                string.IsNullOrWhiteSpace(values[3]) || // 役職
                                string.IsNullOrWhiteSpace(values[4]) || // 入社日
                                string.IsNullOrWhiteSpace(values[5]) || // メールアドレス
                                string.IsNullOrWhiteSpace(values[6]))   // 電話番号
                            {
                                errors.Add($"行{lineNumber}: 必須項目が入力されていません");
                                failureCount++;
                                continue;
                            }

                            var email = values[5].Trim();

                            // メールアドレスで重複チェック
                            var existingEmployee = await _context.Employees
                                .FirstOrDefaultAsync(e => e.Email == email && e.IsValid == true);

                            if (existingEmployee != null)
                            {
                                errors.Add($"行{lineNumber}: メールアドレス「{email}」は既に登録されています");
                                failureCount++;
                                continue;
                            }

                            // 日付のパースチェック
                            DateTime hireDate;
                            if (!DateTime.TryParse(values[4].Trim(), out hireDate))
                            {
                                errors.Add($"行{lineNumber}: 入社日の形式が正しくありません");
                                failureCount++;
                                continue;
                            }

                            // 新規社員データを作成（社員IDは自動採番なのでスキップ）
                            var newEmployee = new Employee
                            {
                                Name = values[1].Trim(),
                                Department = values[2].Trim(),
                                Position = values[3].Trim(),
                                HireDate = DateTime.SpecifyKind(hireDate, DateTimeKind.Utc),
                                Email = email,
                                Phone = values[6].Trim(),
                                IsValid = true
                            };

                            _context.Employees.Add(newEmployee);
                            await _context.SaveChangesAsync();

                            // 詳細情報がある場合は追加
                            if (values.Length > 7)
                            {
                                DateTime? birthDate = null;
                                if (values.Length > 7 && !string.IsNullOrWhiteSpace(values[7]))
                                {
                                    if (DateTime.TryParse(values[7].Trim(), out DateTime parsedBirthDate))
                                    {
                                        birthDate = DateTime.SpecifyKind(parsedBirthDate, DateTimeKind.Utc);
                                    }
                                }

                                var employeeDetail = new EmployeeDetail
                                {
                                    EmployeeId = newEmployee.Id,
                                    BirthDate = birthDate,
                                    Gender = values.Length > 8 ? values[8].Trim() : null,
                                    Address = values.Length > 9 ? values[9].Trim() : null,
                                    EmergencyContact = values.Length > 10 ? values[10].Trim() : null,
                                    EmploymentType = values.Length > 11 ? values[11].Trim() : null,
                                    Notes = values.Length > 13 ? values[13].Trim() : null,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    IsValid = true
                                };

                                _context.EmployeeDetails.Add(employeeDetail);
                                await _context.SaveChangesAsync();
                            }

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"行{lineNumber}: {ex.Message}");
                            failureCount++;
                        }
                    }
                }

                return Ok(new
                {
                    message = "インポート処理が完了しました",
                    successCount = successCount,
                    failureCount = failureCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "インポートに失敗しました",
                    error = ex.Message
                });
            }
        }

        // CSV行をパースする関数（ダブルクォートとカンマのエスケープに対応）
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // ダブルクォートのエスケープ（""）
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        // クォートの開始/終了
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // カンマで区切る（クォート内でない場合のみ）
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }


        // 社員データをCSVエクスポート用に全件取得（詳細情報含む）
        [HttpGet("export")]
        public async Task<IActionResult> ExportEmployees()
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.IsValid == true)
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        e.Department,
                        e.Position,
                        e.HireDate,
                        e.Email,
                        e.Phone,
                        Details = _context.EmployeeDetails
                            .Where(ed => ed.EmployeeId == e.Id && ed.IsValid == true)
                            .Select(ed => new
                            {
                                ed.BirthDate,
                                ed.Gender,
                                ed.Address,
                                ed.EmergencyContact,
                                ed.EmploymentType,
                                ed.Notes,
                                ManagerName = _context.Employees
                                    .Where(m => m.Id == ed.ManagerId)
                                    .Select(m => m.Name)
                                    .FirstOrDefault()
                            })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // CSVヘッダー
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("社員ID,氏名,所属部署,役職,入社日,メールアドレス,電話番号,生年月日,性別,住所,緊急連絡先,雇用形態,直属の上司,備考");

                // CSVデータ行
                foreach (var emp in employees)
                {
                    csv.AppendLine(string.Join(",",
                        emp.Id,
                        EscapeCsvField(emp.Name),
                        EscapeCsvField(emp.Department),
                        EscapeCsvField(emp.Position),
                        emp.HireDate.ToString("yyyy-MM-dd"),
                        EscapeCsvField(emp.Email),
                        EscapeCsvField(emp.Phone),
                        emp.Details?.BirthDate?.ToString("yyyy-MM-dd") ?? "",
                        EscapeCsvField(emp.Details?.Gender ?? ""),
                        EscapeCsvField(emp.Details?.Address ?? ""),
                        EscapeCsvField(emp.Details?.EmergencyContact ?? ""),
                        EscapeCsvField(emp.Details?.EmploymentType ?? ""),
                        EscapeCsvField(emp.Details?.ManagerName ?? ""),
                        EscapeCsvField(emp.Details?.Notes ?? "")
                    ));
                }

                // BOM付きUTF-8でCSVを返す（Excelで文字化け防止）
                var bytes = System.Text.Encoding.UTF8.GetPreamble()
                    .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                    .ToArray();

                return File(bytes, "text/csv", $"社員一覧_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "エクスポートに失敗しました", error = ex.Message });
            }
        }

        // CSVフィールドのエスケープ処理（カンマ、改行、ダブルクォートを含む場合）
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // カンマ、改行、ダブルクォートが含まれる場合はダブルクォートで囲む
            if (field.Contains(",") || field.Contains("\n") || field.Contains("\""))
            {
                // ダブルクォートを2つにエスケープ
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

    }
}