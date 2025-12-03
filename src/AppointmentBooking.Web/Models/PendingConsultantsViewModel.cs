namespace AppointmentBooking.Web.Models;

public class PendingConsultantViewModel
{
    public int ConsultantId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
}

public class PendingConsultantsViewModel
{
    public List<PendingConsultantViewModel> PendingConsultants { get; set; } = new();
}
