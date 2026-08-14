using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PIPDC.Application.Auth;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Infrastructure.Data;

/// <summary>
/// Seeds demo content (admin, agents, properties, blog posts, enquiries) so the
/// frontend has data to render in development. Each section is idempotent: running
/// the application repeatedly must never create duplicate users, agents, properties,
/// blog posts or enquiries. Stable identifiers (email, slug) are used to detect
/// records that already exist.
/// </summary>
public static class DevelopmentSeeder
{
    private const string SeedAdminPasswordKey = "SeedAdminPassword";

    private sealed record AgentSeed(string First, string Last, string Email, string Title, string Phone, string Bio, string Photo);
    private sealed record PropertySeed(
        string Title, string Slug, string Desc, decimal Price, string? Period,
        PropertyType Type, ListingType Listing, int? Beds, int? Baths,
        double Size, double? Lot, int? Built, string Address, string City, string Area,
        string State, double Lat, double Lng, string[] Amenities, string Images,
        bool Featured, int Agent, int DaysAgo);
    private sealed record BlogSeed(string Title, string Slug, string Excerpt, string Content, string Cover, int DaysAgo);
    private sealed record EnquirySeed(string Name, string Email, string Phone, string Message, int PropertyIndex, EnquiryStatus Status, int DaysAgo);

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        var seedPassword = configuration[SeedAdminPasswordKey]
            ?? throw new InvalidOperationException(
                $"The configuration value '{SeedAdminPasswordKey}' is required to seed development data. " +
                $"Set it with user secrets, e.g.: dotnet user-secrets set \"{SeedAdminPasswordKey}\" \"<password>\"");

        // ---------------------------------------------------------------
        // Admin user
        // ---------------------------------------------------------------
        var admin = await EnsureUserAsync(userManager, seedPassword, "Admin", "User", "agbojisimon107@gmail.com", Roles.Admin);

        // ---------------------------------------------------------------
        // Agents
        // ---------------------------------------------------------------
        var agentSeed = new[]
        {
            new AgentSeed("Nankin", "Bagudu", "nankin.bagudu@pipdc.gov.ng", "Principal Property Consultant", "+234 803 555 0142",
                "Nankin leads the residential advisory desk at PIPDC, specialising in premium homes across Jos, Rayfield and Bukuru. Over a decade of experience guiding families and investors through verified transactions.",
                "https://images.pexels.com/photos/2182970/pexels-photo-2182970.jpeg?auto=compress&cs=tinysrgb&w=800"),
            new AgentSeed("Grace", "Ibrahim", "grace.ibrahim@pipdc.gov.ng", "Commercial Property Specialist", "+234 805 555 0178",
                "Grace advises businesses and institutions on commercial leasing and acquisition across Plateau State, with a focus on retail, office and mixed-use developments.",
                "https://images.pexels.com/photos/3760263/pexels-photo-3760263.jpeg?auto=compress&cs=tinysrgb&w=800"),
            new AgentSeed("Daniel", "Dachung", "daniel.dachung@pipdc.gov.ng", "Land & Investment Advisor", "+234 802 555 0193",
                "Daniel is PIPDC's lead land advisor, helping investors identify titled plots and agricultural holdings with clear documentation across the Plateau.",
                "https://images.pexels.com/photos/2379004/pexels-photo-2379004.jpeg?auto=compress&cs=tinysrgb&w=800"),
            new AgentSeed("Aisha", "Mohammed", "aisha.mohammed@pipdc.gov.ng", "Leasing & Rental Consultant", "+234 807 555 0124",
                "Aisha manages the leasing desk, connecting tenants with quality apartments and family homes across Jos metropolis and surrounding districts.",
                "https://images.pexels.com/photos/3727464/pexels-photo-3727464.jpeg?auto=compress&cs=tinysrgb&w=800"),
            new AgentSeed("Stephen", "Pam", "stephen.pam@pipdc.gov.ng", "Luxury Estates Manager", "+234 809 555 0166",
                "Stephen curates PIPDC's luxury portfolio, representing the finest estates and penthouses in Rayfield, Lamingo and the Jos Plateau highlands.",
                "https://images.pexels.com/photos/3785067/pexels-photo-3785067.jpeg?auto=compress&cs=tinysrgb&w=800"),
            new AgentSeed("Maryam", "Audu", "maryam.audu@pipdc.gov.ng", "First-Time Buyer Advisor", "+234 806 555 0188",
                "Maryam supports first-time buyers through every step of ownership, from documentation to financing referrals, with patience and clarity.",
                "https://images.pexels.com/photos/5905789/pexels-photo-5905789.jpeg?auto=compress&cs=tinysrgb&w=800"),
        };

