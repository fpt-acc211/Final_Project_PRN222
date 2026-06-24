using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class QuestionReportDAO
{
    private static QuestionReportDAO? _instance;
    private static readonly object _lock = new();

    private QuestionReportDAO() { }

    public static QuestionReportDAO Instance
    {
        get { lock (_lock) { return _instance ??= new QuestionReportDAO(); } }
    }

    public void Create(QuizManagementDbContext context, QuestionReport report)
    {
        context.QuestionReports.Add(report);
        context.SaveChanges();
    }

    // Returns reports with Question (ignores soft-delete filter) + User + Deck + Subject
    public List<QuestionReport> GetAll(QuizManagementDbContext context)
    {
        return context.QuestionReports
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Question.Deck.Subject)
            .AsNoTracking()
            .OrderBy(r => r.IsResolved)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();
    }

    // Only reports where Question belongs to decks owned by ownerUserId
    public List<QuestionReport> GetByContentOwner(QuizManagementDbContext context, string ownerUserId)
    {
        return context.QuestionReports
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Question.Deck.Subject)
            .Where(r => r.Question.Deck.Subject.UserId == ownerUserId)
            .AsNoTracking()
            .OrderBy(r => r.IsResolved)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();
    }

    public QuestionReport? GetById(QuizManagementDbContext context, int id)
        => context.QuestionReports.Find(id);

    public QuestionReport? GetByIdWithDetails(QuizManagementDbContext context, int id)
    {
        return context.QuestionReports
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Question.Deck.Subject)
            .AsNoTracking()
            .FirstOrDefault(r => r.Id == id);
    }

    public void Resolve(QuizManagementDbContext context, int id)
    {
        var report = context.QuestionReports
            .IgnoreQueryFilters()
            .FirstOrDefault(r => r.Id == id);
        if (report is null) return;
        report.IsResolved = true;
        context.SaveChanges();
    }

    public bool HasPendingReport(QuizManagementDbContext context, int questionId, string userId)
        => context.QuestionReports.Any(r => r.QuestionId == questionId && r.UserId == userId && !r.IsResolved);
}
