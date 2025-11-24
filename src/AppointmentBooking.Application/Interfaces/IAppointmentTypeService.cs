using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IAppointmentTypeService
{
    Task<IEnumerable<AppointmentTypeDto>> GetAllAsync();
    Task<AppointmentTypeDto?> GetByIdAsync(int id);
}
