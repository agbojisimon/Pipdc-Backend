using PIPDC.Domain.Entities;

namespace PIPDC.Application.Agents;

public static class AgentMappers
{
    public static AgentDto ToDto(this Agent agent, int propertyCount) =>
        new(
            agent.Id,
            agent.Bio,
            agent.Title,
            agent.PhotoUrl,
            agent.PhotoPublicId,
            agent.AgencyName,
            agent.LicenseNumber,
            agent.PhoneNumber,
            agent.IsVerified,
            agent.User.FullName,
            agent.UserId,
            agent.User.Email!,
            agent.User.FirstName,
            agent.User.LastName,
            agent.CreatedAt,
            agent.UpdatedAt,
            propertyCount);
}
