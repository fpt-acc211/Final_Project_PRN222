# Quiz Management System

Ứng dụng web quản lý và luyện tập câu hỏi trắc nghiệm, được xây dựng bằng ASP.NET Core MVC và SQL Server cho final project PRN222.

> Trạng thái kiểm tra gần nhất: .NET SDK 9.0.313, Release build `0 warning / 0 error`, `120/120` test đạt.

## Tính năng chính

- Quản lý học liệu theo cấu trúc **Môn học → Bộ đề → Câu hỏi → Đáp án**.
- Hỗ trợ câu hỏi một hoặc nhiều đáp án đúng, Markdown và soft delete.
- Làm quiz có trộn câu, giới hạn thời gian, lưu lượt làm, kết quả và lịch sử chi tiết.
- Học bằng flashcard với lịch ôn spaced repetition lưu trong database; xem thống kê cá nhân hoặc thống kê nội dung.
- Import câu hỏi từ `.xlsx` hoặc Aiken text; export bộ đề ra `.docx` và `.pdf` Unicode.
- Báo cáo câu hỏi có vấn đề và theo dõi trạng thái xử lý.
- Cookie authentication, xác minh email, quên/đặt lại mật khẩu, phân quyền `Admin` / `Mentor` / `User`, rate limiting và khóa đăng nhập lưu bền vững.
- Admin quản lý tài khoản, vai trò, trạng thái hoạt động và học liệu toàn hệ thống.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Backend | C#, ASP.NET Core MVC, .NET 9 |
| View | Razor Views, Bootstrap 5, CSS |
| Database | SQL Server, Entity Framework Core 9 |
| Authentication | Cookie Authentication, policy-based authorization |
| Testing | xUnit, ASP.NET Core MVC tests, SQL Server LocalDB integration tests |
| Kiến trúc | N-Tier: Controller → Service → Repository → DAO → DbContext |

## Cấu trúc solution

```text
Final_Project_PRN222/
├── BusinessObjects/         # Entity và read model
├── DataAccessObjects/       # DbContext, DAO và truy vấn dữ liệu
├── Repositories/            # Repository interfaces và implementations
├── Services/                # Business logic, validation, import/export
├── QuizManagement/          # ASP.NET Core MVC application
│   ├── Controllers/
│   ├── Infrastructure/
│   ├── ViewModels/
│   ├── Views/
│   └── wwwroot/
├── QuizManagement.Tests/    # Unit, controller, view và integration tests
├── CreateDB.sql             # Bootstrap schema hiện tại
├── SeedDemoData.sql         # Dữ liệu demo tùy chọn
├── global.json              # Pin .NET SDK
└── QuizManagementSystem.slnx
```

Luồng xử lý chính:

```text
Razor View → Controller → Service → Repository → DAO → DbContext → SQL Server
```

## Yêu cầu môi trường

