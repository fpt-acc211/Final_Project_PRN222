USE [QuizManagementDB];
GO

SET XACT_ABORT ON;

IF OBJECT_ID(N'tempdb..#QuestionMap') IS NOT NULL DROP TABLE #QuestionMap;
IF OBJECT_ID(N'tempdb..#DeckMap') IS NOT NULL DROP TABLE #DeckMap;
IF OBJECT_ID(N'tempdb..#SeedUserIds') IS NOT NULL DROP TABLE #SeedUserIds;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @SeedBy NVARCHAR(256) = N'Naams2k10fpt';
    DECLARE @AdminId NVARCHAR(450) = N'seed-admin-001';
    DECLARE @MentorId NVARCHAR(450) = N'seed-mentor-001';
    DECLARE @UserId NVARCHAR(450) = N'seed-user-001';

    CREATE TABLE #SeedUserIds (Id NVARCHAR(450) NOT NULL PRIMARY KEY);
    INSERT INTO #SeedUserIds (Id)
    VALUES (@AdminId), (@MentorId), (@UserId);

    ---------------------------------------------------------------------------
    -- Cleanup previous demo data. Only data owned by these seed users is reset.
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NOT NULL
    BEGIN
        EXEC(N'
            DELETE FROM dbo.LoginAttempts
            WHERE UserId IN (SELECT Id FROM #SeedUserIds)
               OR Email IN (
                    N''admin.demo@quiz.local'',
                    N''mentor.demo@quiz.local'',
                    N''user.demo@quiz.local''
               );
        ');
    END;

    IF OBJECT_ID(N'dbo.QuestionReports', N'U') IS NOT NULL
    BEGIN
        EXEC(N'
            DELETE qr
            FROM dbo.QuestionReports qr
            WHERE qr.UserId IN (SELECT Id FROM #SeedUserIds)
               OR qr.QuestionId IN (
                    SELECT q.Id
                    FROM dbo.Questions q
                    JOIN dbo.Decks d ON d.Id = q.DeckId
                    JOIN dbo.Subjects s ON s.Id = d.SubjectId
                    WHERE s.UserId IN (SELECT Id FROM #SeedUserIds)
               );
        ');
    END;

    DELETE trd
    FROM dbo.TestResultDetails trd
    JOIN dbo.TestHistories th ON th.Id = trd.TestHistoryId
    WHERE th.UserId IN (SELECT Id FROM #SeedUserIds)
       OR th.DeckId IN (
            SELECT d.Id
            FROM dbo.Decks d
            JOIN dbo.Subjects s ON s.Id = d.SubjectId
            WHERE s.UserId IN (SELECT Id FROM #SeedUserIds)
       );

    DELETE th
    FROM dbo.TestHistories th
    WHERE th.UserId IN (SELECT Id FROM #SeedUserIds)
       OR th.DeckId IN (
            SELECT d.Id
            FROM dbo.Decks d
            JOIN dbo.Subjects s ON s.Id = d.SubjectId
            WHERE s.UserId IN (SELECT Id FROM #SeedUserIds)
       );

    DELETE a
    FROM dbo.Answers a
    JOIN dbo.Questions q ON q.Id = a.QuestionId
    JOIN dbo.Decks d ON d.Id = q.DeckId
    JOIN dbo.Subjects s ON s.Id = d.SubjectId
    WHERE s.UserId IN (SELECT Id FROM #SeedUserIds);

    DELETE q
    FROM dbo.Questions q
    JOIN dbo.Decks d ON d.Id = q.DeckId
    JOIN dbo.Subjects s ON s.Id = d.SubjectId
    WHERE s.UserId IN (SELECT Id FROM #SeedUserIds);

    DELETE d
    FROM dbo.Decks d
    JOIN dbo.Subjects s ON s.Id = d.SubjectId
    WHERE s.UserId IN (SELECT Id FROM #SeedUserIds);

    DELETE FROM dbo.Subjects WHERE UserId IN (SELECT Id FROM #SeedUserIds);
    DELETE FROM dbo.Users WHERE Id IN (SELECT Id FROM #SeedUserIds);

    IF EXISTS (
        SELECT 1
        FROM dbo.Users
        WHERE Email IN (N'admin.demo@quiz.local', N'mentor.demo@quiz.local', N'user.demo@quiz.local')
           OR Username IN (N'admin_demo', N'mentor_demo', N'user_demo')
    )
    BEGIN
        THROW 51000, 'Demo username/email already exists outside seed data. Please rename or remove the duplicate account first.', 1;
    END;

    ---------------------------------------------------------------------------
    -- Demo accounts
    -- Password for all accounts: Test@123456
    ---------------------------------------------------------------------------
    INSERT INTO dbo.Users (Id, Username, Email, PasswordHash, Role)
    VALUES
    (
        @AdminId,
        N'admin_demo',
        N'admin.demo@quiz.local',
        N'AQAAAAIAAYagAAAAEDzzWWoiT7nnykl+8erbwXYh6DYpC9EO7Rnh/DFhzx1R2pdkUBltTQaL3/k60Uq/BQ==',
        N'Admin'
    ),
    (
        @MentorId,
        N'mentor_demo',
        N'mentor.demo@quiz.local',
        N'AQAAAAIAAYagAAAAEB7bK/ID0O9kFUY2PhmAzbPayqyF4PtR+IY1PCeDb8L8dCfu03y+1yDBBY4E4i0ITg==',
        N'Mentor'
    ),
    (
        @UserId,
        N'user_demo',
        N'user.demo@quiz.local',
        N'AQAAAAIAAYagAAAAEIaKi+BX47VrcqNs2tXvpIg6Hv0UNPpeRcrGUh4IxPMwydoTC4eyQo86LyU/gnzGtA==',
        N'User'
    );

    IF COL_LENGTH(N'dbo.Users', N'SecurityStamp') IS NOT NULL
        EXEC(N'UPDATE dbo.Users SET SecurityStamp = CONVERT(NVARCHAR(450), NEWID()) WHERE Id IN (SELECT Id FROM #SeedUserIds);');

    IF COL_LENGTH(N'dbo.Users', N'IsDisabled') IS NOT NULL
        EXEC(N'UPDATE dbo.Users SET IsDisabled = 0 WHERE Id IN (SELECT Id FROM #SeedUserIds);');

    ---------------------------------------------------------------------------
    -- Subjects and decks
    ---------------------------------------------------------------------------
    DECLARE @SubjectCSharpId INT;
    DECLARE @SubjectAspNetId INT;
    DECLARE @DeckCSharpId INT;
    DECLARE @DeckMvcId INT;
    DECLARE @DeckEfId INT;

    INSERT INTO dbo.Subjects (UserId, Name)
    VALUES (@MentorId, N'C# Fundamentals');
    SET @SubjectCSharpId = SCOPE_IDENTITY();

    INSERT INTO dbo.Subjects (UserId, Name)
    VALUES (@MentorId, N'PRN222 - ASP.NET Core MVC');
    SET @SubjectAspNetId = SCOPE_IDENTITY();

    IF COL_LENGTH(N'dbo.Subjects', N'CreatedBy') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Subjects SET CreatedBy = @SeedBy WHERE Id IN (@SubjectCSharpId, @SubjectAspNetId);',
            N'@SeedBy NVARCHAR(256), @SubjectCSharpId INT, @SubjectAspNetId INT',
            @SeedBy = @SeedBy,
            @SubjectCSharpId = @SubjectCSharpId,
            @SubjectAspNetId = @SubjectAspNetId;
    END;

    CREATE TABLE #DeckMap (DeckKey NVARCHAR(50) NOT NULL PRIMARY KEY, Id INT NOT NULL);

    INSERT INTO dbo.Decks (SubjectId, Name)
    VALUES (@SubjectCSharpId, N'C# and OOP Basics');
    SET @DeckCSharpId = SCOPE_IDENTITY();
    INSERT INTO #DeckMap (DeckKey, Id) VALUES (N'csharp', @DeckCSharpId);

    INSERT INTO dbo.Decks (SubjectId, Name)
    VALUES (@SubjectAspNetId, N'ASP.NET Core MVC');
    SET @DeckMvcId = SCOPE_IDENTITY();
    INSERT INTO #DeckMap (DeckKey, Id) VALUES (N'mvc', @DeckMvcId);

    INSERT INTO dbo.Decks (SubjectId, Name)
    VALUES (@SubjectAspNetId, N'Entity Framework Core');
    SET @DeckEfId = SCOPE_IDENTITY();
    INSERT INTO #DeckMap (DeckKey, Id) VALUES (N'efcore', @DeckEfId);

    IF COL_LENGTH(N'dbo.Decks', N'TimeLimitMinutes') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Decks
              SET TimeLimitMinutes = CASE Id
                  WHEN @DeckCSharpId THEN 15
                  WHEN @DeckMvcId THEN 20
                  WHEN @DeckEfId THEN 10
                  ELSE TimeLimitMinutes
              END
              WHERE Id IN (@DeckCSharpId, @DeckMvcId, @DeckEfId);',
            N'@DeckCSharpId INT, @DeckMvcId INT, @DeckEfId INT',
            @DeckCSharpId = @DeckCSharpId,
            @DeckMvcId = @DeckMvcId,
            @DeckEfId = @DeckEfId;
    END;

    IF COL_LENGTH(N'dbo.Decks', N'CreatedBy') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Decks SET CreatedBy = @SeedBy WHERE Id IN (@DeckCSharpId, @DeckMvcId, @DeckEfId);',
            N'@SeedBy NVARCHAR(256), @DeckCSharpId INT, @DeckMvcId INT, @DeckEfId INT',
            @SeedBy = @SeedBy,
            @DeckCSharpId = @DeckCSharpId,
            @DeckMvcId = @DeckMvcId,
            @DeckEfId = @DeckEfId;
    END;

    ---------------------------------------------------------------------------
    -- Questions and answers
    ---------------------------------------------------------------------------
    DECLARE @QuestionSeeds TABLE
    (
        QuestionCode NVARCHAR(50) PRIMARY KEY,
        DeckKey NVARCHAR(50) NOT NULL,
        QuestionType INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Explanation NVARCHAR(MAX) NULL
    );

    INSERT INTO @QuestionSeeds (QuestionCode, DeckKey, QuestionType, Content, Explanation)
    VALUES
    (N'CS01', N'csharp', 1, N'CLR trong .NET có vai trò chính là gì?', N'CLR quản lý việc thực thi mã .NET, bộ nhớ, ngoại lệ và bảo mật runtime.'),
    (N'CS02', N'csharp', 2, N'Những đặc tính cốt lõi của lập trình hướng đối tượng gồm những gì?', N'OOP thường được mô tả bằng đóng gói, kế thừa, đa hình và trừu tượng hóa.'),
    (N'CS03', N'csharp', 1, N'Trong C#, kiểu string có đặc điểm nào?', N'String là immutable, mỗi thao tác thay đổi thường tạo chuỗi mới.'),
    (N'CS04', N'csharp', 1, N'Interface thường được dùng để làm gì?', N'Interface mô tả hợp đồng hành vi mà class hoặc struct triển khai.'),
    (N'CS05', N'csharp', 1, N'LINQ deferred execution nghĩa là gì?', N'Truy vấn chỉ được thực thi khi dữ liệu được duyệt hoặc materialize.'),
    (N'CS06', N'csharp', 2, N'Đâu là ví dụ của value type trong C#?', N'Value type thường lưu trực tiếp giá trị, ví dụ int, bool, struct.'),
    (N'CS07', N'csharp', 1, N'Khối finally trong try-catch-finally được chạy khi nào?', N'finally thường chạy sau try/catch, dù có exception hay không, trừ một số trường hợp đặc biệt như process bị dừng.'),
    (N'MVC01', N'mvc', 1, N'Controller trong mô hình MVC chịu trách nhiệm chính gì?', N'Controller tiếp nhận request, điều phối xử lý và chọn response hoặc view phù hợp.'),
    (N'MVC02', N'mvc', 1, N'Razor View dùng để làm gì trong ASP.NET Core MVC?', N'Razor View render HTML động dựa trên model và cú pháp Razor.'),
    (N'MVC03', N'mvc', 1, N'ModelState.IsValid thường dùng để kiểm tra điều gì?', N'Nó kiểm tra dữ liệu binding và validation có hợp lệ theo ViewModel hay không.'),
    (N'MVC04', N'mvc', 2, N'Những Tag Helper nào thường dùng để tạo route hoặc form binding?', N'Các asp-controller, asp-action, asp-for giúp Razor sinh HTML gắn với route hoặc model.'),
    (N'MVC05', N'mvc', 1, N'Trong pipeline ASP.NET Core, UseAuthentication nên đặt ở đâu?', N'UseAuthentication nên đặt trước UseAuthorization để authorization đọc được user principal.'),
    (N'MVC06', N'mvc', 1, N'ViewModel giúp ích gì trong MVC?', N'ViewModel giới hạn dữ liệu cần hiển thị hoặc nhận từ form, tránh bind trực tiếp entity quá mức.'),
    (N'MVC07', N'mvc', 1, N'TempData thường phù hợp cho tình huống nào?', N'TempData phù hợp truyền thông báo ngắn sau redirect, ví dụ success hoặc error message.'),
    (N'EF01', N'efcore', 1, N'DbContext trong Entity Framework Core đại diện cho điều gì?', N'DbContext đại diện cho phiên làm việc với database, quản lý DbSet, tracking và SaveChanges.'),
    (N'EF02', N'efcore', 1, N'Include trong EF Core dùng để làm gì?', N'Include eager-load navigation property để lấy dữ liệu liên quan trong cùng truy vấn.'),
    (N'EF03', N'efcore', 1, N'Migration trong EF Core dùng cho mục đích gì?', N'Migration mô tả thay đổi schema theo thời gian và giúp cập nhật database có kiểm soát.'),
    (N'EF04', N'efcore', 1, N'AsNoTracking phù hợp khi nào?', N'AsNoTracking phù hợp cho truy vấn chỉ đọc, giảm chi phí change tracking.'),
    (N'EF05', N'efcore', 2, N'Quan hệ nào đúng với schema Quiz Management hiện tại?', N'Schema hiện tại có User - Subject, Subject - Deck và Deck - Question theo dạng một-nhiều.'),
    (N'EF06', N'efcore', 1, N'SaveChanges trong EF Core có tác dụng gì?', N'SaveChanges gửi các thay đổi đang được DbContext tracking xuống database.'),
    (N'EF07', N'efcore', 1, N'Soft delete thường được hiểu là gì?', N'Soft delete đánh dấu dữ liệu đã xóa bằng cờ như IsDeleted thay vì xóa vật lý khỏi database.');

    CREATE TABLE #QuestionMap (QuestionCode NVARCHAR(50) NOT NULL PRIMARY KEY, Id INT NOT NULL);

    MERGE dbo.Questions AS target
    USING
    (
        SELECT qs.QuestionCode, dm.Id AS DeckId, qs.Content, qs.Explanation, qs.QuestionType
        FROM @QuestionSeeds qs
        JOIN #DeckMap dm ON dm.DeckKey = qs.DeckKey
    ) AS source
    ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (DeckId, Content, Explanation, QuestionType)
        VALUES (source.DeckId, source.Content, source.Explanation, source.QuestionType)
    OUTPUT source.QuestionCode, inserted.Id INTO #QuestionMap (QuestionCode, Id);

    IF COL_LENGTH(N'dbo.Questions', N'CreatedBy') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE q
              SET CreatedBy = @SeedBy
              FROM dbo.Questions q
              JOIN #QuestionMap qm ON qm.Id = q.Id;',
            N'@SeedBy NVARCHAR(256)',
            @SeedBy = @SeedBy;
    END;

    DECLARE @AnswerSeeds TABLE
    (
        QuestionCode NVARCHAR(50) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        IsCorrect BIT NOT NULL
    );

    INSERT INTO @AnswerSeeds (QuestionCode, Content, IsCorrect)
    VALUES
    (N'CS01', N'Quản lý thực thi mã .NET, bộ nhớ và exception', 1),
    (N'CS01', N'Thiết kế giao diện người dùng', 0),
    (N'CS01', N'Lưu trữ dữ liệu quan hệ', 0),
    (N'CS01', N'Tạo file CSS cho ứng dụng', 0),
    (N'CS02', N'Đóng gói', 1),
    (N'CS02', N'Kế thừa', 1),
    (N'CS02', N'Đa hình', 1),
    (N'CS02', N'Trừu tượng hóa', 1),
    (N'CS03', N'string là immutable', 1),
    (N'CS03', N'string luôn là value type', 0),
    (N'CS03', N'string không thể so sánh bằng Equals', 0),
    (N'CS03', N'string chỉ lưu được ASCII', 0),
    (N'CS04', N'Mô tả hợp đồng hành vi cần triển khai', 1),
    (N'CS04', N'Bắt buộc chứa constructor có tham số', 0),
    (N'CS04', N'Lưu dữ liệu vào database', 0),
    (N'CS04', N'Thay thế hoàn toàn mọi class', 0),
    (N'CS05', N'Truy vấn chỉ chạy khi được duyệt hoặc materialize', 1),
    (N'CS05', N'Truy vấn luôn chạy ngay khi khai báo', 0),
    (N'CS05', N'Truy vấn không bao giờ chạy trên database', 0),
    (N'CS05', N'Truy vấn chỉ dùng được với mảng', 0),
    (N'CS06', N'int', 1),
    (N'CS06', N'bool', 1),
    (N'CS06', N'struct tự định nghĩa', 1),
    (N'CS06', N'class tự định nghĩa', 0),
    (N'CS07', N'Sau try/catch, dù có exception hay không trong luồng bình thường', 1),
    (N'CS07', N'Chỉ chạy khi không có exception', 0),
    (N'CS07', N'Chỉ chạy trước try', 0),
    (N'CS07', N'Không bao giờ chạy nếu có catch', 0),
    (N'MVC01', N'Tiếp nhận request và điều phối response hoặc view', 1),
    (N'MVC01', N'Chỉ chứa CSS của trang', 0),
    (N'MVC01', N'Là bảng dữ liệu trong SQL Server', 0),
    (N'MVC01', N'Chỉ dùng để lưu ảnh', 0),
    (N'MVC02', N'Render HTML động từ model và Razor syntax', 1),
    (N'MVC02', N'Thay thế hoàn toàn controller', 0),
    (N'MVC02', N'Chỉ chạy câu lệnh SQL', 0),
    (N'MVC02', N'Mã hóa password người dùng', 0),
    (N'MVC03', N'Dữ liệu gửi lên có hợp lệ theo validation hay không', 1),
    (N'MVC03', N'Người dùng có phải admin hay không', 0),
    (N'MVC03', N'Ứng dụng có kết nối internet hay không', 0),
    (N'MVC03', N'File CSS có tồn tại hay không', 0),
    (N'MVC04', N'asp-controller', 1),
    (N'MVC04', N'asp-action', 1),
    (N'MVC04', N'asp-for', 1),
    (N'MVC04', N'href cố định không qua Tag Helper', 0),
    (N'MVC05', N'Trước UseAuthorization', 1),
    (N'MVC05', N'Sau MapControllerRoute', 0),
    (N'MVC05', N'Chỉ đặt trong appsettings.json', 0),
    (N'MVC05', N'Không cần dùng khi có cookie auth', 0),
    (N'MVC06', N'Giới hạn dữ liệu hiển thị hoặc nhận từ form', 1),
    (N'MVC06', N'Thay thế database', 0),
    (N'MVC06', N'Bắt buộc trùng hoàn toàn với entity', 0),
    (N'MVC06', N'Chỉ dùng cho file JavaScript', 0),
    (N'MVC07', N'Truyền thông báo ngắn sau redirect', 1),
    (N'MVC07', N'Lưu dữ liệu vĩnh viễn nhiều năm', 0),
    (N'MVC07', N'Thay thế session authentication', 0),
    (N'MVC07', N'Biên dịch Razor View', 0),
    (N'EF01', N'Phiên làm việc với database và change tracker', 1),
    (N'EF01', N'Một file CSS', 0),
    (N'EF01', N'Một loại cookie', 0),
    (N'EF01', N'Một trình duyệt web', 0),
    (N'EF02', N'Eager-load dữ liệu liên quan qua navigation property', 1),
    (N'EF02', N'Mã hóa password', 0),
    (N'EF02', N'Tạo branch Git', 0),
    (N'EF02', N'Xóa mọi dữ liệu trong database', 0),
    (N'EF03', N'Quản lý thay đổi schema database theo thời gian', 1),
    (N'EF03', N'Tự động viết README', 0),
    (N'EF03', N'Chỉ dùng để tạo CSS', 0),
    (N'EF03', N'Thay thế controller', 0),
    (N'EF04', N'Khi truy vấn chỉ đọc và không cần cập nhật entity', 1),
    (N'EF04', N'Khi bắt buộc sửa entity ngay sau query', 0),
    (N'EF04', N'Khi muốn bật tracking mạnh hơn', 0),
    (N'EF04', N'Khi muốn bỏ qua validation form', 0),
    (N'EF05', N'User có nhiều Subject', 1),
    (N'EF05', N'Subject có nhiều Deck', 1),
    (N'EF05', N'Deck có nhiều Question', 1),
    (N'EF05', N'Answer có nhiều Question làm parent trực tiếp', 0),
    (N'EF06', N'Ghi các thay đổi đang tracking xuống database', 1),
    (N'EF06', N'Chỉ render HTML', 0),
    (N'EF06', N'Tự động đăng nhập người dùng', 0),
    (N'EF06', N'Tạo file PDF', 0),
    (N'EF07', N'Đánh dấu IsDeleted thay vì xóa vật lý', 1),
    (N'EF07', N'Xóa thẳng record khỏi database trong mọi trường hợp', 0),
    (N'EF07', N'Tự động tạo tài khoản admin', 0),
    (N'EF07', N'Chỉ áp dụng cho file ảnh', 0);

    INSERT INTO dbo.Answers (QuestionId, Content, IsCorrect)
    SELECT qm.Id, a.Content, a.IsCorrect
    FROM @AnswerSeeds a
    JOIN #QuestionMap qm ON qm.QuestionCode = a.QuestionCode;

    ---------------------------------------------------------------------------
    -- Quiz history for leaderboard/statistics
    ---------------------------------------------------------------------------
    DECLARE @HistoryId INT;

    INSERT INTO dbo.TestHistories (UserId, DeckId, Score, Percentage, CreatedAt)
    VALUES (@UserId, @DeckMvcId, 8.00, 80.00, DATEADD(DAY, -1, SYSUTCDATETIME()));
    SET @HistoryId = SCOPE_IDENTITY();

    DECLARE @Mvc04Id INT = (SELECT Id FROM #QuestionMap WHERE QuestionCode = N'MVC04');
    DECLARE @Mvc05Id INT = (SELECT Id FROM #QuestionMap WHERE QuestionCode = N'MVC05');

    INSERT INTO dbo.TestResultDetails (TestHistoryId, QuestionId, SelectedAnswerId, IsCorrect)
    SELECT @HistoryId, qm.Id, a.Id, 1
    FROM #QuestionMap qm
    JOIN dbo.Answers a ON a.QuestionId = qm.Id AND a.IsCorrect = 1
    WHERE qm.QuestionCode IN (N'MVC01', N'MVC02', N'MVC03');

    INSERT INTO dbo.TestResultDetails (TestHistoryId, QuestionId, SelectedAnswerId, IsCorrect)
    SELECT @HistoryId, @Mvc04Id, a.Id, 1
    FROM dbo.Answers a
    WHERE a.QuestionId = @Mvc04Id AND a.IsCorrect = 1;

    INSERT INTO dbo.TestResultDetails (TestHistoryId, QuestionId, SelectedAnswerId, IsCorrect)
    SELECT TOP (1) @HistoryId, @Mvc05Id, a.Id, 0
    FROM dbo.Answers a
    WHERE a.QuestionId = @Mvc05Id AND a.IsCorrect = 0
    ORDER BY a.Id;

    INSERT INTO dbo.TestHistories (UserId, DeckId, Score, Percentage, CreatedAt)
    VALUES (@UserId, @DeckCSharpId, 6.00, 75.00, DATEADD(DAY, -2, SYSUTCDATETIME()));

    ---------------------------------------------------------------------------
    -- Phase 10 demo data: question reports and login attempts
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.QuestionReports', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'QuestionId') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'UserId') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'Reason') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'Note') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'IsResolved') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'CreatedAt') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'INSERT INTO dbo.QuestionReports (QuestionId, UserId, Reason, Note, IsResolved, CreatedAt)
              VALUES (@QuestionId, @UserId, @Reason, @Note, 0, DATEADD(HOUR, -6, SYSUTCDATETIME()));',
            N'@QuestionId INT, @UserId NVARCHAR(450), @Reason NVARCHAR(100), @Note NVARCHAR(500)',
            @QuestionId = @Mvc05Id,
            @UserId = @UserId,
            @Reason = N'UnclearQuestion',
            @Note = N'Demo: người học cần mentor kiểm tra lại vị trí middleware.';
    END;

    IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'Email') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'IpAddress') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'IsSuccess') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'UserId') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'CreatedAt') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'INSERT INTO dbo.LoginAttempts (Email, IpAddress, IsSuccess, UserId, CreatedAt)
              VALUES
              (N''admin.demo@quiz.local'', N''127.0.0.1'', 1, @AdminId, DATEADD(HOUR, -4, SYSUTCDATETIME())),
              (N''mentor.demo@quiz.local'', N''127.0.0.1'', 1, @MentorId, DATEADD(HOUR, -3, SYSUTCDATETIME())),
              (N''user.demo@quiz.local'', N''127.0.0.1'', 1, @UserId, DATEADD(HOUR, -2, SYSUTCDATETIME())),
              (N''wrong.demo@quiz.local'', N''127.0.0.1'', 0, NULL, DATEADD(HOUR, -1, SYSUTCDATETIME()));',
            N'@AdminId NVARCHAR(450), @MentorId NVARCHAR(450), @UserId NVARCHAR(450)',
            @AdminId = @AdminId,
            @MentorId = @MentorId,
            @UserId = @UserId;
    END;

    COMMIT TRANSACTION;

    SELECT Username, Email, Role, N'Test@123456' AS DemoPassword
    FROM dbo.Users
    WHERE Id IN (@AdminId, @MentorId, @UserId)
    ORDER BY Role;

    SELECT
        (SELECT COUNT(*) FROM dbo.Subjects WHERE UserId = @MentorId) AS SubjectCount,
        (SELECT COUNT(*)
         FROM dbo.Decks d
         JOIN dbo.Subjects s ON s.Id = d.SubjectId
         WHERE s.UserId = @MentorId) AS DeckCount,
        (SELECT COUNT(*)
         FROM dbo.Questions q
         JOIN dbo.Decks d ON d.Id = q.DeckId
         JOIN dbo.Subjects s ON s.Id = d.SubjectId
         WHERE s.UserId = @MentorId) AS QuestionCount,
        (SELECT COUNT(*)
         FROM dbo.TestHistories
         WHERE UserId = @UserId) AS TestHistoryCount;

    IF OBJECT_ID(N'dbo.QuestionReports', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.QuestionReports', N'UserId') IS NOT NULL
        EXEC(N'SELECT COUNT(*) AS QuestionReportCount FROM dbo.QuestionReports WHERE UserId IN (SELECT Id FROM #SeedUserIds);');

    IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.LoginAttempts', N'Email') IS NOT NULL
        EXEC(N'SELECT COUNT(*) AS LoginAttemptCount FROM dbo.LoginAttempts WHERE Email LIKE N''%.demo@quiz.local'' OR Email = N''wrong.demo@quiz.local'';');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'SeedDemoData.sql failed. No partial seed data was committed.';
    THROW;
END CATCH;
GO