        var agents = new List<Agent>();
        foreach (var seed in agentSeed)
        {
            var user = await EnsureUserAsync(userManager, seedPassword, seed.First, seed.Last, seed.Email, Roles.Agent);

            var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (agent is null)
            {
                agent = new Agent
                {
                    Bio = seed.Bio,
                    Title = seed.Title,
                    PhotoUrl = seed.Photo,
                    AgencyName = "PIPDC Official",
                    LicenseNumber = $"PIPDC-{seed.First[0]}{seed.Last[0]}-{new Random().Next(1000, 9999)}",
                    PhoneNumber = seed.Phone,
                    IsVerified = true,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                };
                dbContext.Agents.Add(agent);
            }

            agents.Add(agent);
        }

        await dbContext.SaveChangesAsync();

        // ---------------------------------------------------------------
        // Properties (port of the frontend mock catalogue)
        // ---------------------------------------------------------------
        var images = new Dictionary<string, string[]>
        {
            ["villa"] = ["https://images.pexels.com/photos/1396122/pexels-photo-1396122.jpeg?auto=compress&cs=tinysrgb&w=1200",
                         "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg?auto=compress&cs=tinysrgb&w=1200",
                         "https://images.pexels.com/photos/271639/pexels-photo-271639.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["house"] = ["https://images.pexels.com/photos/106399/pexels-photo-106399.jpeg?auto=compress&cs=tinysrgb&w=1200",
                         "https://images.pexels.com/photos/1571468/pexels-photo-1571468.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["penthouse"] = ["https://images.pexels.com/photos/323780/pexels-photo-323780.jpeg?auto=compress&cs=tinysrgb&w=1200",
                             "https://images.pexels.com/photos/164877/pexels-photo-164877.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["land"] = ["https://images.pexels.com/photos/1438832/pexels-photo-1438832.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["apartment"] = ["https://images.pexels.com/photos/259588/pexels-photo-259588.jpeg?auto=compress&cs=tinysrgb&w=1200",
                             "https://images.pexels.com/photos/1080721/pexels-photo-1080721.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["commercial"] = ["https://images.pexels.com/photos/2467558/pexels-photo-2467558.jpeg?auto=compress&cs=tinysrgb&w=1200",
                              "https://images.pexels.com/photos/271639/pexels-photo-271639.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["townhouse"] = ["https://images.pexels.com/photos/2102587/pexels-photo-2102587.jpeg?auto=compress&cs=tinysrgb&w=1200",
                             "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg?auto=compress&cs=tinysrgb&w=1200"],
            ["mansion"] = ["https://images.pexels.com/photos/32870/pexels-photo.jpg?auto=compress&cs=tinysrgb&w=1200",
                           "https://images.pexels.com/photos/271639/pexels-photo-271639.jpeg?auto=compress&cs=tinysrgb&w=1200",
                           "https://images.pexels.com/photos/1571468/pexels-photo-1571468.jpeg?auto=compress&cs=tinysrgb&w=1200"],
        };

