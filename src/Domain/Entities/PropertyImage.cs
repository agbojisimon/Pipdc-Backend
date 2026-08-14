using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class PropertyImage : BaseEntity
{
    // Images are hosted on Cloudinary; Url is the public link and PublicId is used for updates/deletes.
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public bool IsCover { get; set; }
    public int DisplayOrder { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}
