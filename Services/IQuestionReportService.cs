using BusinessObjects;

namespace Services;

public interface IQuestionReportService
{
    void Submit(int questionId, string userId, string reason, string? note);
    List<QuestionReport> GetAllReports();
    List<QuestionReport> GetReportsByContentOwner(string ownerUserId);
    QuestionReport? GetReportById(int reportId);
    void Resolve(int reportId);
    bool HasPendingReport(int questionId, string userId);
}
