using Microsoft.Extensions.DependencyInjection;
using PIPDC.Application.Agents;
using PIPDC.Application.Blog;
using PIPDC.Application.Conversations;
using PIPDC.Application.Dashboard;
using PIPDC.Application.Enquiries;
using PIPDC.Application.Properties;
using PIPDC.Application.SavedProperties;
using PIPDC.Application.Users;

namespace PIPDC.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IEnquiryService, EnquiryService>();
        services.AddScoped<ISavedPropertyService, SavedPropertyService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
