using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Import;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize(Policy = "ManageContent")]
    public class QuestionImportController : Controller
    {
        private readonly IDeckService _deckService;
        private readonly IQuestionService _questionService;
        private readonly IQuestionImportService _questionImportService;

        public QuestionImportController(
            IDeckService deckService,
            IQuestionService questionService,
            IQuestionImportService questionImportService)
        {
            _deckService = deckService;
            _questionService = questionService;
            _questionImportService = questionImportService;
        }

        public IActionResult Import(int deckId)
        {
            var deck = _deckService.GetDeckById(deckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            return View("~/Views/Questions/Import.cshtml", BuildInputModel(deck));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Preview(QuestionImportInputViewModel model)
        {
            var deck = _deckService.GetDeckById(model.DeckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            QuestionImportPreview preview;
            var sourceName = "Text";

            try
            {
                if (model.ExcelFile is { Length: > 0 })
                {
                    if (!model.ExcelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.ExcelFile), "Chỉ hỗ trợ file .xlsx.");
                        return View("~/Views/Questions/Import.cshtml", BuildInputModel(deck, model.RawText));
                    }

                    using var stream = model.ExcelFile.OpenReadStream();
                    preview = _questionImportService.ParseExcel(stream);
                    sourceName = model.ExcelFile.FileName;
                }
                else if (!string.IsNullOrWhiteSpace(model.RawText))
                {
                    preview = _questionImportService.ParseText(model.RawText);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng tải file Excel hoặc nhập nội dung text.");
                    return View("~/Views/Questions/Import.cshtml", BuildInputModel(deck, model.RawText));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Không đọc được dữ liệu import: {ex.Message}");
                return View("~/Views/Questions/Import.cshtml", BuildInputModel(deck, model.RawText));
            }

            var previewModel = BuildPreviewModel(deck, preview, sourceName);
            return View("~/Views/Questions/ImportPreview.cshtml", previewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Commit(QuestionImportPreviewViewModel model)
        {
            var deck = _deckService.GetDeckById(model.DeckId, CurrentUserId());
            if (deck is null)
            {
                return NotFound();
            }

            var validation = _questionImportService.ValidateRows(model.ValidRows.Select(ToServiceRow));
            if (validation.ValidRows.Count == 0)
            {
                var previewModel = BuildPreviewModel(deck, validation, model.SourceName);
                ModelState.AddModelError(string.Empty, "Không có câu hỏi hợp lệ để import.");
                return View("~/Views/Questions/ImportPreview.cshtml", previewModel);
            }

            foreach (var row in validation.ValidRows)
            {
                _questionService.AddQuestion(new Question
                {
                    DeckId = deck.Id,
                    Content = row.Content,
                    Explanation = row.Explanation,
                    QuestionType = row.QuestionType,
                    CreatedBy = User.Identity?.Name,
                    UpdatedBy = User.Identity?.Name,
                    Answers = row.Answers.Select(answer => new Answer
                    {
                        Content = answer.Content,
                        IsCorrect = answer.IsCorrect
                    }).ToList()
                });
            }

            TempData["SuccessMessage"] = $"Đã import {validation.ValidRows.Count} câu hỏi.";
            return RedirectToAction("Index", "Questions", new { deckId = deck.Id });
        }

        private QuestionImportInputViewModel BuildInputModel(Deck deck, string? rawText = null)
        {
            return new QuestionImportInputViewModel
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                RawText = rawText,
                TextTemplate = _questionImportService.GetTextTemplate()
            };
        }

        private static QuestionImportPreviewViewModel BuildPreviewModel(Deck deck, QuestionImportPreview preview, string sourceName)
        {
            return new QuestionImportPreviewViewModel
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                SourceName = sourceName,
                ValidRows = preview.ValidRows.Select(row => new QuestionImportRowViewModel
                {
                    RowNumber = row.RowNumber,
                    Content = row.Content,
                    QuestionType = row.QuestionType,
                    Explanation = row.Explanation,
                    Answers = row.Answers.Select(answer => new QuestionImportAnswerViewModel
                    {
                        Content = answer.Content,
                        IsCorrect = answer.IsCorrect
                    }).ToList()
                }).ToList(),
                Errors = preview.Errors.Select(error => new QuestionImportErrorViewModel
                {
                    RowNumber = error.RowNumber,
                    Message = error.Message
                }).ToList()
            };
        }

        private static QuestionImportRow ToServiceRow(QuestionImportRowViewModel row)
        {
            return new QuestionImportRow
            {
                RowNumber = row.RowNumber,
                Content = row.Content,
                QuestionType = row.QuestionType,
                Explanation = row.Explanation,
                Answers = row.Answers.Select(answer => new QuestionImportAnswer
                {
                    Content = answer.Content,
                    IsCorrect = answer.IsCorrect
                }).ToList()
            };
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }
    }
}