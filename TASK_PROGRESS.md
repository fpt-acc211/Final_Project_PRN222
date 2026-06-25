# Task Progress - Quiz Management System

Cập nhật gần nhất: 2026-06-25

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
| Git/local hygiene | `[x]` | `.gitignore` bỏ qua `Error.md`, `PRN222.md`, `bin/`, `obj/`, `.vs/`; `TASK_PROGRESS.md` được commit để team theo dõi |
| Quiz engine | `[x]` | Đã có tạo bài, shuffle Fisher-Yates, chấm điểm single/multiple choice, lưu lịch sử |
| UI final | `[x]` | Dark Theme + Glassmorphism hoàn tất; readability cải thiện đạt WCAG AA |
| Admin panel | `[x]` | Admin/Index, Admin/Users, phân quyền Policy `ManageUsers` |
| Search client-side | `[x]` | Search box JS real-time cho Subjects/Decks, không reload trang |
| Timer quiz | `[x]` | Mentor đặt thời gian trên Deck; countdown tự nộp bài khi hết giờ |
| Leaderboard | `[x]` | Top 20 theo bộ đề, huy chương vàng/bạc/đồng |
| Thống kê Mentor | `[x]` | Stat-card + bảng theo môn/bộ đề; Mentor chỉ thấy dữ liệu sở hữu |
| Báo cáo câu hỏi | `[x]` | Bảng `QuestionReports`; user báo từ Result; Mentor/Admin xử lý |
| LoginAttempts log | `[x]` | Ghi DB mọi lần đăng nhập; Admin xem và lọc theo kết quả |

## 3. Thứ Tự Ưu Tiên MVP

1. Chạy được app với database local và connection string đúng. `[x]`
2. Đăng ký, đăng nhập, đăng xuất bằng cookie authentication. `[x]`
3. CRUD Subject và Deck theo đúng user đang đăng nhập. `[x]`
4. CRUD Question và Answer, có validate single choice / multiple choice. `[x]`
5. Tạo bài quiz theo Deck, shuffle câu hỏi và đáp án. `[x]`
6. Submit bài làm, chấm điểm, lưu `TestHistories` và `TestResultDetails`. `[x]`
7. Xem lịch sử làm bài và xem lại chi tiết kết quả. `[x]`
8. Hoàn thiện UI, validation message, responsive. `[x]`
9. Làm bonus import/Markdown/thống kê nếu còn thời gian. `[x]`

## 4. Phase 0 - Nền Tảng Và Vệ Sinh Project

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Kiểm tra solution build được | `[x]` | BE | `dotnet build --no-restore` thành công, 0 warning, 0 error |
| Kiểm tra connection string | `[x]` | BE | `appsettings.json` dùng placeholder, hỗ trợ `appsettings.Local.json` cho local secret |
| Bỏ hard-code connection string trong `OnConfiguring` | `[x]` | BE | DbContext chỉ dùng DI/config từ `Program.cs` |
| Kiểm tra `.gitignore` cho `bin/`, `obj/`, `.vs/` | `[x]` | All | Đã thêm `.gitignore` để bỏ qua build output và file IDE |
| Ignore local notes | `[x]` | All | `Error.md`, `PRN222.md` không hiện trong Git status; `TASK_PROGRESS.md` được theo dõi trong Git |
| Chuẩn hóa README / tài liệu setup | `[x]` | Leader | README mô tả setup, kiến trúc, database, roadmap, local config |
| Tạo sample data để demo | `[x]` | BE | `SeedDemoData.sql` có Admin/Mentor/User, 2 subjects, 3 decks, 21 questions |

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
| Phân quyền Admin/Mentor/User | `[x]` | BE | Admin toàn quyền; Mentor CRUD học liệu sở hữu; User chỉ xem/học/quiz; `StudyContent` tách khỏi `ManageContent` |

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
| Ownership và quyền học Deck | `[x]` | BE | Mọi role được học Deck đang hoạt động; Mentor chỉ sửa/xóa Deck sở hữu; Admin được quản lý tất cả |

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
| Tạo `QuizDAO` / query hỗ trợ quiz | `[x]` | BE | Lấy câu hỏi theo Deck kèm Answers đúng scope user |
| Tạo `QuizService` | `[x]` | BE | Lấy câu hỏi theo Deck và số lượng câu hỏi |
| Tạo ViewModels cho quiz | `[x]` | BE | Không gửi `IsCorrect` ra client khi làm bài |
| Tạo màn hình cấu hình bài test | `[x]` | FE/BE | Chọn số lượng câu hỏi, validate min/max |
| Xáo trộn câu hỏi bằng Fisher-Yates | `[x]` | BE | Thứ tự câu hỏi thay đổi mỗi lần làm |
| Xáo trộn đáp án bằng Fisher-Yates | `[x]` | BE | Thứ tự đáp án thay đổi mỗi lần làm |
| Tạo `QuizController` | `[x]` | BE | Có Config/Start, Submit, Result |
| Giao diện làm bài quiz | `[x]` | FE | Chọn single/multiple answer rõ ràng, responsive |
| Cảnh báo câu chưa trả lời khi submit | `[x]` | FE | Người dùng được xác nhận trước khi nộp bài chưa hoàn tất |
| Chấm điểm single choice | `[x]` | BE | Chọn đúng đáp án thì được điểm |
| Chấm điểm multiple choice | `[x]` | BE | Đúng khi tập đáp án chọn trùng khớp tập đáp án đúng |
| Lưu `TestHistories` | `[x]` | BE | Lưu score, percentage, user, deck, created time |
| Lưu `TestResultDetails` | `[x]` | BE | Lưu từng câu hỏi, đáp án đã chọn, đúng/sai |
| Hiển thị result sau submit | `[x]` | FE/BE | Có điểm, tỷ lệ, đáp án đúng, explanation |

