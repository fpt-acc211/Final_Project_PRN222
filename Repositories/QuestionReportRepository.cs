using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class QuestionReportRepository : IQuestionReportRepository
{
    private readonly QuizManagementDbContext _context;

    public QuestionReportRepository(QuizManagementDbContext context)
    {
        _context = context;
    }

    public void Create(QuestionReport report) => QuestionReportDAO.Instance.Create(_context, report);
    public List<QuestionReport> GetAll() => QuestionReportDAO.Instance.GetAll(_context);
    public List<QuestionReport> GetByContentOwner(string ownerUserId) => QuestionReportDAO.Instance.GetByContentOwner(_context, ownerUserId);
    public QuestionReport? GetById(int id) => QuestionReportDAO.Instance.GetById(_context, id);
    public void Resolve(int id) => QuestionReportDAO.Instance.Resolve(_context, id);
    public bool HasPendingReport(int questionId, string userId) => QuestionReportDAO.Instance.HasPendingReport(_context, questionId, userId);
}
