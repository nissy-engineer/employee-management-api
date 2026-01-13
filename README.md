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
├── .gitignore
├── appsettings.Development.json
├── appsettings.json
├── employee-management-api.csproj
├── employee-management-api.http
├── employee-management-api.sln
├── Program.cs
└── README.md
```