## 9. Phase 5 - History Và Dashboard

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo trang Dashboard | `[x]` | FE/BE | Hiện subject/deck gần đây và thống kê cơ bản |
| Tạo trang lịch sử làm bài | `[x]` | FE/BE | Liệt kê các lần làm bài của user |
| Tạo trang chi tiết lịch sử | `[x]` | FE/BE | Xem lại câu hỏi, đáp án đã chọn, đáp án đúng |
| Tính thống kê cơ bản | `[x]` | BE | Số bài đã làm, điểm trung bình, lần làm gần nhất |
| Chặn xem history của user khác | `[x]` | BE | Mỗi user chỉ xem lịch sử của mình |

## 10. Phase 6 - UI/UX Final

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Chốt Bootstrap hay Tailwind | `[x]` | Team | Bootstrap 5 dark mode + CSS custom properties |
| Thiết kế `_Layout.cshtml` | `[x]` | FE | Glassmorphism navbar, Google Fonts (Inter), nav-avatar badge, page-content wrapper |
| Tạo dark theme | `[x]` | FE | CSS token system trong `site.css`; tất cả view dùng `--bg`, `--surface`, `--text`, `--accent` |
| Áp dụng glassmorphism có tiết chế | `[x]` | FE | Card dùng `backdrop-filter: blur(12px)` với fallback; không làm chậm trên thiết bị cũ |
| Redesign Login / Register | `[x]` | FE | `auth-page` layout fullscreen, `auth-brand`, `auth-card`; fix lỗi frame do CSS stacking context |
| Redesign Home/Dashboard | `[x]` | FE | `stat-card--purple/cyan/green`, bảng lịch sử gần đây, danh sách môn học |
| Redesign Subjects/Index | `[x]` | FE | `item-grid` thay bảng, `item-card__actions` |
| Redesign Decks/Index | `[x]` | FE | `item-grid`, breadcrumb với accent link |
| Redesign Quiz/Take | `[x]` | FE | `quiz-sticky-header`, progress bar, `answer-option` checkbox/radio, visual feedback câu đã trả lời |
| Redesign Quiz/Result | `[x]` | FE | `score-ring` pass/fail, `result-card--correct/wrong`, hiển thị đáp án đã chọn/đáp án đúng |
| Redesign Admin/Index | `[x]` | FE | `admin-stat` cards với SVG icon màu `icon-purple/cyan/green/yellow/red` |
| Redesign Admin/Users | `[x]` | FE | `u-avatar` component, bảng người dùng cải tiến, badge vai trò |
| Redesign Statistics/Index | `[x]` | FE | `stat-card` components, chart Y-axis labels hiển thị rõ |
| Redesign Profile/Index | `[x]` | FE | Avatar fallback dùng accent color thay `bg-secondary`; fix chữ initials bị mờ |
| Cải thiện readability (contrast) | `[x]` | FE | `--text-muted: #a8b8cc`, `--text-dim: #6b7e94`; chart label, dropdown, warning badge, placeholder đều đạt WCAG AA |
| Tạo form style dùng chung | `[x]` | FE | Form auth và CRUD dùng token CSS, glass card style |
| Tạo table/list style dùng chung | `[x]` | FE | Table dark, item-grid, breadcrumb đồng bộ toàn app |
| Thêm toast/alert | `[x]` | FE | TempData alert có thể dismiss; màu sắc theo trạng thái |
| Kiểm tra responsive | `[~]` | FE | Desktop hoạt động tốt; tablet/mobile cơ bản ổn, chưa kiểm tra đầy đủ |
| Kiểm tra empty states | `[x]` | FE | Tất cả list/table có empty state với text muted rõ ràng |

