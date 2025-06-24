using System.Collections.Generic;
using LearningManagementSystem.Models;

namespace LearningManagementSystem.Models.ViewModels
{
    public class PaymentDetailViewModel
    {
        public Payment Payment { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }

        public string SuccessMessage { get; set; }
    }
}