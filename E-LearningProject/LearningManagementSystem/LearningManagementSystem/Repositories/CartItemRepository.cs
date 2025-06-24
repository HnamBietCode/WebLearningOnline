using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningManagementSystem.Repositories
{
    public class CartItemRepository : ICartItemRepository
    {
        private readonly LMSContext _context;

        public CartItemRepository(LMSContext context)
        {
            _context = context;
        }

        public async Task<List<CartItem>> GetCartItemsByCartAsync(string cartId)
        {
            return await _context.CartItems
                .Include(ci => ci.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role)
                .Include(ci => ci.Cart)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        public async Task<CartItem> GetCartItemByIdAsync(string cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role)
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task<CartItem> GetCartItemByCartAndCourseAsync(string cartId, string courseId)
        {
            return await _context.CartItems
                .Include(ci => ci.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role)
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.CourseId == courseId);
        }

        public async Task AddCartItemAsync(CartItem cartItem)
        {
            await _context.CartItems.AddAsync(cartItem);
        }

        public async Task RemoveCartItemAsync(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
            await Task.CompletedTask;
        }

        public async Task ClearCartAsync(IEnumerable<CartItem> cartItems)
        {
            _context.CartItems.RemoveRange(cartItems);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}