## 10b. Phase 6b - Admin Panel

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo `AdminController` | `[x]` | BE | Yêu cầu Policy `ManageUsers`; action Index + Users/UserDetail/ChangeRole/ToggleDisabled |
| Dashboard Admin | `[x]` | BE/FE | Hiện tổng user, môn học, bộ đề, câu hỏi, lần làm bài |
| Quản lý người dùng | `[x]` | BE/FE | Xem danh sách user; admin có thể thay đổi Role |
| Khóa / mở khóa tài khoản | `[x]` | BE | Đã có `IsDisabled`, bảo vệ Admin cuối cùng và `SecurityStamp`-based session invalidation |
| Tạo tài khoản Admin đầu tiên | `[x]` | BE | Seed opt-in qua `AdminSeed` trong local config; không hard-code hoặc log password |

## 11. Phase 7 - Bonus Sau MVP

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| `[B]` Import câu hỏi từ Excel | `[x]` | BE | Đọc `.xlsx`, validate, preview trước khi import |
| `[B]` Import câu hỏi từ text | `[x]` | BE | Có format mẫu và báo lỗi dòng sai |
| `[B]` Markdown cho nội dung câu hỏi | `[x]` | BE/FE | Render an toàn bằng whitelist HTML sau khi encode input |
| `[B]` Export deck ra PDF/Word | `[x]` | BE | Tạo file `.docx` và `.pdf` tải về được |
| `[B]` Thống kê điểm nâng cao | `[x]` | BE/FE | Có dashboard thống kê, biểu đồ 12 lần làm gần nhất, group theo môn/bộ đề |
| `[B]` Flashcard / spaced repetition | `[x]` | BE/FE | Có màn hình flashcard học theo deck với lịch ôn trong phiên |

## 12. Phase 8 - Bảo Mật Và Chất Lượng

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Bật Anti-Forgery cho form POST | `[x]` | BE | Các POST hiện có dùng `[ValidateAntiForgeryToken]` |
| Validate input server-side | `[x]` | BE | Auth và CRUD hiện có validate server-side bằng ViewModel/ModelState |
| Kiểm tra ownership mọi query | `[x]` | BE | CRUD Subject/Deck/Question lọc theo user đang đăng nhập |
| Không gửi đáp án đúng ra client trước submit | `[x]` | BE | QuizTakeViewModel không chứa IsCorrect |
| Không tin dữ liệu quiz nhạy cảm từ client | `[x]` | BE | Submit dùng Data Protection token để xác thực deck/user/danh sách câu hỏi; `QuestionType` lấy lại từ DB |
| Xử lý lỗi thân thiện | `[~]` | BE/FE | Có validation/NotFound cơ bản, cần error UX tốt hơn |
| Kiểm tra N+1 query | `[~]` | BE | Có `Include` ở DAO; sẽ tối ưu thêm khi làm Quiz/History |
| Test flow chính bằng tay | `[~]` | All | Đã có E2E LocalDB trước đó; nên chạy lại sau seed/final-readiness trước khi tag release |
| Build final không warning nghiêm trọng | `[x]` | All | `dotnet build --no-restore` sạch lỗi tại thời điểm cập nhật |

## 13. Phase 9 - Demo Và Nộp Bài

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Tạo database demo sạch | `[x]` | Leader | `CreateDB.sql` + `SeedDemoData.sql` chạy lại được |
| Tạo tài khoản demo User/Admin | `[x]` | Leader | Có `admin_demo`, `mentor_demo`, `user_demo`; password demo `Test@123456` |
| Chuẩn bị dữ liệu demo | `[x]` | Team | Có 2 subjects, 3 decks, 21 questions và 1 lịch sử làm bài mẫu |
| Cập nhật `PRN222.md` theo code thật | `[ ]` | Leader | Tài liệu không ghi feature chưa làm như đã xong |
| Cập nhật README setup | `[x]` | Leader | README đã được chuẩn hóa |
| Chuẩn bị slide/demo script | `[ ]` | Team | Nói được kiến trúc, DB, luồng quiz, điểm kỹ thuật |
| Quay/chụp ảnh màn hình nếu cần | `[ ]` | Team | Có bằng chứng tính năng chính |
| Tag/release bản nộp | `[ ]` | Leader | Code final rõ ràng, không lẫn file tạm |

