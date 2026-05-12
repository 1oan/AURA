using FluentValidation;

namespace Aura.Application.Profile.Commands.ExchangeSpotifyCode;

public class ExchangeSpotifyCodeCommandValidator : AbstractValidator<ExchangeSpotifyCodeCommand>
{
    public ExchangeSpotifyCodeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.RedirectUri).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
    }
}
