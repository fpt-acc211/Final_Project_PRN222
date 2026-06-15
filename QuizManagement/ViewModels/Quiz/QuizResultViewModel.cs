namespace QuizManagement.ViewModels.Quiz
{
    /// <summary>
    /// ViewModel hiển thị kết quả sau khi nộp bài hoặc xem lại lịch sử
    /// </summary>
    public class QuizResultViewModel
    {
        public int TestHistoryId { get; set; }

        public string DeckName { get; set; } = string.Empty;

        public string SubjectName { get; set; } = string.Empty;

        public double Score { get; set; }

        public double Percentage { get; set; }

        public int CorrectCount { get; set; }

        public int TotalCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<QuizResultQuestionViewModel> Questions { get; set; } = new();
    }

    public class QuizResultQuestionViewModel
    {
        public int QuestionId { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? Explanation { get; set; }

        public int QuestionType { get; set; }

        public bool IsCorrect { get; set; }

        public List<QuizResultAnswerViewModel> Answers { get; set; } = new();
    }

    public class QuizResultAnswerViewModel
    {
        public int AnswerId { get; set; }

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Đáp án này là đáp án đúng
        /// </summary>
        public bool IsCorrectAnswer { get; set; }

        /// <summary>
        /// Người dùng đã chọn đáp án này
        /// </summary>
        public bool WasSelected { get; set; }
    }
}
