using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Quiz;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly IDeckService _deckService;
        private readonly IQuizService _quizService;

        public QuizController(IDeckService deckService, IQuizService quizService)
        {
            _deckService = deckService;
            _quizService = quizService;
        }

        /// <summary>
        /// GET: Hiển thị form cấu hình bài quiz (chọn số câu hỏi)
        /// </summary>
        public IActionResult Config(int deckId)
        {
            var deck = _deckService.GetDeckById(deckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            var availableCount = _quizService.GetAvailableQuestionCount(deckId, CurrentUserId());
            if (availableCount == 0)
            {
                TempData["ErrorMessage"] = "Bộ đề này chưa có câu hỏi nào để làm bài.";
                return RedirectToAction("Index", "Questions", new { deckId });
            }

            return View(new QuizConfigViewModel
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                AvailableQuestionCount = availableCount,
                QuestionCount = Math.Min(10, availableCount)
            });
        }

        /// <summary>
        /// POST: Nhận cấu hình, lấy và shuffle câu hỏi, hiển thị giao diện làm bài
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Take(QuizConfigViewModel config)
        {
            var deck = _deckService.GetDeckById(config.DeckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            var availableCount = _quizService.GetAvailableQuestionCount(config.DeckId, CurrentUserId());
            config.AvailableQuestionCount = availableCount;
            config.DeckName = deck.Name;
            config.SubjectName = deck.Subject.Name;

            if (config.QuestionCount < 1 || config.QuestionCount > availableCount)
            {
                ModelState.AddModelError(nameof(config.QuestionCount),
                    $"Số câu hỏi phải từ 1 đến {availableCount}.");
                return View("Config", config);
            }

            if (!ModelState.IsValid)
            {
                return View("Config", config);
            }

            // Lấy câu hỏi đã shuffle (Fisher-Yates)
            var questions = _quizService.GetQuestionsForQuiz(
                config.DeckId, config.QuestionCount, CurrentUserId());

            // Map sang ViewModel - KHÔNG gửi IsCorrect ra client
            var model = new QuizTakeViewModel
            {
                DeckId = config.DeckId,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                Questions = questions.Select(q => new QuizQuestionViewModel
                {
                    QuestionId = q.Id,
                    Content = q.Content,
                    QuestionType = q.QuestionType,
                    Answers = q.Answers.Select(a => new QuizAnswerOptionViewModel
                    {
                        AnswerId = a.Id,
                        Content = a.Content
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        /// <summary>
        /// POST: Nhận bài làm, chấm điểm, lưu kết quả, redirect tới Result
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(QuizSubmitViewModel model)
        {
            var deck = _deckService.GetDeckById(model.DeckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            // Chuẩn bị dữ liệu cho grading
            var submittedAnswers = model.Questions.Select(q =>
            {
                var selectedIds = new List<int>();

                if (q.QuestionType == 1) // Single choice
                {
                    if (q.SelectedAnswerId.HasValue)
                        selectedIds.Add(q.SelectedAnswerId.Value);
                }
                else // Multiple choice
                {
                    selectedIds = q.SelectedAnswerIds ?? new List<int>();
                }

                return (q.QuestionId, q.QuestionType, selectedIds);
            }).ToList();

            // Chấm điểm và lưu kết quả
            var history = _quizService.GradeAndSaveQuiz(
                model.DeckId, CurrentUserId(), submittedAnswers);

            TempData["SuccessMessage"] = "Đã nộp bài thành công!";
            return RedirectToAction(nameof(Result), new { id = history.Id });
        }

        /// <summary>
        /// GET: Hiển thị kết quả bài làm
        /// </summary>
        public IActionResult Result(int id)
        {
            var history = _quizService.GetTestHistoryById(id, CurrentUserId());
            if (history is null)
            {
                return NotFound();
            }

            var resultModel = BuildResultViewModel(history);
            return View(resultModel);
        }

        /// <summary>
        /// Xây dựng QuizResultViewModel từ TestHistory
        /// </summary>
        private static QuizResultViewModel BuildResultViewModel(TestHistory history)
        {
            // Group TestResultDetails theo QuestionId
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