        var now = DateTime.UtcNow;
        var propertySeed = new[]
        {
            new PropertySeed("Highland Villa with Panoramic Plateau Views", "highland-villa-rayfield",
                "A statement villa perched on the Rayfield highlands with uninterrupted views of the Jos Plateau. Designed for refined family living, the residence features expansive living spaces, a private garden, and a double garage. Verified title and documentation available.",
                185_000_000m, null, PropertyType.Villa, ListingType.ForSale, 6, 5, 520d, 1200d, 2022,
                "12 Highland Drive, Rayfield", "Jos", "Rayfield", "Plateau", 9.8965, 8.8924,
                ["Swimming Pool", "Garden", "Garage", "Air Conditioning", "Solar Power", "CCTV", "Borehole", "Furnished"],
                "villa", true, 5, 33),
            new PropertySeed("Contemporary Family Home in Lamingo", "contemporary-family-home-lamingo",
                "Modern family residence in a quiet, paved neighbourhood of Lamingo. Open-plan kitchen, en-suite bedrooms, and a landscaped backyard make this an ideal long-term home.",
                78_000_000m, null, PropertyType.DetachedHouse, ListingType.ForSale, 4, 3, 320d, 600d, 2021,
                "8 Congo Street, Lamingo", "Jos", "Lamingo", "Plateau", 9.9216, 8.9086,
                ["Garden", "Garage", "Air Conditioning", "Borehole", "Solar Power"],
                "house", true, 1, 35),
            new PropertySeed("Executive Penthouse in Jos City Centre", "executive-penthouse-jos-city-centre",
                "A turnkey penthouse with floor-to-ceiling glass, private terrace, and dedicated parking in the heart of Jos. Ideal for professionals and investors seeking rental yield.",
                120_000_000m, null, PropertyType.Penthouse, ListingType.ForSale, 3, 3, 280d, null, 2023,
                "Plateau Towers, Ahmadu Bello Way", "Jos", "City Centre", "Plateau", 9.9265, 8.8922,
                ["Air Conditioning", "Gym", "Concierge", "Parking", "Elevator", "Furnished"],
                "penthouse", true, 5, 31),
            new PropertySeed("Titled Land Parcel in Bukuru", "titled-land-bukuru",
                "A well-located, titled parcel of land suitable for residential development or long-term investment. All documentation verified by PIPDC.",
                35_000_000m, null, PropertyType.Land, ListingType.ForSale, null, null, 1500d, null, null,
                "Bukuru Layout, Jos South", "Jos", "Bukuru", "Plateau", 9.8465, 8.8724,
                ["Fenced", "Titled", "Borehole Ready", "Paved Access"],
                "land", false, 3, 38),
            new PropertySeed("Luxury Apartment for Lease in Rayfield", "luxury-apartment-lease-rayfield",
                "Fully furnished apartment available for lease in a serviced estate. Includes 24/7 security, backup power, and access to shared amenities.",
                4_500_000m, "/ year", PropertyType.Apartment, ListingType.ForLease, 3, 2, 180d, null, 2022,
                "Rayfield Gardens Estate, Rayfield", "Jos", "Rayfield", "Plateau", 9.8965, 8.8924,
                ["Furnished", "Air Conditioning", "Gym", "Swimming Pool", "Security", "Backup Power"],
                "apartment", true, 4, 32),
            new PropertySeed("Commercial Retail Space on Ahmadu Bello Way", "commercial-retail-ahmadu-bello",
                "Ground-floor retail space on Jos's busiest commercial corridor. High foot traffic, ample parking, and flexible fit-out options.",
                18_000_000m, "/ year", PropertyType.Commercial, ListingType.ForLease, null, 2, 240d, null, null,
                "47 Ahmadu Bello Way, Jos", "Jos", "City Centre", "Plateau", 9.9265, 8.8922,
                ["Parking", "Security", "Air Conditioning", "Loading Bay"],
                "commercial", false, 2, 39),
            new PropertySeed("Elegant Townhouse in Rayfield Estate", "elegant-townhouse-rayfield",
                "A smartly designed townhouse in a gated estate with shared security and green spaces. Perfect for young families and professionals.",
                95_000_000m, null, PropertyType.Townhouse, ListingType.ForSale, 4, 4, 260d, null, 2023,
                "10 Cedar Close, Rayfield Estate", "Jos", "Rayfield", "Plateau", 9.8965, 8.8924,
                ["Garden", "Garage", "Air Conditioning", "Security", "Borehole"],
                "townhouse", false, 1, 41),
            new PropertySeed("Mansion with Guest Wing in Lamingo", "mansion-guest-wing-lamingo",
                "A grand mansion with separate guest wing, manicured grounds, and staff quarters. One of the finest residences currently available on the Plateau.",
                320_000_000m, null, PropertyType.Mansion, ListingType.ForSale, 8, 7, 780d, 2000d, 2024,
                "1 Hilltop Road, Lamingo", "Jos", "Lamingo", "Plateau", 9.9216, 8.9086,
                ["Swimming Pool", "Garden", "Garage", "Air Conditioning", "Solar Power", "CCTV", "Borehole", "Staff Quarters", "Guest Wing"],
                "mansion", true, 5, 34),
            new PropertySeed("Modern Apartment in Bukuru", "modern-apartment-bukuru",
                "Affordable modern apartment close to schools and markets. A solid entry point for first-time buyers and investors.",
                32_000_000m, null, PropertyType.Apartment, ListingType.ForSale, 2, 2, 95d, null, 2020,
                "Block C, Bukuru Heights", "Jos", "Bukuru", "Plateau", 9.8465, 8.8724,
                ["Parking", "Security", "Borehole"],
                "apartment", false, 6, 42),
            new PropertySeed("Semi-Detached Home in Jos North", "semi-detached-jos-north",
                "A well-maintained semi-detached home in a family-friendly neighbourhood with easy access to schools and places of worship.",
                58_000_000m, null, PropertyType.SemiDetached, ListingType.ForSale, 3, 2, 210d, null, 2019,
                "5 Tenti Road, Jos North", "Jos", "Jos North", "Plateau", 9.9565, 8.8822,
                ["Garage", "Borehole", "Garden"],
                "house", false, 1, 45),
            new PropertySeed("Terrace Duplex in Rayfield Gardens", "terrace-duplex-rayfield-gardens",
                "Brand-new terrace duplex with contemporary finishes, fitted kitchen, and a private backyard. Ready to move in.",
                88_000_000m, null, PropertyType.Terrace, ListingType.ForSale, 4, 3, 240d, null, 2024,
                "Plot 14, Rayfield Gardens", "Jos", "Rayfield", "Plateau", 9.8965, 8.8924,
                ["Fitted Kitchen", "Air Conditioning", "Security", "Parking"],
                "penthouse", false, 4, 47),
            new PropertySeed("Office Suite for Lease in City Centre", "office-suite-lease-city-centre",
                "Professional office suite with partitioned rooms, server area, and dedicated parking. Suitable for SMEs and consultancies.",
                12_000_000m, "/ year", PropertyType.Commercial, ListingType.ForLease, null, 2, 180d, null, null,
                "2nd Floor, Plateau House, Jos", "Jos", "City Centre", "Plateau", 9.9265, 8.8922,
                ["Parking", "Air Conditioning", "Elevator", "Security", "Backup Power"],
                "commercial", false, 2, 49),
        };