- [.NET SDK 9.0.313](https://dotnet.microsoft.com/download/dotnet/9.0), được pin trong `global.json`.
- SQL Server 2019 trở lên, SQL Server Express hoặc SQL Server LocalDB.
- SQL Server Management Studio hoặc công cụ có thể chạy T-SQL script.
- Visual Studio 2022 là tùy chọn; có thể build và chạy hoàn toàn bằng CLI.
- SQL Server LocalDB nếu muốn chạy đầy đủ integration tests.

## Cài đặt và chạy

Thực hiện các bước sau từ thư mục gốc repository.

### 1. Tạo database

Mở và chạy toàn bộ [CreateDB.sql](./CreateDB.sql) bằng SQL Server Management Studio.

Script có ba hành vi rõ ràng:

- Chưa có `QuizManagementDB`: tự tạo database và schema đầy đủ.
- Database đã đúng schema hiện tại: kiểm tra rồi kết thúc, không thay đổi dữ liệu.
- Database cũ hoặc thiếu schema: dừng với lỗi `51020` hoặc `51021`; không tự sửa và không tự xóa dữ liệu.

Project dùng `CreateDB.sql` làm nguồn schema chính, không yêu cầu chạy EF migration.

Nếu đang dùng schema trước ngày 22/07/2026, chạy [UpgradeDB_20260722.sql](./UpgradeDB_20260722.sql) một lần để bổ sung xác minh email, khóa đăng nhập bền vững và tiến độ flashcard mà không xóa dữ liệu.

### 2. Cấu hình connection string

Tạo file local từ mẫu:

```powershell
Copy-Item .\QuizManagement\appsettings.Local.example.json `
          .\QuizManagement\appsettings.Local.json
```

Cập nhật `ConnectionStrings:DefaultConnection` trong `appsettings.Local.json`.

SQL Server Express với SQL Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=QuizManagementDB;User Id=sa;Password=your_password;TrustServerCertificate=True;"
  }
}
```

Windows Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=QuizManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

`appsettings.Local.json` đã được Git ignore. Không commit connection string hoặc credential thật.

Để gửi email thật, cấu hình section `Email` theo [appsettings.Local.example.json](./QuizManagement/appsettings.Local.example.json). Khi chạy môi trường Development mà chưa cấu hình `Email:Host`, liên kết xác minh và đặt lại mật khẩu được ghi vào log ứng dụng.

### 3. Restore, build và test

```powershell
dotnet restore
dotnet build QuizManagementSystem.slnx -c Release
dotnet test QuizManagementSystem.slnx -c Release --no-build
```

Integration tests tạo database tạm trên `(localdb)\MSSQLLocalDB` và tự xóa sau khi chạy.

### 4. Chạy ứng dụng

```powershell
dotnet run --project QuizManagement --launch-profile http
```

Mở [http://localhost:5039](http://localhost:5039).

Với Visual Studio, mở `QuizManagementSystem.slnx`, chọn `QuizManagement` làm startup project rồi chạy profile `http` hoặc `https`.

## Dữ liệu demo

Dữ liệu demo là tùy chọn và chỉ được dùng trên database cô lập. Sau khi tạo schema, mở [SeedDemoData.sql](./SeedDemoData.sql) trong SSMS. Trong chính query session đó, chạy opt-in sau trước khi chạy phần còn lại của script:

```sql
EXEC sys.sp_set_session_context
    @key = N'QuizManagement.AllowDemoSeed',
    @value = 1;
```

Không đổi connection/session giữa lệnh opt-in và script seed. Opt-in được tự xóa khi script hoàn tất hoặc rollback.

Script có thể chạy lại; mỗi lần chạy chỉ thay thế dữ liệu thuộc các seed identity cố định. Dữ liệu mẫu gồm tài khoản, học liệu, quiz attempt, lịch sử, báo cáo câu hỏi và login attempt.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin.demo@quiz.local` | `Test@123456` |
| Mentor | `mentor.demo@quiz.local` | `Test@123456` |
| User | `user.demo@quiz.local` | `Test@123456` |

> [!WARNING]
> Các credential trên là công khai. Không chạy `SeedDemoData.sql` trên production hoặc database chứa dữ liệu thật.

### Reset database demo

Khi database demo có schema cũ, có thể xóa và tạo lại:

```sql
USE [master];
ALTER DATABASE [QuizManagementDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [QuizManagementDB];
```

Sau đó chạy lại `CreateDB.sql` và, nếu cần, `SeedDemoData.sql`. Thao tác này xóa toàn bộ dữ liệu hiện có.

## Tạo Admin local tùy chọn

Nếu không dùng demo seed, có thể bật `AdminSeed` trong `appsettings.Local.json`:

```json
{
  "AdminSeed": {
    "Enabled": true,
    "Username": "Admin",
    "Email": "admin@example.local",
    "Password": "a-strong-local-password"
  }
}
```

Mật khẩu phải dài 8–100 ký tự. Seed chỉ tạo tài khoản khi hệ thống chưa có Admin. Sau lần chạy đầu tiên, đặt `Enabled` về `false`.

## Phân quyền

| Role | Quyền chính |
| --- | --- |
| `Admin` | Quản lý toàn bộ học liệu, tài khoản, role, login attempt và báo cáo |
| `Mentor` | Xem toàn bộ học liệu; quản lý/import/export nội dung do mình sở hữu |
| `User` | Xem học liệu, học flashcard, làm quiz, gửi báo cáo và xem thống kê cá nhân |

Các ràng buộc quan trọng:

- Mentor chỉ sửa hoặc xóa học liệu thuộc sở hữu của mình; Admin quản lý toàn bộ.
- Admin không thể tự hạ role, tự vô hiệu hóa hoặc vô hiệu hóa Admin hoạt động cuối cùng.
- Tên môn học không trùng trong cùng chủ sở hữu; tên bộ đề không trùng trong cùng môn học.
- Mỗi câu hỏi có ít nhất hai đáp án và phải thỏa quy tắc đáp án đúng theo loại câu hỏi.
- Xóa môn học, bộ đề và câu hỏi sử dụng soft delete để giữ lịch sử.

## Kiểm thử

Test project hiện có 120 test, bao phủ:

- Service và validation nghiệp vụ.
- Controller, authorization, token xác minh/đặt lại mật khẩu, khóa đăng nhập bền vững và error handling.
- Quiz attempt, lịch sử và result snapshot.
- Import atomicity, concurrency và unique constraints trên SQL Server.
- Demo seed, spaced repetition flashcard, UTC date/time, soft delete và quyền sở hữu dữ liệu.
- PDF Unicode và phản hồi lỗi trên Razor Views.

Chạy lại toàn bộ:

```powershell
dotnet test QuizManagementSystem.slnx -c Release
```

Integration tests mặc định dùng `(localdb)\MSSQLLocalDB`. Để dùng SQL Server khác,
đặt connection string có quyền tạo/xóa database tạm trước khi chạy test:

```powershell
$env:QUIZ_TEST_SQLSERVER_CONNECTION = `
    (Get-Content .\QuizManagement\appsettings.Local.json | ConvertFrom-Json).ConnectionStrings.DefaultConnection
dotnet test QuizManagementSystem.slnx -c Release
```

## Lưu ý vận hành

- Production phải cấu hình SMTP và lưu bền vững ASP.NET Core Data Protection keys để các token vẫn hợp lệ khi restart hoặc chạy nhiều instance.
- Responsive đã được kiểm tra trên viewport 320px, 375px và 1440px; nên chạy lại QA trên thiết bị mục tiêu khi thay đổi layout.

## Xử lý lỗi thường gặp

- **Không kết nối được database:** kiểm tra SQL Server đang chạy và `DefaultConnection` trong `appsettings.Local.json`.
- **`CreateDB.sql` báo `51020`/`51021`:** database đang có schema cũ hoặc không đầy đủ; với dữ liệu demo, reset database rồi chạy lại script.
- **`SeedDemoData.sql` báo `51019`:** opt-in chưa được đặt trong đúng SQL session.
- **Integration tests không kết nối được:** khởi động LocalDB `MSSQLLocalDB` hoặc đặt `QUIZ_TEST_SQLSERVER_CONNECTION` để dùng SQL Server khác.
- **HTTPS certificate chưa được trust:** dùng profile `http`, hoặc chạy `dotnet dev-certs https --trust`.

## Ghi chú bảo mật

- Không commit `appsettings.Local.json`, Data Protection keys, `bin/`, `obj/` hoặc `.vs/`.
- Không dùng demo credential ngoài môi trường demo cô lập.
- Không bật `AdminSeed` với password mẫu hoặc giữ seed bật sau khi đã tạo Admin.