## 14. Phase 10 - Tính Năng Bổ Sung (2026-06-24)

| Task | Trạng thái | Owner | Tiêu chí hoàn thành |
| --- | --- | --- | --- |
| Search box client-side cho Subjects | `[x]` | FE | JS filter real-time trên `item-card` không reload trang |
| Search box client-side cho Decks | `[x]` | FE | JS filter trên danh sách deck, có empty state khi không có kết quả |
| Mentor đặt giới hạn thời gian trên Deck | `[x]` | BE/FE | Thêm `TimeLimitMinutes` vào `Decks` (ALTER TABLE), Mentor chọn qua dropdown trong Create/Edit |
| Countdown timer khi làm bài | `[x]` | FE | Timer hiển thị trên header, chuyển đỏ khi còn 60s, tự nộp bài khi về 0 |
| Trang cấu hình quiz chỉ hiển thị thời gian | `[x]` | FE | `Config.cshtml` chỉ hiện badge thời gian (không cho user chỉnh); đọc từ `deck.TimeLimitMinutes` |
| Leaderboard theo bộ đề | `[x]` | BE/FE | `/Statistics/Leaderboard?deckId=X` — top 20 theo điểm cao nhất, huy chương vàng/bạc/đồng |
| Thống kê nội dung cho Mentor | `[x]` | BE/FE | `/Statistics/MentorStats` — 4 stat-card, bảng theo môn và theo bộ đề; Mentor chỉ thấy dữ liệu sở hữu |
| Nút bảng xếp hạng trên trang Decks | `[x]` | FE | Nút "Bảng XH" trên mỗi deck card liên kết vào Leaderboard |
| Link nav Thống kê / Báo cáo lỗi cho Mentor | `[x]` | FE | `_Layout.cshtml` hiện "Thống kê nội dung" và "Báo cáo lỗi" nếu là Mentor hoặc Admin |
| Báo cáo câu hỏi sai | `[x]` | BE/FE | Bảng `QuestionReports` mới; user báo từ trang Result; Mentor/Admin xem và đánh dấu đã xử lý |
| Form báo cáo câu hỏi | `[x]` | FE | Dropdown lý do (WrongAnswer/UnclearQuestion/DuplicateQuestion/Other) + ghi chú tuỳ chọn |
| Trang quản lý báo cáo lỗi | `[x]` | FE | Bảng chờ xử lý, nút "Đã xử lý" và "Sửa câu hỏi", phần báo cáo đã giải quyết thu gọn |
| Ghi log lần đăng nhập vào DB | `[x]` | BE | Bảng `LoginAttempts` mới; `AccountController` ghi cả login thành công/thất bại |
| Trang Admin xem lịch sử đăng nhập | `[x]` | BE/FE | `/Admin/LoginAttempts` với filter Tất cả/Thất bại/Thành công, hiện Email/IP/Kết quả/Thời gian |
| Flashcard 3D flip animation | `[x]` | FE | Teammate PR — animation lật thẻ 3D khi học flashcard |

