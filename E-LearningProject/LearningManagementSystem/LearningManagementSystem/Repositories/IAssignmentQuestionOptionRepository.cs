using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Repositories
{
    public interface IAssignmentQuestionOptionRepository
    {
        IQueryable<AssignmentQuestionOption> GetAll();
        void Add(AssignmentQuestionOption option);
        void Update(AssignmentQuestionOption option);
        void Delete(string optionId);
        AssignmentQuestionOption GetById(string optionId);
        List<AssignmentQuestionOption> GetByQuestionId(string questionId);
        void Save();
        void DeleteRange(IEnumerable<string> optionIds);
    }
}