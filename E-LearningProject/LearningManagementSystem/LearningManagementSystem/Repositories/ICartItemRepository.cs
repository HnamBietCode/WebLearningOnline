using LearningManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningManagementSystem.Repositories
{
    public interface ICartItemRepository
    {
        Task<List<CartItem>> GetCartItemsByCartAsync(string cartId);
        Task<CartItem> GetCartItemByIdAsync(string cartItemId);
        Task<CartItem> GetCartItemByCartAndCourseAsync(string cartId, string courseId);
        Task AddCartItemAsync(CartItem cartItem);
        Task RemoveCartItemAsync(CartItem cartItem);
        Task ClearCartAsync(IEnumerable<CartItem> cartItems);
        Task SaveChangesAsync();
    }
}