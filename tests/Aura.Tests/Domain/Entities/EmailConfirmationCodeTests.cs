using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class EmailConfirmationCodeTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidCode = "123456";

    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsUnusedCodeWithExpiry()
    {
        var before = DateTime.UtcNow;
        var code = EmailConfirmationCode.Create(ValidUserId, ValidCode);
        var after = DateTime.UtcNow;

        code.Id.Should().NotBe(Guid.Empty);
        code.UserId.Should().Be(ValidUserId);
        code.Code.Should().Be(ValidCode);
        code.IsUsed.Should().BeFalse();
        code.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        code.ExpiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithCustomExpiryMinutes_SetsCorrectExpiry()
    {
        var before = DateTime.UtcNow;

        var code = EmailConfirmationCode.Create(ValidUserId, ValidCode, expiryMinutes: 30);

        code.ExpiresAt.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        var act = () => EmailConfirmationCode.Create(Guid.Empty, ValidCode);

        act.Should().Throw<DomainException>().WithMessage("User ID is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("   ")]
    public void Create_WithWrongCodeLength_ThrowsDomainException(string code)
    {
        var act = () => EmailConfirmationCode.Create(ValidUserId, code);

        act.Should().Throw<DomainException>().WithMessage("Code must be exactly 6 characters.");
    }

    // ─── MarkAsUsed() ────────────────────────────────────────────────────

    [Fact]
    public void MarkAsUsed_WhenValidAndUnused_SetsIsUsedToTrue()
    {
        var code = EmailConfirmationCode.Create(ValidUserId, ValidCode);

        code.MarkAsUsed();

        code.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_WhenAlreadyUsed_ThrowsDomainException()
    {
        var code = EmailConfirmationCode.Create(ValidUserId, ValidCode);
        code.MarkAsUsed();

        var act = () => code.MarkAsUsed();

        act.Should().Throw<DomainException>().WithMessage("Confirmation code has already been used.");
    }

    [Fact]
    public void MarkAsUsed_WhenExpired_ThrowsDomainException()
    {
        // expiryMinutes: -1 means already expired
        var code = EmailConfirmationCode.Create(ValidUserId, ValidCode, expiryMinutes: -1);

        var act = () => code.MarkAsUsed();

        act.Should().Throw<DomainException>().WithMessage("Confirmation code has expired.");
    }
}
