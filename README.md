# Quiz Management System

Quiz Management System là ứng dụng web hỗ trợ người dùng tạo, quản lý và luyện tập câu hỏi trắc nghiệm theo cấu trúc **Môn học -> Bộ đề -> Câu hỏi -> Đáp án**. Dự án được xây dựng bằng ASP.NET Core MVC theo kiến trúc phân tầng, phù hợp cho final project môn PRN222.

## Mục Tiêu Dự Án

- Quản lý ngân hàng câu hỏi cá nhân theo từng người dùng.
- Hỗ trợ câu hỏi một đáp án đúng và nhiều đáp án đúng.
- Cho phép người dùng luyện tập/làm bài kiểm tra từ bộ đề đã tạo.
- Lưu lịch sử làm bài và kết quả để theo dõi tiến độ học tập.
- Duy trì cấu trúc code rõ ràng, dễ bảo trì và dễ mở rộng.

## Tech Stack

| Thành phần | Công nghệ |
| --- | --- |
| Web Framework | ASP.NET Core MVC (.NET 9) |
| Language | C# |
| View Engine | Razor Views |
| Database | SQL Server |
| ORM | Entity Framework Core 9 |
| Authentication | Cookie Authentication + Policy-based Authorization |
| UI Framework | Bootstrap 5 Dark Mode + CSS Glassmorphism |
| Architecture | N-Tier, DAO, Repository, Service |
| Import/Export | OpenXML `.xlsx` parser, custom `.docx`/`.pdf` export |

## Trạng Thái Hiện Tại

| Nhóm chức năng | Trạng thái |
| --- | --- |
| Khởi tạo solution N-Tier | Hoàn thành |
| Database script và EF Core models | Hoàn thành |
| Cookie authentication + Role/Policy | Hoàn thành |
| Đăng ký, đăng nhập, đăng xuất | Hoàn thành |
| CRUD Môn học | Hoàn thành |
| CRUD Bộ đề | Hoàn thành |
| CRUD Câu hỏi và Đáp án | Hoàn thành |
| Soft delete và Ownership | Hoàn thành |
| Quiz engine (shuffle, chấm điểm, lưu history) | Hoàn thành |
| Test history và Dashboard | Hoàn thành |
| Thống kê nâng cao + biểu đồ | Hoàn thành |
| Flashcard (spaced repetition trong phiên) | Hoàn thành |
| Import câu hỏi từ Excel (.xlsx) và text | Hoàn thành |
| Export bộ đề ra Word (.docx) và PDF | Hoàn thành |
| Admin panel (quản lý user, Role) | Hoàn thành |
| Hồ sơ cá nhân (xem, sửa, đổi mật khẩu) | Hoàn thành |
| Dark Theme + Glassmorphism UI | Hoàn thành |
| Cải thiện readability / contrast (WCAG AA) | Hoàn thành |
| Responsive mobile | Cơ bản hoạt động, chưa kiểm tra đầy đủ |
| Demo data và seed | Hoàn thành |

Chi tiết tiến độ được theo dõi tại [TASK_PROGRESS.md](./TASK_PROGRESS.md).

## Kiến Trúc Hệ Thống

Dự án được chia thành các tầng chính:

| Project | Vai trò |
| --- | --- |
| `BusinessObjects` | Chứa entity/model ánh xạ database: `User`, `Subject`, `Deck`, `Question`, `Answer`, `TestHistory`, `TestResultDetail` |
| `DataAccessObjects` | Chứa `QuizManagementDbContext` và các lớp DAO xử lý truy vấn dữ liệu |
| `Repositories` | Định nghĩa interface và repository gọi xuống DAO |
| `Services` | Chứa business logic, validation và điều phối nghiệp vụ |
| `QuizManagement` | ASP.NET Core MVC app: Controllers, ViewModels, Razor Views, static assets |

Luồng xử lý chính:

```text
Razor View -> Controller -> Service -> Repository -> DAO -> DbContext -> SQL Server
```

Ví dụ pattern đang dùng:

```csharp
public void UpdateSubject(Subject subject)
    => SubjectDAO.Instance.UpdateSubject(_context, subject);
```

DAO sử dụng `Instance` để giữ style quen thuộc của môn học, còn `DbContext` vẫn được quản lý bởi Dependency Injection để tránh hard-code connection string và hạn chế lỗi lifetime trong web app.

