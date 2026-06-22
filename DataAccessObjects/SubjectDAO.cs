using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessObjects
{
    public class SubjectDAO
    {
        private static SubjectDAO? instance;
        private static readonly object instanceLock = new();

        private SubjectDAO()
        {
        }

        public static SubjectDAO Instance
        {
            get
            {
                lock (instanceLock)
                {
                    instance ??= new SubjectDAO();
                    return instance;
                }
            }
        }

        public IEnumerable<Subject> GetSubjectsByUserId(QuizManagementDbContext context, string userId)
        {
            return context.Subjects
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.Name)
                .ToList();
        }

        public IEnumerable<Subject> GetAllSubjects(QuizManagementDbContext context)
        {
            return context.Subjects
                .OrderBy(s => s.Name)
                .ThenBy(s => s.CreatedAt)
                .ToList();
        }

        public Subject? GetSubjectForStudy(QuizManagementDbContext context, int id)
        {
            return context.Subjects.FirstOrDefault(s => s.Id == id);
        }

        public Subject? GetSubjectById(
            QuizManagementDbContext context, int id, string userId, bool allowAll = false)
        {
            return context.Subjects.FirstOrDefault(s =>
                s.Id == id && (allowAll || s.UserId == userId));
        }

        public bool NameExists(QuizManagementDbContext context, string userId, string name, int? excludedId = null)
        {
            return context.Subjects.Any(s =>
                s.UserId == userId &&
                s.Name == name &&
                (!excludedId.HasValue || s.Id != excludedId.Value));
        }

        public void AddSubject(QuizManagementDbContext context, Subject subject)
        {
            subject.CreatedAt = DateTime.UtcNow;
            context.Subjects.Add(subject);
            context.SaveChanges();
        }

        public void UpdateSubject(QuizManagementDbContext context, Subject subject)
        {
            subject.UpdatedAt = DateTime.UtcNow;
            context.Subjects.Update(subject);
            context.SaveChanges();
        }

        public void DeleteSubject(QuizManagementDbContext context, Subject subject)
        {
            subject.IsDeleted = true;
            subject.UpdatedAt = DateTime.UtcNow;

            foreach (var deck in context.Decks.Where(d => d.SubjectId == subject.Id))
            {
                deck.IsDeleted = true;
                deck.UpdatedAt = DateTime.UtcNow;

                foreach (var question in context.Questions.Where(q => q.DeckId == deck.Id))
                {
                    question.IsDeleted = true;
                    question.UpdatedAt = DateTime.UtcNow;
                }
            }

            context.Subjects.Update(subject);
            context.SaveChanges();
        }
    }
}