        var properties = new List<Property>();
        foreach (var seed in propertySeed)
        {
            var agent = agents[seed.Agent - 1];

            var property = await dbContext.Properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Slug == seed.Slug);

            if (property is null)
            {
                property = new Property
                {
                    Title = seed.Title,
                    Slug = seed.Slug,
                    Description = seed.Desc,
                    Price = seed.Price,
                    Currency = "NGN",
                    Period = seed.Period,
                    Status = PropertyStatus.Available,
                    PropertyType = seed.Type,
                    ListingType = seed.Listing,
                    Bedrooms = seed.Beds,
                    Bathrooms = seed.Baths,
                    Size = seed.Size,
                    SizeUnit = "sqm",
                    LotSize = seed.Lot,
                    YearBuilt = seed.Built,
                    Address = seed.Address,
                    State = seed.State,
                    City = seed.City,
                    Area = seed.Area,
                    Latitude = seed.Lat,
                    Longitude = seed.Lng,
                    Amenities = seed.Amenities.ToList(),
                    Featured = seed.Featured,
                    AgentId = agent.Id,
                    CreatedByUserId = admin.Id,
                    CreatedAt = now.AddDays(-seed.DaysAgo)
                };

                var urls = images[seed.Images];
                for (var i = 0; i < urls.Length; i++)
                {
                    property.PropertyImages.Add(new PropertyImage
                    {
                        Url = urls[i],
                        PublicId = $"seed/{seed.Slug}-{i}",
                        DisplayOrder = i,
                        IsCover = i == 0
                    });
                }

                dbContext.Properties.Add(property);
            }

