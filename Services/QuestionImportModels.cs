namespace Services
{
    public class QuestionImportPreview
    {
        public List<QuestionImportRow> ValidRows { get; set; } = new();

        public List<QuestionImportError> Errors { get; set; } = new();
    }

    public class QuestionImportRow
    {
        public int RowNumber { get; set; }

        public string Content { get; set; } = string.Empty;

        public int QuestionType { get; set; }

        public string? Explanation { get; set; }

        public List<QuestionImportAnswer> Answers { get; set; } = new();
    }

    public class QuestionImportAnswer
    {
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }

    public class QuestionImportError
    {
        public int RowNumber { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}