# Task Progress - Quiz Management System

Cập nhật gần nhất: 2026-06-14

Mục tiêu: triển khai hoàn chỉnh ứng dụng ASP.NET Core MVC cho final project PRN222, bao gồm quản lý ngân hàng câu hỏi, làm bài trắc nghiệm, chấm điểm và lưu lịch sử.

## 1. Quy Ước Theo Dõi

| Ký hiệu | Ý nghĩa |
| --- | --- |
| `[x]` | Đã xong và đã kiểm tra cơ bản |
| `[ ]` | Chưa làm |
| `[~]` | Đang làm hoặc đã có một phần |
| `[!]` | Cần quyết định / có rủi ro |
| `[B]` | Bonus, làm sau MVP |

## 2. Snapshot Hiện Tại

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Solution N-Tier | `[x]` | Đã có `BusinessObjects`, `DataAccessObjects`, `Repositories`, `Services`, `QuizManagement` |
| Database script | `[x]` | `CreateDB.sql` đã có các bảng chính và `TestResultDetails` |
| Entities / DbContext | `[x]` | Đã scaffold models và `QuizManagementDbContext` |
| DAO layer | `[x]` | Đã thêm `SubjectDAO`, `DeckDAO`, `QuestionDAO` theo mẫu `DAO.Instance` |
| Repository / Service | `[x]` | Repository gọi DAO, Service xử lý nghiệp vụ/validation |
| MVC controllers/views | `[x]` | Đã có Account, Subject, Deck, Question controllers/views |
| Authentication | `[x]` | Đã có register/login/logout bằng custom cookie authentication |
| CRUD nội dung học tập | `[x]` | Đã có controller/views cho Subject, Deck, Question/Answer |
| README/setup | `[x]` | README đã được chuẩn hóa, có hướng dẫn local config |
| Git/local hygiene | `[x]` | `.gitignore` bỏ qua `Error.md`, `PRN222.md`, `TASK_PROGRESS.md`, `bin/`, `obj/`, `.vs/` |
| Quiz engine | `[ ]` | Chưa có tạo bài, shuffle, chấm điểm, lưu lịch sử |
| UI final | `[ ]` | Chưa có dark theme/glassmorphism theo kế hoạch |

## 3. Thứ Tự Ưu Tiên MVP

1. Chạy được app với database local và connection string đúng. `[x]`
2. Đăng ký, đăng nhập, đăng xuất bằng cookie authentication. `[x]`
3. CRUD Subject và Deck theo đúng user đang đăng nhập. `[x]`
4. CRUD Question và Answer, có validate single choice / multiple choice. `[x]`
5. Tạo bài quiz theo Deck, shuffle câu hỏi và đáp án. `[ ]`
6. Submit bài làm, chấm điểm, lưu `TestHistories` và `TestResultDetails`. `[ ]`
7. Xem lịch sử làm bài và xem lại chi tiết kết quả. `[ ]`
8. Hoàn thiện UI, validation message, responsive. `[ ]`
9. Làm bonus import/Markdown/thống kê nếu còn thời gian. `[B]`

## 4. Phase 0 - Nền Tảng Và Vệ Sinh Project

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Kiểm tra solution build được | `[x]` | BE | `dotnet build --no-restore` thành công, 0 warning, 0 error |
| Kiểm tra connection string | `[x]` | BE | `appsettings.json` dùng placeholder, hỗ trợ `appsettings.Local.json` cho local secret |
| Bỏ hard-code connection string trong `OnConfiguring` | `[x]` | BE | DbContext chỉ dùng DI/config từ `Program.cs` |
| Kiểm tra `.gitignore` cho `bin/`, `obj/`, `.vs/` | `[x]` | All | Đã thêm `.gitignore` để bỏ qua build output và file IDE |
| Ignore local notes | `[x]` | All | `Error.md`, `PRN222.md`, `TASK_PROGRESS.md` không hiện trong Git status |
| Chuẩn hóa README / tài liệu setup | `[x]` | Leader | README mô tả setup, kiến trúc, database, roadmap, local config |
| Tạo sample data để demo | `[ ]` | BE | Có user, subject, deck, question, answer mẫu |

