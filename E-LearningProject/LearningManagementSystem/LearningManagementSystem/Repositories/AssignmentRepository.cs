using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly LMSContext _context;

        public AssignmentRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<Assignment> GetAll()
        {
            return _context.Assignments.AsQueryable();
        }

        public void Add(Assignment assignment)
        {
            _context.Assignments.Add(assignment);
        }

        public void Update(Assignment assignment)
        {
            _context.Assignments.Update(assignment);
        }

        public void Delete(string assignmentId)
        {
            var assignment = _context.Assignments.Find(assignmentId);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
            }
        }

        public Assignment GetById(string assignmentId)
        {
            return _context.Assignments
                .Include(a => a.Lesson)
                .Include(a => a.Course)
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefault(a => a.AssignmentId == assignmentId);
        }

        public List<Assignment> GetByLessonId(string lessonId)
        {
            return _context.Assignments
                .Where(a => a.LessonId == lessonId)
                .Include(a => a.Lesson)
                .Include(a => a.Course)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}