using PIPDC.Application.Agents;
using PIPDC.Application.Auth;
using PIPDC.Application.Enquiries;
using PIPDC.Application.Properties;

namespace PIPDC.Application.Dashboard;

public record AdminDashboardDto(
    int TotalProperties,
    int TotalAgents,
    int TotalEnquiries,
    int TotalUsers,
    IReadOnlyList<PropertyDto> RecentProperties,
    IReadOnlyList<EnquiryDto> RecentEnquiries);

public record AgentDashboardDto(
    AgentDto Agent,
    int TotalProperties,
    IReadOnlyList<PropertyDto> RecentProperties,
    int TotalEnquiries,
    int PendingEnquiries,
    IReadOnlyList<EnquiryDto> RecentEnquiries);

public record ClientDashboardDto(
    CurrentUserDto Profile,
    int TotalSavedProperties,
    IReadOnlyList<PropertyDto> SavedProperties,
    int TotalEnquiries,
    int PendingEnquiries,
    IReadOnlyList<EnquiryDto> RecentEnquiries);