## 5. Phase 1 - Database Và EF Core

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Đồng bộ `CreateDB.sql` với entities | `[x]` | BE | Các bảng chính đã có trong script và model |
| Thêm `QuestionType` vào tài liệu thiết kế | `[ ]` | Leader | `PRN222.md` ghi rõ `1 = SingleChoice`, `2 = MultipleChoice` |
| Thêm global query filter cho soft delete | `[x]` | BE | `Subject`, `Deck`, `Question` mặc định chỉ lấy `IsDeleted = false` |
| Đổi delete vật lý sang soft delete | `[x]` | BE | Đã làm cho `Subject`, `Deck`, `Question` |
| Xử lý soft delete theo cấp | `[x]` | BE | Xóa Subject ẩn Deck/Question liên quan; xóa Deck ẩn Question liên quan |
| Thêm audit fields khi create/update | `[x]` | BE | Đã gán audit fields cho entity chính có audit fields |
| Thêm index/constraint cần thiết | `[x]` | BE | `CreateDB.sql` có filtered unique index cho Subject theo user |

## 6. Phase 2 - Authentication Và Authorization

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Chốt cách auth | `[x]` | Team | Đã chọn custom cookie authentication theo bảng `Users` hiện có |
| Tạo `AccountController` | `[x]` | BE | Có actions Register, Login, Logout |
| Tạo ViewModels cho auth | `[x]` | BE | Có validation cho username/email/password |
| Hash password bằng `PasswordHasher` | `[x]` | BE | Không lưu plain text password |
| Cấu hình Cookie Authentication trong `Program.cs` | `[x]` | BE | `UseAuthentication()` đặt trước `UseAuthorization()` |
| Gán claim UserId/Role khi login | `[x]` | BE | Cookie có UserId, Username, Email, Role |
| Thêm `[Authorize]` cho màn hình cần đăng nhập | `[x]` | BE | `HomeController` yêu cầu đăng nhập, Account Login/Register cho phép anonymous |
| Phân quyền User/Admin cơ bản | `[~]` | BE | Đã có Role claim; sẽ enforce thêm khi có màn hình Admin/User cụ thể |

## 7. Phase 3 - CRUD Nội Dung Học Tập

### Subject

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo `SubjectDAO` | `[x]` | BE | DAO xử lý truy vấn EF và soft delete |
| Hoàn thiện `ISubjectRepository` / `SubjectRepository` | `[x]` | BE | Repository gọi `SubjectDAO.Instance`, có filter theo user |
| Hoàn thiện `ISubjectService` / `SubjectService` | `[x]` | BE | Có validation trùng tên và soft delete |
| Tạo `SubjectsController` | `[x]` | BE | List/Create/Edit/Delete hoạt động |
| Tạo views Subject | `[x]` | FE | Giao diện quản lý môn học dùng layout chung |

### Deck

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo `DeckDAO` | `[x]` | BE | DAO xử lý truy vấn EF và soft delete theo deck |
| Tạo repository/service cho Deck | `[x]` | BE | CRUD theo `SubjectId` và user hiện tại |
| Tạo `DecksController` | `[x]` | BE | List/Create/Edit/Delete hoạt động |
| Tạo views Deck | `[x]` | FE | Xem được các bộ đề trong một Subject |
| Chặn truy cập Deck không thuộc user | `[x]` | BE | User không xem/sửa/xóa dữ liệu của người khác |

### Question Và Answer

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo `QuestionDAO` | `[x]` | BE | DAO xử lý Question và Answer cùng lúc |
| Tạo repository/service cho Question | `[x]` | BE | CRUD question theo `DeckId` |
| Tạo repository/service cho Answer | `[x]` | BE | Thêm/sửa/xóa đáp án trong question qua `QuestionDAO` |
| Tạo `QuestionsController` | `[x]` | BE | Create/Edit gồm cả answers |
| Tạo ViewModels cho question form | `[x]` | BE | Form không bind trực tiếp entity quá mức cần thiết |
| Validate single choice | `[x]` | BE | Bắt buộc đúng 1 đáp án |
| Validate multiple choice | `[x]` | BE | Bắt buộc có ít nhất 1 đáp án đúng |
| Validate số đáp án tối thiểu | `[x]` | BE | Mỗi câu hỏi có ít nhất 2 đáp án |
| Tạo views Question/Answer | `[x]` | FE | Tạo câu hỏi và đánh dấu đáp án đúng dễ dùng |

