
// アプリケーションが起動する時に一番最初に実行されるファイル

using Microsoft.EntityFrameworkCore; 
using employee_management_api.Models; 

var builder = WebApplication.CreateBuilder(args);


// ===== 各種設定 =====

// データベース接続の設定
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// CORS設定 = 異なるドメイン間のリソース共有
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:3000")  // Reactのアドレスを許可
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 全設定の反映
var app = builder.Build();


// ===== 実行 =====

app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.MapControllers();
app.Run();