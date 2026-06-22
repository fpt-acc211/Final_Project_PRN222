using BusinessObjects;
using Repositories;

namespace Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _repository;
        private static readonly Random _random = new();

        public QuizService(IQuizRepository repository)
        {
            _repository = repository;
        }

        public List<Question> GetQuestionsForQuiz(int deckId, int questionCount)
        {
            var allQuestions = _repository.GetQuestionsForQuiz(deckId);

            // Fisher-Yates shuffle cho câu hỏi
            Shuffle(allQuestions);

            // Lấy số câu hỏi theo yêu cầu
            var selected = allQuestions.Take(questionCount).ToList();

            // Fisher-Yates shuffle cho đáp án mỗi câu
            foreach (var question in selected)
            {
                var answers = question.Answers.ToList();
                Shuffle(answers);
                question.Answers = answers;
            }

            return selected;
        }

        public int GetAvailableQuestionCount(int deckId)
            => _repository.GetQuestionCount(deckId);

        public TestHistory GradeAndSaveQuiz(
            int deckId, string userId,
            List<(int QuestionId, int QuestionType, List<int> SelectedAnswerIds)> submittedAnswers)
        {
            // Lấy tất cả câu hỏi + đáp án đúng từ DB
            var allQuestions = _repository.GetQuestionsForQuiz(deckId);
            var questionDict = allQuestions.ToDictionary(q => q.Id);

            var details = new List<TestResultDetail>();
            int correctCount = 0;
            int totalCount = 0;

            foreach (var (questionId, questionType, selectedAnswerIds) in submittedAnswers)
            {
                // Chỉ xử lý câu hỏi thuộc deck này
                if (!questionDict.TryGetValue(questionId, out var question))
                    continue;

                totalCount++;

                // Lấy tập đáp án đúng
                var correctAnswerIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.Id)
                    .ToHashSet();

                var selectedSet = selectedAnswerIds.ToHashSet();

                // Chấm điểm
                bool isCorrect;
                if (questionType == 1) // Single choice
                {
                    isCorrect = selectedSet.Count == 1 && correctAnswerIds.Contains(selectedSet.First());
                }
                else // Multiple choice
                {
                    isCorrect = correctAnswerIds.SetEquals(selectedSet);
                }

                if (isCorrect) correctCount++;

                // Tạo TestResultDetail rows
                if (selectedSet.Count == 0)
                {
                    // Câu chưa trả lời
                    details.Add(new TestResultDetail
                    {
                        QuestionId = questionId,
                        SelectedAnswerId = null,
                        IsCorrect = false
                    });
                }
                else
                {
                    foreach (var answerId in selectedSet)
                    {
                        details.Add(new TestResultDetail
                        {
                            QuestionId = questionId,
                            SelectedAnswerId = answerId,
                            IsCorrect = isCorrect
                        });
                    }
                }
            }

            // Tính điểm
            double percentage = totalCount > 0 ? (double)correctCount / totalCount * 100 : 0;
            double score = totalCount > 0 ? (double)correctCount / totalCount * 10 : 0;

            var history = new TestHistory
            {
                UserId = userId,
                DeckId = deckId,
                Score = Math.Round(score, 2),
                Percentage = Math.Round(percentage, 2),
                CreatedAt = DateTime.UtcNow,
                TestResultDetails = details
            };

            return _repository.SaveTestResult(history);
        }

        public TestHistory? GetTestHistoryById(int id, string userId)
            => _repository.GetTestHistoryById(id, userId);

        public List<TestHistory> GetTestHistoriesByUser(string userId)
            => _repository.GetTestHistoriesByUser(userId);

        public List<TestHistory> GetRecentTestHistories(string userId, int count)
            => _repository.GetRecentTestHistories(userId, count);

        public (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId)
            => _repository.GetQuizStatistics(userId);

        /// <summary>
        /// Fisher-Yates shuffle algorithm
        /// </summary>
        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
