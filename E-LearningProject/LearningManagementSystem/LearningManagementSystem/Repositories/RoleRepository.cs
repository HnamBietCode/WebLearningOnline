using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly LMSContext _context;

        public RoleRepository(LMSContext context)
        {
            _context = context;
        }

        public IEnumerable<Role> GetAll()
        {
            return _context.Roles.ToList();
        }

        public Role GetByName(string name)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleName == name);
        }

        public Role GetById(string id)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleId == id);
        }

        public void Add(Role role)
        {
            _context.Roles.Add(role);
        }

        public void Update(Role role)
        {
            _context.Roles.Update(role);
        }

        public void Delete(string id)
        {
            var role = GetById(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}