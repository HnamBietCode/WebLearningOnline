using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningManagementSystem.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly LMSContext _context;

        public ProgressRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<Progress> GetAll()
        {
            return _context.Progresses
                .Include(p => p.User)
                .Include(p => p.Lesson)
                .ThenInclude(l => l.Course)
                .AsQueryable();
        }

        public Progress GetById(string id)
        {
            return _context.Progresses
                .Include(p => p.User)
                .Include(p => p.Lesson)
                .ThenInclude(l => l.Course)
                .FirstOrDefault(p => p.ProgressId == id);
        }

        public Progress GetByUserAndLesson(string userName, string lessonId)
        {
            return _context.Progresses
                .FirstOrDefault(p => p.UserName == userName && p.LessonId == lessonId);
        }

        public void Add(Progress progress)
        {
            _context.Progresses.Add(progress);
        }

        public void Update(Progress progress)
        {
            _context.Progresses.Update(progress);
        }

        public void Delete(string id)
        {
            var progress = GetById(id);
            if (progress != null)
            {
                _context.Progresses.Remove(progress);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