## 15. Definition Of Done Cho Mỗi Feature

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
| 2026-06-14 | Naams2k10fpt | Tạo tracker tiến độ ban đầu | Build check, chốt authentication, làm auth | Repo hiện có nền tảng N-Tier và entities |
| 2026-06-14 | Naams2k10fpt | Phase 0: build thành công, thêm `.gitignore`, bỏ hard-code connection string trong DbContext | Điền connection string local, chuẩn hóa README, tạo sample data | Build cần restore NuGet lần đầu |
| 2026-06-14 | Naams2k10fpt | Phase 1: thêm global query filter cho soft delete, đổi delete sang soft delete | Làm auth để có user context, sau đó hoàn thiện audit/ownership | Build sau chỉnh sửa thành công |
| 2026-06-14 | Naams2k10fpt | Phase 2: thêm custom cookie auth, register/login/logout, password hashing, role claims, auth views | Bước sau là Phase 3 CRUD Subject/Deck | User đã test đăng ký/đăng nhập được |
| 2026-06-14 | Naams2k10fpt | Phase 3: thêm DAO layer theo mẫu `DAO.Instance`, CRUD Subject/Deck/Question/Answer, views và ownership checks | Test thực tế CRUD trên trình duyệt, sau đó sang Phase 4 Quiz engine | Build thành công; `/Subjects` redirect về Login khi chưa đăng nhập |
| 2026-06-14 | Naams2k10fpt | Chuẩn bị push Git: chuẩn hóa README, thêm `.gitignore` cho local notes, bỏ password thật khỏi `appsettings.json`, thêm `appsettings.Local.example.json` | Tạo `appsettings.Local.json` trên máy local nếu cần chạy bằng password thật | Build thành công, 0 warning, 0 error |
| 2026-06-15 | NguyenNgu2005 | Phase 4: Quiz Engine hoàn chỉnh - QuizDAO, QuizService (Fisher-Yates shuffle, chấm điểm single/multi choice), QuizController (Config/Take/Submit/Result), ViewModels (không lộ IsCorrect), Views (radio/checkbox, cảnh báo câu chưa trả lời) | Test flow quiz trên trình duyệt | Build thành công, 0 warning, 0 error |
| 2026-06-15 | NguyenNgu2005 | Phase 5: Dashboard + History - trang Dashboard với thống kê (số bài, điểm TB, lần làm gần nhất), trang lịch sử làm bài, chi tiết xem lại bài đã làm, ownership check | Test dashboard và history trên trình duyệt | Build thành công, 0 warning, 0 error |
| 2026-06-15 | NguyenNgu2005 | Phase 7: bonus sau MVP - import Excel/text có preview, Markdown an toàn, export Word/PDF, thống kê nâng cao, flashcard theo deck | Test lại flow import/export/quiz trên trình duyệt | Build thành công, 0 warning, 0 error |
| 2026-06-21 | locphan8541 | Phase 6 UI/UX hoàn tất - redesign toàn bộ 12 view với Dark Theme + Glassmorphism: Layout, Login, Register, Home, Subjects, Decks, Quiz/Take, Quiz/Result, Admin, Statistics, Profile | Kiểm tra responsive trên mobile, chuẩn bị demo data | 54 files thay đổi, +3929/-643 lines; push lên branch `feature/phase2-redesign` |
| 2026-06-21 | locphan8541 | Fix lỗi auth page frame (CSS stacking context từ `transform` trên animation), fix avatar Profile, cải thiện readability toàn app (WCAG AA contrast tokens) | Kiểm tra toàn bộ màn hình trên nhiều độ phân giải | Đã xác nhận chạy trên http://localhost:5039 |
| 2026-06-22 | Naams2k10fpt | Stabilize main, hoàn thiện RBAC Admin/Mentor/User, gộp schema Phase 2, sửa đáp án trống và footer đè form | Review diff rồi commit/push PR | E2E LocalDB đạt: register/login, đổi role, ownership, CRUD, import, export Word/PDF, flashcard, quiz 10/10, history/statistics, khóa tài khoản và soft delete; build 0 warning, 0 error; chưa commit |
| 2026-06-23 | Naams2k10fpt | Final readiness: harden quiz submit bằng signed attempt token, thêm `SeedDemoData.sql`, chuẩn hóa config mẫu và tài liệu | Chạy lại demo flow trên browser, kiểm tra responsive, chuẩn bị slide/tag release | Build cần kiểm tra lại sau khi hoàn tất chỉnh tài liệu |
| 2026-06-24 | locphan8541 | Phase 10: thêm search box JS real-time cho Subjects và Decks; Mentor đặt `TimeLimitMinutes` trên Deck; countdown timer tự nộp bài; Leaderboard theo deck; Thống kê nội dung Mentor | Kiểm tra leaderboard và mentor stats trên trình duyệt | ALTER TABLE thêm `TimeLimitMinutes INT NOT NULL DEFAULT 0` cho Decks |
| 2026-06-24 | locphan8541 | Phase 10 (tiếp): Báo cáo câu hỏi sai (bảng `QuestionReports`, nút báo cáo trên Result, trang quản lý cho Mentor/Admin); ghi log `LoginAttempts` vào DB; trang Admin xem lịch sử đăng nhập; pull flashcard 3D flip từ teammate | Cập nhật TASK_PROGRESS.md và push lên branch mới | Build thành công 0 warning 0 error; app chạy tại http://localhost:5039 |
| 2026-06-25 | Antigravity | Cải tiến học Flashcard: track trạng thái từng thẻ không vượt tổng thẻ, Enter đánh dấu đã nhớ, R cần học lại, thêm bảng hướng dẫn phím tắt, hiệu ứng lướt flashcard (swipe right = đã nhớ, swipe left = học lại); Sửa UX chọn câu trả lời quiz (toàn bộ ô đáp án đều click được). | Phát triển các tính năng tiếp theo (Match game, Learn mode, Timer update). | Đẩy lên nhánh `feature/flashcard-improvements-and-quiz-ux` |
