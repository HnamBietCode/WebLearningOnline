using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly LMSContext _context;

        public CommentRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<Comment> GetAll()
        {
            return _context.Comments
                .Include(c => c.User)
                .Include(c => c.Course)
                .AsQueryable();
        }

        public Comment GetById(string id)
        {
            return _context.Comments
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefault(c => c.CommentId == id);
        }

        public void Add(Comment comment)
        {
            _context.Comments.Add(comment);
        }

        public void Update(Comment comment)
        {
            _context.Comments.Update(comment);
        }

        public void Delete(string id)
        {
            var comment = GetById(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}