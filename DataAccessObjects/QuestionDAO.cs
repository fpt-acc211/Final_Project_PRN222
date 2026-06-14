using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessObjects
{
    public class QuestionDAO
    {
        private static QuestionDAO? instance;
        private static readonly object instanceLock = new();

        private QuestionDAO()
        {
        }

        public static QuestionDAO Instance
        {
            get
            {
                lock (instanceLock)
                {
                    instance ??= new QuestionDAO();
                    return instance;
                }
            }
        }

        public IEnumerable<Question> GetQuestionsByDeck(QuizManagementDbContext context, int deckId, string userId)
        {
            return context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Deck)
                .ThenInclude(d => d.Subject)
                .Where(q => q.DeckId == deckId && q.Deck.Subject.UserId == userId)
                .OrderByDescending(q => q.CreatedAt)
                .ToList();
        }

        public Question? GetQuestionById(QuizManagementDbContext context, int id, string userId)
        {
            return context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Deck)
                .ThenInclude(d => d.Subject)
                .FirstOrDefault(q => q.Id == id && q.Deck.Subject.UserId == userId);
        }

        public void AddQuestion(QuizManagementDbContext context, Question question)
        {
            question.CreatedAt = DateTime.UtcNow;
            context.Questions.Add(question);
            context.SaveChanges();
        }

        public void UpdateQuestion(QuizManagementDbContext context, Question question)
        {
            var existingQuestion = context.Questions
                .Include(q => q.Answers)
                .First(q => q.Id == question.Id);

            existingQuestion.Content = question.Content;
            existingQuestion.Explanation = question.Explanation;
            existingQuestion.QuestionType = question.QuestionType;
            existingQuestion.UpdatedAt = DateTime.UtcNow;
            existingQuestion.UpdatedBy = question.UpdatedBy;

            var incomingAnswers = question.Answers.ToList();
            var incomingIds = incomingAnswers.Where(a => a.Id > 0).Select(a => a.Id).ToHashSet();
            var removedAnswers = existingQuestion.Answers.Where(a => !incomingIds.Contains(a.Id)).ToList();
            context.Answers.RemoveRange(removedAnswers);

            foreach (var incomingAnswer in incomingAnswers)
            {
                if (incomingAnswer.Id > 0)
                {
                    var existingAnswer = existingQuestion.Answers.First(a => a.Id == incomingAnswer.Id);
                    existingAnswer.Content = incomingAnswer.Content;
                    existingAnswer.IsCorrect = incomingAnswer.IsCorrect;
                }
                else
                {
                    existingQuestion.Answers.Add(new Answer
                    {
                        Content = incomingAnswer.Content,
                        IsCorrect = incomingAnswer.IsCorrect
                    });
                }
            }

            context.SaveChanges();
        }

        public void DeleteQuestion(QuizManagementDbContext context, Question question)
        {
            question.IsDeleted = true;
            question.UpdatedAt = DateTime.UtcNow;
            context.Questions.Update(question);
            context.SaveChanges();
        }
    }
}
