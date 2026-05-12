namespace Aura.Application.Profile.Common;

public record InterestCategoryDto(string Category, List<InterestItemDto> Items);
public record InterestItemDto(string Slug, string Label, int DisplayOrder);
