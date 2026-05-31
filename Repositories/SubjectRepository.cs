using BusinessObjects;
using DataAccessObjects;
using System.Collections.Generic;
using System.Linq;

namespace Repositories
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly QuizManagementDbContext _context;

        // Tiêm DbContext vào (Khắc phục hoàn toàn lỗi Pseudo-DI)
        public SubjectRepository(QuizManagementDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Subject> GetAllSubjects() => _context.Subjects.ToList();

        public Subject GetSubjectById(int id) => _context.Subjects.FirstOrDefault(s => s.Id == id)!;

        public void AddSubject(Subject subject)
        {
            _context.Subjects.Add(subject);
            _context.SaveChanges();
        }

        public void UpdateSubject(Subject subject)
        {
            _context.Subjects.Update(subject);
            _context.SaveChanges();
        }

        public void DeleteSubject(Subject subject)
        {
            _context.Subjects.Remove(subject);
            _context.SaveChanges();
        }
    }
}