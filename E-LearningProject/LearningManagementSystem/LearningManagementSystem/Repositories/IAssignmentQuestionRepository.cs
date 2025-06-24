using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Repositories
{
    public interface IAssignmentQuestionRepository
    {
        IQueryable<AssignmentQuestion> GetAll();
        void Add(AssignmentQuestion question);
        void Update(AssignmentQuestion question);
        void Delete(string questionId);
        AssignmentQuestion GetById(string questionId);
        List<AssignmentQuestion> GetByAssignmentId(string assignmentId);
        void Save();
    }
}