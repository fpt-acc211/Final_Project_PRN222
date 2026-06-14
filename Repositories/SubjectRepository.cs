using BusinessObjects;
using DataAccessObjects;
using System.Collections.Generic;

namespace Repositories
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly QuizManagementDbContext _context;

        public SubjectRepository(QuizManagementDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Subject> GetSubjectsByUserId(string userId)
            => SubjectDAO.Instance.GetSubjectsByUserId(_context, userId);

        public Subject? GetSubjectById(int id, string userId)
            => SubjectDAO.Instance.GetSubjectById(_context, id, userId);

        public bool NameExists(string userId, string name, int? excludedId = null)
            => SubjectDAO.Instance.NameExists(_context, userId, name, excludedId);

        public void AddSubject(Subject subject)
            => SubjectDAO.Instance.AddSubject(_context, subject);

        public void UpdateSubject(Subject subject)
            => SubjectDAO.Instance.UpdateSubject(_context, subject);

        public void DeleteSubject(Subject subject)
            => SubjectDAO.Instance.DeleteSubject(_context, subject);
    }
}
