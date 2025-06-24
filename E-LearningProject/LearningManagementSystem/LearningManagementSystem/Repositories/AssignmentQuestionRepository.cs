using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class AssignmentQuestionRepository : IAssignmentQuestionRepository
    {
        private readonly LMSContext _context;

        public AssignmentQuestionRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<AssignmentQuestion> GetAll()
        {
            return _context.AssignmentQuestions.AsQueryable();
        }
        public void Add(AssignmentQuestion question)
        {
            _context.AssignmentQuestions.Add(question);
        }

        public void Update(AssignmentQuestion question)
        {
            _context.AssignmentQuestions.Update(question);
        }

        public void Delete(string questionId)
        {
            var question = _context.AssignmentQuestions.Find(questionId);
            if (question != null)
            {
                _context.AssignmentQuestions.Remove(question);
            }
        }

        public AssignmentQuestion GetById(string questionId)
        {
            return _context.AssignmentQuestions
                .Include(q => q.Assignment)
                .Include(q => q.Options)
                .FirstOrDefault(q => q.QuestionId == questionId);
        }

        public List<AssignmentQuestion> GetByAssignmentId(string assignmentId)
        {
            return _context.AssignmentQuestions
                .Where(q => q.AssignmentId == assignmentId)
                .Include(q => q.Options)
                .OrderBy(q => q.OrderNumber)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}