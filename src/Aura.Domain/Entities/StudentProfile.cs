using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Microsoft.AspNetCore.DataProtection;

namespace Aura.Domain.Entities;

public class StudentProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public SleepSchedule? SleepSchedule { get; private set; }
    public WakeUpTime? WakeUpTime { get; private set; }
    public int? CleanlinessLevel { get; private set; }
    public NoiseTolerance? NoiseTolerance { get; private set; }
    public StudyLocation? StudyLocation { get; private set; }
    public GuestFrequency? GuestFrequency { get; private set; }
    public SmokingHabit? SmokingHabit { get; private set; }
    public DateTime? LifestyleCompletedAt { get; private set; }

    public int[] TipiAnswers { get; private set; } = Array.Empty<int>();
    public decimal? TipiExtraversion { get; private set; }
    public decimal? TipiAgreeableness { get; private set; }
    public decimal? TipiConscientiousness { get; private set; }
    public decimal? TipiEmotionalStability { get; private set; }
    public decimal? TipiOpenness { get; private set; }
    public DateTime? TipiCompletedAt { get; private set; }

    public string[] InterestSlugs { get; private set; } = Array.Empty<string>();
    public DateTime? InterestsCompletedAt { get; private set; }

    public string? SpotifyAccessToken { get; private set; }
    public string? SpotifyRefreshToken { get; private set; }
    public DateTime? SpotifyTokenExpiresAt { get; private set; }
    public DateTime? SpotifyConnectedAt { get; private set; }
    public string[] SpotifyScopes { get; private set; } = Array.Empty<string>();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool SpotifyConnected => SpotifyConnectedAt is not null;
    public bool HasLifestyleData => LifestyleCompletedAt is not null;
    public int CompletenessPercent
    {
        get
        {
            var sections = 0;
            if (LifestyleCompletedAt is not null) sections++;
            if (TipiCompletedAt is not null) sections++;
            if (InterestsCompletedAt is not null) sections++;
            if (SpotifyConnectedAt is not null) sections++;
            return sections * 25;
        }
    }

    private StudentProfile() { }

    public static StudentProfile Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User id is required.");

        var now = DateTime.UtcNow;
        return new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void SubmitLifestyle(
        SleepSchedule sleepSchedule, WakeUpTime wakeUpTime, int cleanlinessLevel,
        NoiseTolerance noiseTolerance, StudyLocation studyLocation,
        GuestFrequency guestFrequency, SmokingHabit smokingHabit)
    {
        if (cleanlinessLevel is < 1 or > 5)
            throw new DomainException("Cleanliness level must be between 1 and 5.");
        if (!Enum.IsDefined(sleepSchedule)) throw new DomainException("Invalid sleep schedule.");
        if (!Enum.IsDefined(wakeUpTime)) throw new DomainException("Invalid wake up time.");
        if (!Enum.IsDefined(noiseTolerance)) throw new DomainException("Invalid noise tolerance.");
        if (!Enum.IsDefined(studyLocation)) throw new DomainException("Invalid study location.");
        if (!Enum.IsDefined(guestFrequency)) throw new DomainException("Invalid guest frequency.");
        if (!Enum.IsDefined(smokingHabit)) throw new DomainException("Invalid smoking habit.");

        SleepSchedule = sleepSchedule;
        WakeUpTime = wakeUpTime;
        CleanlinessLevel = cleanlinessLevel;
        NoiseTolerance = noiseTolerance;
        StudyLocation = studyLocation;
        GuestFrequency = guestFrequency;
        SmokingHabit = smokingHabit;
        LifestyleCompletedAt = DateTime.UtcNow;
        UpdatedAt = LifestyleCompletedAt.Value;
    }

    public void SubmitTipi(int[] answers)
    {
        if (answers is null) throw new DomainException("TIPI answers are required.");
        if (answers.Length != 10)
            throw new DomainException("TIPI requires exactly 10 answers.");
        if (answers.Any(a => a is < 1 or > 7))
            throw new DomainException("Each TIPI answer must be between 1 and 7.");

        TipiAnswers = answers.ToArray();
        TipiExtraversion = (answers[0] + Reverse(answers[5])) / 2m;
        TipiAgreeableness = (Reverse(answers[1]) + answers[6]) / 2m;
        TipiConscientiousness = (answers[2] + Reverse(answers[7])) / 2m;
        TipiEmotionalStability = (Reverse(answers[3]) + answers[8]) / 2m;
        TipiOpenness = (answers[4] + Reverse(answers[9])) / 2m;
        TipiCompletedAt = DateTime.UtcNow;
        UpdatedAt = TipiCompletedAt.Value;
    }

    public void SetInterests(string[] slugs)
    {
        ArgumentNullException.ThrowIfNull(slugs);
        if (slugs.Length > 50)
            throw new DomainException("A maximum of 50 interests is allowed.");
        if (slugs.Distinct().Count() != slugs.Length)
            throw new DomainException("Interest slugs cannot contain duplicates.");
        if (slugs.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Interest slugs cannot be blank.");

        InterestSlugs = slugs.ToArray();
        InterestsCompletedAt = DateTime.UtcNow;
        UpdatedAt = InterestsCompletedAt.Value;
    }

    public void ConnectSpotify(
        string accessToken, string refreshToken, DateTime expiresAt,
        string[] scopes, IDataProtector protector)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new DomainException("Spotify access token is required.");
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new DomainException("Spotify refresh token is required.");
        ArgumentNullException.ThrowIfNull(protector);

        SpotifyAccessToken = protector.Protect(accessToken);
        SpotifyRefreshToken = protector.Protect(refreshToken);
        SpotifyTokenExpiresAt = expiresAt;
        SpotifyScopes = scopes ?? Array.Empty<string>();
        SpotifyConnectedAt = DateTime.UtcNow;
        UpdatedAt = SpotifyConnectedAt.Value;
    }

    public void DisconnectSpotify()
    {
        SpotifyAccessToken = null;
        SpotifyRefreshToken = null;
        SpotifyTokenExpiresAt = null;
        SpotifyConnectedAt = null;
        SpotifyScopes = Array.Empty<string>();
        UpdatedAt = DateTime.UtcNow;
    }

    private static int Reverse(int value) => 8 - value;
}
