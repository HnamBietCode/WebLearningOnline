using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Repositories
{
    public interface IAssignmentSubmissionRepository
    {
        void Add(AssignmentSubmission submission);
        void Update(AssignmentSubmission submission);
        void Delete(string submissionId);
        AssignmentSubmission GetById(string submissionId);
        List<AssignmentSubmission> GetByAssignmentId(string assignmentId);
        void Save();
    }
}