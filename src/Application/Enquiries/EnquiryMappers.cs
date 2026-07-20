using PIPDC.Domain.Entities;

namespace PIPDC.Application.Enquiries;

public static class EnquiryMappers
{
    public static EnquiryDto ToDto(this Enquiry enquiry) =>
        new(
            enquiry.Id,
            enquiry.FullName,
            enquiry.Email,
            enquiry.Phone,
            enquiry.Message,
            enquiry.Status.ToString(),
            enquiry.PropertyId,
            enquiry.Property.Title,
            enquiry.UserId,
            enquiry.CreatedAt,
            enquiry.UpdatedAt);
}
