using Services;

namespace QuizManagement.Tests.Services;

public class QuestionImportServiceTests
{
    private readonly QuestionImportService _service = new();

    [Fact]
    public void ParseText_WithValidSingleChoice_ReturnsOneValidRow()
    {
        var input = """
Question: What is CLR?
Type: single
* Runtime for .NET code
- CSS renderer
- SQL database
Explanation: CLR executes managed code.
""";

        var result = _service.ParseText(input);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.ValidRows);
        Assert.Equal("What is CLR?", row.Content);
        Assert.Equal(1, row.QuestionType);
        Assert.Equal(3, row.Answers.Count);
        Assert.Single(row.Answers, answer => answer.IsCorrect);
    }

    [Fact]
    public void ParseText_WhenMultipleAnswersAreCorrect_InfersMultipleChoice()
    {
        var input = """
Question: Select OOP pillars
* Encapsulation
* Polymorphism
- Migration
""";

        var result = _service.ParseText(input);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.ValidRows);
        Assert.Equal(2, row.QuestionType);
        Assert.Equal(2, row.Answers.Count(a => a.IsCorrect));
    }

    [Fact]
    public void ParseText_WithInvalidSingleChoice_ReturnsValidationError()
    {
        var input = """
Question: Select one correct answer
Type: single
* A
* B
- C
""";

        var result = _service.ParseText(input);

        Assert.Empty(result.ValidRows);
        var error = Assert.Single(result.Errors);
        Assert.Contains("single", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRows_TrimsContentAndDropsBlankAnswers()
    {
        var rows = new[]
        {
            new QuestionImportRow
            {
                RowNumber = 1,
                Content = "  Question content  ",
                QuestionType = 1,
                Answers =
                {
                    new QuestionImportAnswer { Content = "  Correct  ", IsCorrect = true },
                    new QuestionImportAnswer { Content = "   ", IsCorrect = false },
                    new QuestionImportAnswer { Content = "Wrong", IsCorrect = false }
                }
            }
        };

        var result = _service.ValidateRows(rows);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.ValidRows);
        Assert.Equal("Question content", row.Content);
        Assert.Equal(2, row.Answers.Count);
        Assert.Equal("Correct", row.Answers[0].Content);
    }
}
