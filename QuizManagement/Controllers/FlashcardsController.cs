using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Flashcards;
using Services;

namespace QuizManagement.Controllers
{
    [Authorize(Policy = "StudyContent")]
    public class FlashcardsController : Controller
    {
        private readonly IDeckService _deckService;
        private readonly IQuestionService _questionService;

        public FlashcardsController(IDeckService deckService, IQuestionService questionService)
        {
            _deckService = deckService;
            _questionService = questionService;
        }

        public IActionResult Study(int deckId)
        {
            var deck = _deckService.GetDeckForStudy(deckId);
            if (deck is null)
            {
                return NotFound();
            }

            var questions = _questionService.GetQuestionsByDeckForStudy(deckId).ToList();
            var model = new FlashcardStudyViewModel
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                Cards = questions.Select(question => new FlashcardViewModel
                {
                    QuestionId = question.Id,
                    Content = question.Content,
                    Explanation = question.Explanation,
                    QuestionType = question.QuestionType,
                    CorrectAnswers = question.Answers
                        .Where(answer => answer.IsCorrect)
                        .OrderBy(answer => answer.Id)
                        .Select(answer => answer.Content)
                        .ToList(),
                    AllAnswers = question.Answers
                        .OrderBy(answer => answer.Id)
                        .Select(answer => new FlashcardAnswerOption
                        {
                            Content = answer.Content,
                            IsCorrect = answer.IsCorrect
                        })
                        .ToList()
                }).ToList()
            };

            return View(model);
        }

    }
}
