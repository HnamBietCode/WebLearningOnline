using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Repositories
{
    public interface IAssignmentRepository
    {
        IQueryable<Assignment> GetAll();
        void Add(Assignment assignment);
        void Update(Assignment assignment);
        void Delete(string assignmentId);
        Assignment GetById(string assignmentId);
        List<Assignment> GetByLessonId(string lessonId);
        void Save();
    }
}