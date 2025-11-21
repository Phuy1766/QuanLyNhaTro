namespace QuanLyNhaTro.DAL.Models
{
    public class Building
    {
        public int BuildingId { get; set; }
        public string BuildingCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int TotalFloors { get; set; } = 1;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Calculated properties
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
    }
}
