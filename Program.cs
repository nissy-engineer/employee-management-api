// アプリケーションが起動する時に一番最初に実行されるファイル

using Microsoft.EntityFrameworkCore; 
using employee_management_api.Models; 

var builder = WebApplication.CreateBuilder(args);


// ===== 各種設定 =====

// データベース接続の設定
string connectionString;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // RailwayのDATABASE_URLをNpgsql形式に変換
    var uri = new Uri(databaseUrl);
    var host = uri.Host;
    var port = uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    // ローカル開発環境用
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// CORS設定 = 異なるドメイン間のリソース共有
// 環境変数 ALLOWED_ORIGINS からフロントエンドのURLを取得（カンマ区切りで複数可）
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?? "http://localhost:3000";
var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(origins)  // 環境変数から取得したURLを許可
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 全設定の反映
var app = builder.Build();


// ===== 実行 =====

app.UseStaticFiles();

app.UseCors("AllowReact");
app.MapControllers();
app.Run();










// // アプリケーションが起動する時に一番最初に実行されるファイル

// using Microsoft.EntityFrameworkCore;
// using employee_management_api.Models;

// var builder = WebApplication.CreateBuilder(args);


// // ===== 各種設定 =====

// // データベース接続の設定
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// // builder.Services.AddOpenApi();

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// builder.Services.AddControllers();

// // CORS設定 = 異なるドメイン間のリソース共有
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowReact", policy =>
//     {
//         policy.WithOrigins("http://localhost:3000")  // Reactのアドレスを許可
//               .AllowAnyHeader()
//               .AllowAnyMethod();
//     });
// });

// // 全設定の反映
// var app = builder.Build();


// // ===== 実行 =====

// app.UseStaticFiles();
// if (app.Environment.IsDevelopment())
// {
//     // app.MapOpenApi();
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
// app.UseHttpsRedirection();
// app.UseCors("AllowReact");
// app.MapControllers();
// app.Run();