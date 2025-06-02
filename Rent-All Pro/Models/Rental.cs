using System;
using System.ComponentModel.DataAnnotations;
namespace RentAllPro.Models
{
    public class Rental
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }

        [Required]
        public int RentalDays { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property - kapcsolat a Customer táblával
        public Customer Customer { get; set; } = new Customer();
    }
}