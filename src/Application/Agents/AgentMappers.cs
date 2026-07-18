using PIPDC.Domain.Entities;

namespace PIPDC.Application.Agents;

public static class AgentMappers
{
    public static AgentDto ToDto(this Agent agent) =>
        new(
            agent.Id,
            agent.Bio,
            agent.AgencyName,
            agent.LicenseNumber,
            agent.PhoneNumber,
            agent.IsVerified,
            agent.UserId,
            agent.User.Email!,
            agent.User.FirstName,
            agent.User.LastName,
            agent.CreatedAt,
            agent.UpdatedAt);
}
