namespace AiClinic.Application.DTOs;

public record DoctorDto(
    Guid Id,
    string FullName,
    string Specialization,
    string Organization,
    string Status,
    decimal Rating,
    int TotalConsultations
);

public record UpdateDoctorStatusRequest(
    Guid DoctorId,
    string Status
);
