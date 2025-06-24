using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class AssignmentSubmissionRepository : IAssignmentSubmissionRepository
    {
        private readonly LMSContext _context;

        public AssignmentSubmissionRepository(LMSContext context)
        {
            _context = context;
        }

        public void Add(AssignmentSubmission submission)
        {
            _context.AssignmentSubmissions.Add(submission);
        }

        public void Update(AssignmentSubmission submission)
        {
            _context.AssignmentSubmissions.Update(submission);
        }

        public void Delete(string submissionId)
        {
            var submission = _context.AssignmentSubmissions.Find(submissionId);
            if (submission != null)
            {
                _context.AssignmentSubmissions.Remove(submission);
            }
        }

        public AssignmentSubmission GetById(string submissionId)
        {
            return _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Question)
                .Include(s => s.User)
                .FirstOrDefault(s => s.SubmissionId == submissionId);
        }

        public List<AssignmentSubmission> GetByAssignmentId(string assignmentId)
        {
            return _context.AssignmentSubmissions
                .Where(s => s.AssignmentId == assignmentId)
                .Include(s => s.User)
                .Include(s => s.Question)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}