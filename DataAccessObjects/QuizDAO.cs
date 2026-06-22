using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessObjects
{
    public class QuizDAO
    {
        private static QuizDAO? instance;
        private static readonly object instanceLock = new();

        private QuizDAO()
        {
        }

        public static QuizDAO Instance
        {
            get
            {
                lock (instanceLock)
                {
                    instance ??= new QuizDAO();
                    return instance;
                }
            }
        }

        public List<Question> GetQuestionsForQuiz(QuizManagementDbContext context, int deckId)
        {
            return context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Deck)
                    .ThenInclude(d => d.Subject)
                .Where(q => q.DeckId == deckId)
                .ToList();
        }

        public int GetQuestionCount(QuizManagementDbContext context, int deckId)
        {
            return context.Questions
                .Include(q => q.Deck)
                    .ThenInclude(d => d.Subject)
                .Count(q => q.DeckId == deckId);
        }

        public TestHistory SaveTestResult(QuizManagementDbContext context, TestHistory history)
        {
            context.TestHistories.Add(history);
            context.SaveChanges();
            return history;
        }

        public TestHistory? GetTestHistoryById(QuizManagementDbContext context, int id, string userId)
        {
            return context.TestHistories
                .Include(h => h.Deck)
                    .ThenInclude(d => d.Subject)
                .Include(h => h.TestResultDetails)
                    .ThenInclude(d => d.Question)
                        .ThenInclude(q => q.Answers)
                .Include(h => h.TestResultDetails)
                    .ThenInclude(d => d.SelectedAnswer)
                .FirstOrDefault(h => h.Id == id && h.UserId == userId);
        }

        public List<TestHistory> GetTestHistoriesByUser(QuizManagementDbContext context, string userId)
        {
            return context.TestHistories
                .Include(h => h.Deck)
                    .ThenInclude(d => d.Subject)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .ToList();
        }

        public List<TestHistory> GetRecentTestHistories(QuizManagementDbContext context, string userId, int count)
        {
            return context.TestHistories
                .Include(h => h.Deck)
                    .ThenInclude(d => d.Subject)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(count)
                .ToList();
        }

        public (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(
            QuizManagementDbContext context, string userId)
        {
            var histories = context.TestHistories.Where(h => h.UserId == userId);
            var total = histories.Count();
            if (total == 0) return (0, 0, null);
            var avg = histories.Average(h => h.Percentage);
            var last = histories.Max(h => h.CreatedAt);
            return (total, Math.Round(avg, 1), last);
        }
    }
}
