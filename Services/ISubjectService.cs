using BusinessObjects;
using System.Collections.Generic;

namespace Services
{
    public interface ISubjectService
    {
        IEnumerable<Subject> GetAllSubjects();
        Subject GetSubjectById(int id);
        void AddSubject(Subject subject);
        void UpdateSubject(Subject subject);
        void DeleteSubject(Subject subject);
    }
}