## Cấu Trúc Thư Mục

```text
Final_Project_PRN222/
├── BusinessObjects/
├── DataAccessObjects/
├── Repositories/
├── Services/
├── QuizManagement/
│   ├── Controllers/
│   ├── ViewModels/
│   ├── Views/
│   └── wwwroot/
├── CreateDB.sql
├── SeedDemoData.sql
├── TASK_PROGRESS.md
└── README.md
```

## Database

Các bảng chính:

- `Users`
- `Subjects`
- `Decks`
- `Questions`
- `Answers`
- `TestHistories`
- `TestResultDetails`

Giới hạn đăng nhập sai đang được xử lý bằng `IMemoryCache` ở tầng web, không lưu thành bảng riêng trong database.

Quan hệ chính:

```text
User 1 - n Subject
Subject 1 - n Deck
Deck 1 - n Question
Question 1 - n Answer
User 1 - n TestHistory
Deck 1 - n TestHistory
TestHistory 1 - n TestResultDetail
```

Script tạo database nằm tại [CreateDB.sql](./CreateDB.sql). Dữ liệu demo nằm tại [SeedDemoData.sql](./SeedDemoData.sql).

## Hướng Dẫn Cài Đặt

### 1. Yêu cầu môi trường

- Visual Studio 2022 hoặc mới hơn
- .NET SDK hỗ trợ .NET 9
- SQL Server 2019+ hoặc SQL Server Express
- SQL Server Management Studio hoặc công cụ tương đương

### 2. Tạo database

Mở SQL Server Management Studio và chạy script schema hoàn chỉnh:

```text
CreateDB.sql
```

`CreateDB.sql` đã bao gồm các trường Role/Profile/Security của Phase 2 (`AvatarUrl`, `IsDisabled`, `SecurityStamp`). Project dùng file này làm nguồn schema chính và không chạy EF `InitialCreate`.

Nếu cần dữ liệu demo, chạy thêm:

```text
SeedDemoData.sql
```

Script demo có thể chạy lại nhiều lần và tạo sẵn 3 tài khoản:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin.demo@quiz.local` | `Test@123456` |
| Mentor | `mentor.demo@quiz.local` | `Test@123456` |
| User | `user.demo@quiz.local` | `Test@123456` |

Database mặc định:

```text
QuizManagementDB
```

### 3. Cấu hình connection string

Copy file mẫu:

```text
QuizManagement/appsettings.Local.example.json
```

thành:

```text
QuizManagement/appsettings.Local.json
```

Sau đó cập nhật `DefaultConnection` trong `appsettings.Local.json` theo máy local của bạn.

Ví dụ dùng SQL Server Express:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=QuizManagementDB;User Id=sa;Password=your_password;TrustServerCertificate=True;"
}
```

Nếu dùng Windows Authentication:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=QuizManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Không sửa connection string thật trong `appsettings.json`. `appsettings.Local.json` đã được ignore bởi Git.

### 4. Tạo Admin đầu tiên (tùy chọn)

Trong `appsettings.Local.json`, bật `AdminSeed` và đặt credential riêng:

```json
"AdminSeed": {
  "Enabled": true,
  "Username": "Admin",
  "Email": "admin@example.local",
  "Password": "a-strong-local-password"
}
```

Password phải có ít nhất 12 ký tự. Seed chỉ chạy khi chưa có tài khoản mang role `Admin`; ứng dụng không ghi password vào log. Sau khi tạo Admin, nên đặt `Enabled` về `false`.

### 5. Restore và build project

Tại thư mục gốc solution:

```bash
dotnet restore
dotnet build
```

### 6. Chạy ứng dụng

Cách 1: chạy bằng Visual Studio:

1. Mở `QuizManagementSystem.slnx`.
2. Set `QuizManagement` làm startup project.
3. Chạy bằng `Ctrl + F5`.

Cách 2: chạy bằng CLI:

```bash
dotnet run --project QuizManagement --launch-profile http
```

Ứng dụng mặc định chạy tại:

```text
http://localhost:5039
```

## Luồng Sử Dụng Hiện Có

### Luồng cơ bản

