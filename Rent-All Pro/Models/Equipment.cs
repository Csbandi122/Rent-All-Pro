using System;
using System.ComponentModel.DataAnnotations;

namespace RentAllPro.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public decimal Value { get; set; }

        [Required]
        public decimal DailyRate { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}