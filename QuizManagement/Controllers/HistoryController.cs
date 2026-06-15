using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Quiz;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly IQuizService _quizService;

        public HistoryController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        /// <summary>
        /// GET: Danh sách lịch sử làm bài của user hiện tại
        /// </summary>
        public IActionResult Index()
        {
            var histories = _quizService.GetTestHistoriesByUser(CurrentUserId());
            return View(histories);
        }

        /// <summary>
        /// GET: Chi tiết một lần làm bài (xem lại câu hỏi + đáp án)
        /// </summary>
        public IActionResult Details(int id)
        {
            var history = _quizService.GetTestHistoryById(id, CurrentUserId());
            if (history is null)
            {
                return NotFound();
            }

            // Tái sử dụng logic build result từ QuizController
            var resultModel = BuildResultViewModel(history);
            return View(resultModel);
        }

        private static QuizResultViewModel BuildResultViewModel(TestHistory history)
        {
            var questionGroups = history.TestResultDetails
                .GroupBy(d => d.QuestionId)
                .ToList();

            var questionResults = questionGroups.Select(g =>
            {
                var firstDetail = g.First();
                var question = firstDetail.Question;
                var selectedAnswerIds = g
                    .Where(d => d.SelectedAnswerId.HasValue)
                    .Select(d => d.SelectedAnswerId!.Value)
                    .ToHashSet();

                return new QuizResultQuestionViewModel
                {
                    QuestionId = question.Id,
                    Content = question.Content,
                    Explanation = question.Explanation,
                    QuestionType = question.QuestionType,
                    IsCorrect = firstDetail.IsCorrect,
                    Answers = question.Answers
                        .OrderBy(a => a.Id)
                        .Select(a => new QuizResultAnswerViewModel
                        {
                            AnswerId = a.Id,
                            Content = a.Content,
                            IsCorrectAnswer = a.IsCorrect,
                            WasSelected = selectedAnswerIds.Contains(a.Id)
                        }).ToList()
                };
            }).ToList();

            return new QuizResultViewModel
            {
                TestHistoryId = history.Id,
                DeckName = history.Deck.Name,
                SubjectName = history.Deck.Subject.Name,
                Score = history.Score,
                Percentage = history.Percentage,
                CorrectCount = questionResults.Count(q => q.IsCorrect),
                TotalCount = questionResults.Count,
                CreatedAt = history.CreatedAt,
                Questions = questionResults
            };
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }
    }
}
