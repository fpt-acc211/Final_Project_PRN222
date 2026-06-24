using BusinessObjects;
using Repositories;

namespace Services;

public class QuestionReportService : IQuestionReportService
{
    private readonly IQuestionReportRepository _repository;

    public QuestionReportService(IQuestionReportRepository repository)
    {
        _repository = repository;
    }

    public void Submit(int questionId, string userId, string reason, string? note)
    {
        var report = new QuestionReport
        {
            QuestionId = questionId,
            UserId = userId,
            Reason = reason,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };
        _repository.Create(report);
    }

    public List<QuestionReport> GetAllReports() => _repository.GetAll();
    public List<QuestionReport> GetReportsByContentOwner(string ownerUserId) => _repository.GetByContentOwner(ownerUserId);
    public QuestionReport? GetReportById(int reportId) => _repository.GetByIdWithDetails(reportId);
    public void Resolve(int reportId) => _repository.Resolve(reportId);
    public bool HasPendingReport(int questionId, string userId) => _repository.HasPendingReport(questionId, userId);
}
