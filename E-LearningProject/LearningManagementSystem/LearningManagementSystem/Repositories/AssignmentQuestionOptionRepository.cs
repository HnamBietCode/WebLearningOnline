using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class AssignmentQuestionOptionRepository : IAssignmentQuestionOptionRepository
    {
        private readonly LMSContext _context;

        public AssignmentQuestionOptionRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<AssignmentQuestionOption> GetAll()
        {
            return _context.AssignmentQuestionOptions.AsQueryable();
        }
        public void Add(AssignmentQuestionOption option)
        {
            _context.AssignmentQuestionOptions.Add(option);
        }

        public void Update(AssignmentQuestionOption option)
        {
            _context.AssignmentQuestionOptions.Update(option);
        }

        public void Delete(string optionId)
        {
            var option = _context.AssignmentQuestionOptions.Find(optionId);
            if (option != null)
            {
                _context.AssignmentQuestionOptions.Remove(option);
            }
        }

        public AssignmentQuestionOption GetById(string optionId)
        {
            return _context.AssignmentQuestionOptions
                .Include(o => o.Question)
                .FirstOrDefault(o => o.OptionId == optionId);
        }

        public List<AssignmentQuestionOption> GetByQuestionId(string questionId)
        {
            return _context.AssignmentQuestionOptions
                .Where(o => o.QuestionId == questionId)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public void DeleteRange(IEnumerable<string> optionIds)
        {
            var options = _context.AssignmentQuestionOptions
                .Where(o => optionIds.Contains(o.OptionId))
                .ToList();
            _context.AssignmentQuestionOptions.RemoveRange(options);
        }
    }
}