using System.Text.RegularExpressions;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class Interest
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Interest() { }

    public static Interest Create(Guid id, string slug, string label, string category, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Interest slug is required.");
        if (!SlugPattern.IsMatch(slug))
            throw new DomainException("Interest slug must be lowercase kebab-case (e.g. 'board-games').");
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Interest label is required.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Interest category is required.");

        return new Interest
        {
            Id = id,
            Slug = slug,
            Label = label.Trim(),
            Category = category.Trim().ToLowerInvariant(),
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
