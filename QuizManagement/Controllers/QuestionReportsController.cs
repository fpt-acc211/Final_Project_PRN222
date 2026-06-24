using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.QuestionReports;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize(Policy = "StudyContent")]
    public class QuestionReportsController : Controller
    {
        private static readonly HashSet<string> AllowedReportReasons = new(StringComparer.Ordinal)
        {
            "WrongAnswer",
            "UnclearQuestion",
            "DuplicateQuestion",
            "Other"
        };

        private readonly IQuestionReportService _reportService;
        private readonly IQuestionService _questionService;

        public QuestionReportsController(IQuestionReportService reportService, IQuestionService questionService)
        {
            _reportService = reportService;
            _questionService = questionService;
        }

        // GET /QuestionReports/Create?questionId=X&testHistoryId=Y
        public IActionResult Create(int questionId, int? testHistoryId)
        {
            var question = _questionService.GetQuestionById(questionId, CurrentUserId(), allowAll: true);
            if (question is null) return NotFound();

            return View(new SubmitReportViewModel
            {
                QuestionId = questionId,
                QuestionContent = question.Content,
                TestHistoryId = testHistoryId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SubmitReportViewModel model)
        {
            var currentUserId = CurrentUserId();
            var question = _questionService.GetQuestionById(model.QuestionId, currentUserId, allowAll: true);
            if (question is null) return NotFound();

            model.QuestionContent = question.Content;

            if (!AllowedReportReasons.Contains(model.Reason))
            {
                ModelState.AddModelError(nameof(model.Reason), "Lý do báo cáo không hợp lệ.");
            }

            if (!ModelState.IsValid) return View(model);

            if (_reportService.HasPendingReport(model.QuestionId, currentUserId))
            {
                TempData["SuccessMessage"] = "Bạn đã gửi báo cáo cho câu hỏi này và đang chờ xử lý.";
                return RedirectAfterSubmit(model);
            }

            _reportService.Submit(model.QuestionId, currentUserId, model.Reason, model.Note);

            TempData["SuccessMessage"] = "Đã gửi báo cáo. Cảm ơn bạn đã đóng góp!";

            return RedirectAfterSubmit(model);
        }

        // GET /QuestionReports — for Mentor/Admin
        [Authorize(Policy = "ManageContent")]
        public IActionResult Index()
        {
            var isAdmin = User.IsInRole(AppRoles.Admin);
            var reports = isAdmin
                ? _reportService.GetAllReports()
                : _reportService.GetReportsByContentOwner(CurrentUserId());

            var model = reports.Select(r => new QuestionReportListItemViewModel
            {
                Id = r.Id,
                QuestionId = r.QuestionId,
                QuestionContent = r.Question?.Content ?? "(câu hỏi đã bị xóa)",
                DeckName = r.Question?.Deck?.Name ?? "-",
                SubjectName = r.Question?.Deck?.Subject?.Name ?? "-",
                ReporterUsername = r.User?.Username ?? "-",
                Reason = ReasonLabel(r.Reason),
                Note = r.Note,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt
            }).ToList();

            return View(model);
        }

        // POST /QuestionReports/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManageContent")]
        public IActionResult Resolve(int id)
        {
            var report = _reportService.GetReportById(id);
            if (report is null) return NotFound();

            if (!User.IsInRole(AppRoles.Admin)
                && report.Question?.Deck?.Subject?.UserId != CurrentUserId())
            {
                return Forbid();
            }

            _reportService.Resolve(id);
            TempData["SuccessMessage"] = "Đã đánh dấu báo cáo là đã xử lý.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectAfterSubmit(SubmitReportViewModel model)
        {
            if (model.TestHistoryId.HasValue)
                return RedirectToAction("Result", "Quiz", new { id = model.TestHistoryId.Value });

            return RedirectToAction("Index", "Subjects");
        }

        private string CurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new InvalidOperationException("Không tìm thấy UserId.");

        private static string ReasonLabel(string reason) => reason switch
        {
            "WrongAnswer" => "Đáp án sai",
            "UnclearQuestion" => "Câu hỏi không rõ ràng",
            "DuplicateQuestion" => "Câu hỏi trùng lặp",
            "Other" => "Khác",
            _ => reason
        };
    }
}
