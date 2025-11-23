using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.Models;

public class HomeViewModel
{
    public IEnumerable<BranchDto> Branches { get; set; } = new List<BranchDto>();
    public IEnumerable<ServiceDto> Services { get; set; } = new List<ServiceDto>();
}