## 8. Phase 4 - Quiz Engine

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo `QuizDAO` / query hỗ trợ quiz | `[ ]` | BE | Lấy câu hỏi theo Deck kèm Answers đúng scope user |
| Tạo `QuizService` | `[ ]` | BE | Lấy câu hỏi theo Deck và số lượng câu hỏi |
| Tạo ViewModels cho quiz | `[ ]` | BE | Không gửi `IsCorrect` ra client khi làm bài |
| Tạo màn hình cấu hình bài test | `[ ]` | FE/BE | Chọn số lượng câu hỏi, validate min/max |
| Xáo trộn câu hỏi bằng Fisher-Yates | `[ ]` | BE | Thứ tự câu hỏi thay đổi mỗi lần làm |
| Xáo trộn đáp án bằng Fisher-Yates | `[ ]` | BE | Thứ tự đáp án thay đổi mỗi lần làm |
| Tạo `QuizController` | `[ ]` | BE | Có Config/Start, Submit, Result |
| Giao diện làm bài quiz | `[ ]` | FE | Chọn single/multiple answer rõ ràng, responsive |
| Cảnh báo câu chưa trả lời khi submit | `[ ]` | FE | Người dùng được xác nhận trước khi nộp bài chưa hoàn tất |
| Chấm điểm single choice | `[ ]` | BE | Chọn đúng đáp án thì được điểm |
| Chấm điểm multiple choice | `[ ]` | BE | Đúng khi tập đáp án chọn trùng khớp tập đáp án đúng |
| Lưu `TestHistories` | `[ ]` | BE | Lưu score, percentage, user, deck, created time |
| Lưu `TestResultDetails` | `[ ]` | BE | Lưu từng câu hỏi, đáp án đã chọn, đúng/sai |
| Hiển thị result sau submit | `[ ]` | FE/BE | Có điểm, tỷ lệ, đáp án đúng, explanation |

## 9. Phase 5 - History Và Dashboard

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo trang Dashboard | `[ ]` | FE/BE | Hiện subject/deck gần đây và thống kê cơ bản |
| Tạo trang lịch sử làm bài | `[ ]` | FE/BE | Liệt kê các lần làm bài của user |
| Tạo trang chi tiết lịch sử | `[ ]` | FE/BE | Xem lại câu hỏi, đáp án đã chọn, đáp án đúng |
| Tính thống kê cơ bản | `[ ]` | BE | Số bài đã làm, điểm trung bình, lần làm gần nhất |
| Chặn xem history của user khác | `[ ]` | BE | Mỗi user chỉ xem lịch sử của mình |

## 10. Phase 6 - UI/UX Final

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Chốt Bootstrap hay Tailwind | `[x]` | Team | Đang dùng Bootstrap theo template MVC |
| Thiết kế `_Layout.cshtml` | `[~]` | FE | Navbar/login state đã có; cần polish UI sau |
| Tạo dark theme | `[ ]` | FE | Màu sắc dễ đọc, không lỗi contrast |
| Áp dụng glassmorphism có tiết chế | `[ ]` | FE | Card/panel đẹp nhưng không làm rối mắt |
| Tạo form style dùng chung | `[~]` | FE | Form CRUD dùng Bootstrap, cần polish thêm |
| Tạo table/list style dùng chung | `[~]` | FE | List/table CRUD dùng Bootstrap, cần polish thêm |
| Thêm toast/alert | `[~]` | FE | Đã có alert cơ bản qua `TempData` |
| Kiểm tra responsive | `[ ]` | FE | Desktop, tablet, mobile không vỡ layout |
| Kiểm tra empty states | `[x]` | FE | Subject/Deck/Question có empty state cơ bản |

## 11. Phase 7 - Bonus Sau MVP

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| `[B]` Import câu hỏi từ Excel | `[ ]` | BE | Đọc file, validate, preview trước khi import |
| `[B]` Import câu hỏi từ text | `[ ]` | BE | Có format mẫu và báo lỗi dòng sai |
| `[B]` Markdown cho nội dung câu hỏi | `[ ]` | BE/FE | Render an toàn, tránh XSS |
| `[B]` Export deck ra PDF/Word | `[ ]` | BE | Tạo file tải về được |
| `[B]` Thống kê điểm nâng cao | `[ ]` | BE/FE | Biểu đồ tiến trình học tập |
| `[B]` Flashcard / spaced repetition | `[ ]` | BE/FE | Chỉ làm nếu MVP đã ổn định |