            properties.Add(property);
        }

        await dbContext.SaveChangesAsync();

        // ---------------------------------------------------------------
        // Blog posts
        // ---------------------------------------------------------------
        var blogSeed = new[]
        {
            new BlogSeed("Understanding Land Titling in Plateau State", "understanding-land-titling-plateau-state",
                "A practical guide to C of O, R of O and the documentation that protects your property investment on the Plateau.",
                "Land documentation is the foundation of any secure property transaction. This guide walks through the Certificate of Occupancy, Right of Occupancy and the verification process used by PIPDC to protect buyers.",
                "https://images.pexels.com/photos/1396122/pexels-photo-1396122.jpeg?auto=compress&cs=tinysrgb&w=1200", 31),
            new BlogSeed("Why Rayfield Is Jos's Most Sought-After Neighbourhood", "why-rayfield-most-sought-after-neighbourhood",
                "From highland views to gated estates, here is what makes Rayfield a premium address for families and investors.",
                "Rayfield combines altitude, infrastructure and security to create one of the most desirable residential addresses in Plateau State.",
                "https://images.pexels.com/photos/1438832/pexels-photo-1438832.jpeg?auto=compress&cs=tinysrgb&w=1200", 35),
            new BlogSeed("A First-Time Buyer's Checklist for Plateau State", "first-time-buyer-checklist-plateau",
                "Ten things every first-time buyer should verify before making an offer on a property in Plateau State.",
                "Buying your first home is a milestone. This checklist helps you approach the process with confidence and clarity.",
                "https://images.pexels.com/photos/259588/pexels-photo-259588.jpeg?auto=compress&cs=tinysrgb&w=1200", 40),
            new BlogSeed("Investing in Commercial Real Estate in Jos", "investing-commercial-real-estate-jos",
                "Opportunities, risks and the corridors where commercial property is delivering consistent yields.",
                "Commercial real estate in Jos is evolving. We look at the corridors delivering the most consistent yields and what to look for as an investor.",
                "https://images.pexels.com/photos/2102587/pexels-photo-2102587.jpeg?auto=compress&cs=tinysrgb&w=1200", 45),
        };

        foreach (var seed in blogSeed)
        {
            if (await dbContext.BlogPosts.AnyAsync(b => b.Slug == seed.Slug))
                continue;

            dbContext.BlogPosts.Add(new BlogPost
            {
                Title = seed.Title,
                Slug = seed.Slug,
                Excerpt = seed.Excerpt,
                Content = seed.Content,
                CoverImageUrl = seed.Cover,
                Status = BlogPostStatus.Published,
                PublishedAt = now.AddDays(-seed.DaysAgo),
                CreatedAt = now.AddDays(-seed.DaysAgo)
            });
        }

        // ---------------------------------------------------------------
        // Enquiries
        // ---------------------------------------------------------------
        var enquirySeed = new[]
        {
            new EnquirySeed("Chuwang Bala", "chuwang.bala@example.com", "+234 803 000 1122",
                "I would like to schedule a viewing for the Highland Villa in Rayfield.", 0, EnquiryStatus.Pending, 24),
            new EnquirySeed("Patience Okon", "patience.okon@example.com", "+234 805 000 3344",
                "Is the Rayfield apartment available for a 2-year lease?", 4, EnquiryStatus.Pending, 25),
            new EnquirySeed("Yusuf Adamu", "yusuf.adamu@example.com", "+234 802 000 5566",
                "Please share documentation for the Bukuru land parcel.", 3, EnquiryStatus.InProgress, 26),
            new EnquirySeed("Hassana Idris", "hassana.idris@example.com", "+234 807 000 7788",
                "Looking for a 3-bedroom apartment under N40M in Jos North.", 8, EnquiryStatus.Resolved, 28),
        };

        foreach (var seed in enquirySeed)
        {
            if (await dbContext.Enquiries.AnyAsync(e => e.Email == seed.Email))
                continue;

            dbContext.Enquiries.Add(new Enquiry
            {
                FullName = seed.Name,
                Email = seed.Email,
                Phone = seed.Phone,
                Message = seed.Message,
                PropertyId = properties[seed.PropertyIndex].Id,
                Status = seed.Status,
                CreatedAt = now.AddDays(-seed.DaysAgo)
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string seedPassword,
        string firstName,
        string lastName,
        string email,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            var result = await userManager.CreateAsync(user, seedPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }
}
