using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class InterestTests
{
    [Fact]
    public void Create_ValidArguments_Succeeds()
    {
        var id = Guid.NewGuid();
        var interest = Interest.Create(id, "football", "Football", "sports", 1);

        interest.Id.Should().Be(id);
        interest.Slug.Should().Be("football");
        interest.Label.Should().Be("Football");
        interest.Category.Should().Be("sports");
        interest.DisplayOrder.Should().Be(1);
        interest.IsActive.Should().BeTrue();
        interest.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankSlug_ThrowsDomainException(string? slug)
    {
        var act = () => Interest.Create(Guid.NewGuid(), slug!, "Label", "sports", 1);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Theory]
    [InlineData("Football")]      // uppercase
    [InlineData("foot ball")]     // space
    [InlineData("foot_ball")]     // underscore
    [InlineData("foot.ball")]     // period
    public void Create_InvalidSlugFormat_ThrowsDomainException(string slug)
    {
        var act = () => Interest.Create(Guid.NewGuid(), slug, "Label", "sports", 1);
        act.Should().Throw<DomainException>().WithMessage("*kebab*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankLabel_ThrowsDomainException(string label)
    {
        var act = () => Interest.Create(Guid.NewGuid(), "football", label, "sports", 1);
        act.Should().Throw<DomainException>().WithMessage("*label*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCategory_ThrowsDomainException(string category)
    {
        var act = () => Interest.Create(Guid.NewGuid(), "football", "Football", category, 1);
        act.Should().Throw<DomainException>().WithMessage("*category*");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var interest = Interest.Create(Guid.NewGuid(), "football", "Football", "sports", 1);

        interest.Deactivate();

        interest.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_AfterDeactivate_SetsIsActiveTrue()
    {
        var interest = Interest.Create(Guid.NewGuid(), "football", "Football", "sports", 1);
        interest.Deactivate();

        interest.Reactivate();

        interest.IsActive.Should().BeTrue();
    }
}
