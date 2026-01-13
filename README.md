## セットアップ
1. PostgreSQLをインストール
2. `appsettings.json`の`YOUR_PASSWORD_HERE`を実際のパスワードに変更
3. データベースを作成

## ツリー図: 
```
employee-management-api/
├── Controllers/
│   └── EmployeesController.cs
├── Models/ 
│   ├── AppDbContext.cs
│   ├── Employee.cs 
│   ├── EmployeeDetail.cs
│   └── UpdateEmployeeRequest.cs
├── Properties/
│   └── launchSettings.json
├── wwwroot/
│   └── uploads/ 
├── Program.cs
├── appsettings.json
└── employee-management-api.csproj
```