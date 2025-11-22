namespace AppointmentBooking.Core.Entities;

public class BranchService
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
