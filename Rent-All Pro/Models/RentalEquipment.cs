namespace RentAllPro.Models
{
    public class RentalEquipment
    {
        public int Id { get; set; }
        public int RentalId { get; set; }
        public int EquipmentId { get; set; }

        // Navigation properties
        public Rental Rental { get; set; } = new Rental();
        public Equipment Equipment { get; set; } = new Equipment();
    }
}