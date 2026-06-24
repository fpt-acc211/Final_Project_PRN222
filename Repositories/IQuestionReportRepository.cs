using BusinessObjects;

namespace Repositories;

public interface IQuestionReportRepository
{
    void Create(QuestionReport report);
    List<QuestionReport> GetAll();
    List<QuestionReport> GetByContentOwner(string ownerUserId);
    QuestionReport? GetById(int id);
    void Resolve(int id);
    bool HasPendingReport(int questionId, string userId);
}
