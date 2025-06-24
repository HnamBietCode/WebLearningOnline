namespace LearningManagementSystem.Models.ViewModels
{
    public class CartItemViewModel
    {
        public CartItem CartItem { get; set; }
        public string InstructorName { get; set; }

        public CourseListViewModel Course { get; set; }
    }
}