1. Đăng ký tài khoản / đăng nhập.
2. User duyệt **Môn học** → chọn **Bộ đề** → học Flashcard hoặc làm Quiz.
3. Mentor tạo **Môn học** → tạo **Bộ đề** → tạo **Câu hỏi** và **Đáp án** thuộc sở hữu của mình.
4. Admin có thể quản lý toàn bộ học liệu và tài khoản trong hệ thống.
5. Loại câu hỏi: `1` = một đáp án đúng, `2` = nhiều đáp án đúng.
6. Vào bộ đề → bấm **Bắt đầu Quiz** → cấu hình số câu → làm bài → xem kết quả.
7. Xem lịch sử và thống kê học tập cá nhân tại `Thống kê`.

### Luồng nâng cao

- **Import**: Vào bộ đề → Import → tải file `.xlsx` mẫu → điền câu hỏi → upload và xem preview → xác nhận.
- **Export**: Vào bộ đề → Export → chọn định dạng Word hoặc PDF → tải file về.
- **Flashcard**: Vào bộ đề → Flashcard → lật thẻ, đánh dấu nhớ/chưa nhớ để ôn lại.
- **Markdown**: Nội dung câu hỏi và giải thích hỗ trợ cú pháp Markdown cơ bản.

### Phân quyền

| Role | Quyền |
| --- | --- |
| `Admin` | Toàn quyền học liệu; xem dashboard; quản lý user, role và trạng thái tài khoản |
| `Mentor` | Xem toàn bộ học liệu; CRUD/import/export học liệu do mình sở hữu |
| `User` | Xem học liệu; học Flashcard; làm Quiz; xem lịch sử và thống kê cá nhân |

Hệ thống không cho Admin tự hạ role, tự vô hiệu hóa hoặc vô hiệu hóa Admin đang hoạt động cuối cùng.

## Quy Tắc Nghiệp Vụ Đang Áp Dụng

- Mọi tài khoản đã đăng nhập được xem học liệu đang hoạt động.
- Mentor chỉ được sửa/xóa học liệu thuộc sở hữu của mình; Admin được quản lý toàn bộ.
- User không được tạo, sửa, xóa, import hoặc export học liệu.
- Tên môn học không được trùng trong cùng một user.
- Tên bộ đề không được trùng trong cùng một môn học.
- Mỗi câu hỏi phải có ít nhất 2 đáp án.
- Câu hỏi một đáp án đúng phải có đúng 1 đáp án được đánh dấu đúng.
- Câu hỏi nhiều đáp án đúng phải có ít nhất 1 đáp án được đánh dấu đúng.
- Xóa môn học/bộ đề/câu hỏi dùng soft delete thay vì xóa vật lý.

## Roadmap

### Đã hoàn thành

- [x] Quiz Engine: cấu hình, shuffle Fisher-Yates, chấm điểm single/multiple choice, lưu history + result details.
- [x] Dashboard và lịch sử làm bài, xem lại kết quả chi tiết.
- [x] Thống kê nâng cao: biểu đồ 12 lần gần nhất, group theo môn/bộ đề.
- [x] Import câu hỏi từ Excel (.xlsx) và text (Aiken format) với preview.
- [x] Export bộ đề ra Word (.docx) và PDF.
- [x] Markdown cho nội dung câu hỏi và giải thích.
- [x] Flashcard / spaced repetition trong phiên.
- [x] Admin panel: tổng quan hệ thống, quản lý người dùng và vai trò.
- [x] Dark Theme + Glassmorphism toàn bộ UI (12 views).
- [x] Cải thiện contrast và readability đạt WCAG AA.
- [x] Seed demo data: 3 tài khoản, 2 subjects, 3 decks, 21 questions.

### Còn lại trước demo

- [ ] Kiểm tra responsive đầy đủ trên mobile.
- [ ] Slide/demo script.
- [ ] Tag release bản nộp cuối.

## Git Workflow Đề Xuất

Không code trực tiếp trên `main`.

Tạo branch theo chức năng:

```bash
git checkout -b feature/login
git checkout -b feature/subject-crud
git checkout -b feature/quiz-engine
```

Trước khi bắt đầu làm việc:

```bash
git pull origin main
```

Trước khi push:

```bash
dotnet build
```

## Ghi Chú Phát Triển

- File lỗi/debug có thể ghi vào `Error.md` để trao đổi nhanh trong quá trình phát triển.
- Không commit `bin/`, `obj/`, `.vs/` và các file cấu hình local chứa thông tin nhạy cảm.
- Khi thêm chức năng mới, cập nhật lại `TASK_PROGRESS.md` để team theo dõi tiến độ.
