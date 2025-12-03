using AutoMapper;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Mappings;

/// <summary>
/// AutoMapper configuration for entity-to-DTO mappings
/// </summary>
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Appointment mappings
        CreateMap<Appointment, AppointmentDto>();
        CreateMap<CreateAppointmentDto, Appointment>();
        CreateMap<UpdateAppointmentDto, Appointment>();

        // Branch mappings
        CreateMap<Branch, BranchDto>();
        CreateMap<CreateBranchDto, Branch>();
        CreateMap<BranchDto, Branch>();

        // Service mappings
        CreateMap<Service, ServiceDto>();
        CreateMap<ServiceDto, Service>();

        // Customer mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<CustomerDto, Customer>();

        // Consultant mappings
        CreateMap<Consultant, ConsultantDto>()
            .ForMember(dest => dest.BranchName, 
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : string.Empty));
        CreateMap<ConsultantDto, Consultant>();
    }
}