## 12. Phase 8 - Bảo Mật Và Chất Lượng

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Bật Anti-Forgery cho form POST | `[x]` | BE | Các POST hiện có dùng `[ValidateAntiForgeryToken]` |
| Validate input server-side | `[x]` | BE | Auth và CRUD hiện có validate server-side bằng ViewModel/ModelState |
| Kiểm tra ownership mọi query | `[x]` | BE | CRUD Subject/Deck/Question lọc theo user đang đăng nhập |
| Không gửi đáp án đúng ra client trước submit | `[ ]` | BE | Sẽ thực hiện ở Phase 4 Quiz engine |
| Xử lý lỗi thân thiện | `[~]` | BE/FE | Có validation/NotFound cơ bản, cần error UX tốt hơn |
| Kiểm tra N+1 query | `[~]` | BE | Có `Include` ở DAO; sẽ tối ưu thêm khi làm Quiz/History |
| Test flow chính bằng tay | `[~]` | All | User đã test register/login; cần test CRUD end-to-end |
| Build final không warning nghiêm trọng | `[x]` | All | `dotnet build --no-restore` sạch lỗi tại thời điểm cập nhật |

## 13. Phase 9 - Demo Và Nộp Bài

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo database demo sạch | `[ ]` | Leader | Script chạy lại từ đầu được |
| Tạo tài khoản demo User/Admin | `[ ]` | Leader | Đăng nhập demo nhanh |
| Chuẩn bị dữ liệu demo | `[ ]` | Team | Có ít nhất 2 subjects, 3 decks, 20+ questions |
| Cập nhật `PRN222.md` theo code thật | `[ ]` | Leader | Tài liệu không ghi feature chưa làm như đã xong |
| Cập nhật README setup | `[x]` | Leader | README đã được chuẩn hóa |
| Chuẩn bị slide/demo script | `[ ]` | Team | Nói được kiến trúc, DB, luồng quiz, điểm kỹ thuật |
| Quay/chụp ảnh màn hình nếu cần | `[ ]` | Team | Có bằng chứng tính năng chính |
| Tag/release bản nộp | `[ ]` | Leader | Code final rõ ràng, không lẫn file tạm |

## 14. Definition Of Done Cho Mỗi Feature

Một feature chỉ được tick `[x]` khi đạt đủ các điều kiện sau:

- Controller/action hoạt động đúng flow.
- Service chứa logic nghiệp vụ, controller không xử lý quá nhiều logic.
- Repository/DAO/DbContext query đúng, không truy cập data của user khác.
- View hiển thị được success/error/empty state.
- Có validation server-side cho input quan trọng.
- Đã test bằng tay ít nhất một lần với dữ liệu thật.
- Không làm hỏng flow đã có.

## 15. Nhật Ký Tiến Độ

| Ngày | Người làm | Việc đã làm | Việc tiếp theo | Ghi chú |
| --- | --- | --- | --- | --- |
| 2026-06-14 | Team | Tạo tracker tiến độ ban đầu | Build check, chốt authentication, làm auth | Repo hiện có nền tảng N-Tier và entities |
| 2026-06-14 | Codex | Phase 0: build thành công, thêm `.gitignore`, bỏ hard-code connection string trong DbContext | Điền connection string local, chuẩn hóa README, tạo sample data | Build cần restore NuGet lần đầu |
| 2026-06-14 | Codex | Phase 1: thêm global query filter cho soft delete, đổi delete sang soft delete | Làm auth để có user context, sau đó hoàn thiện audit/ownership | Build sau chỉnh sửa thành công |
| 2026-06-14 | Codex | Phase 2: thêm custom cookie auth, register/login/logout, password hashing, role claims, auth views | Bước sau là Phase 3 CRUD Subject/Deck | User đã test đăng ký/đăng nhập được |
| 2026-06-14 | Codex | Phase 3: thêm DAO layer theo mẫu `DAO.Instance`, CRUD Subject/Deck/Question/Answer, views và ownership checks | Test thực tế CRUD trên trình duyệt, sau đó sang Phase 4 Quiz engine | Build thành công; `/Subjects` redirect về Login khi chưa đăng nhập |
| 2026-06-14 | Codex | Chuẩn bị push Git: chuẩn hóa README, thêm `.gitignore` cho local notes, bỏ password thật khỏi `appsettings.json`, thêm `appsettings.Local.example.json` | Tạo `appsettings.Local.json` trên máy local nếu cần chạy bằng password thật | Build thành công, 0 warning, 0 error |
