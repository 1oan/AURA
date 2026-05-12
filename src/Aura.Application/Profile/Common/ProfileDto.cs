namespace Aura.Application.Profile.Common;

public record ProfileDto(
    int CompletenessPercent,
    bool HasLifestyleData,
    LifestyleDto? Lifestyle,
    TipiDto? Tipi,
    InterestsDto Interests,
    SpotifyDto Spotify);

public record LifestyleDto(
    string SleepSchedule, string WakeUpTime, int CleanlinessLevel,
    string NoiseTolerance, string StudyLocation, string GuestFrequency, string SmokingHabit,
    DateTime CompletedAt);

public record TipiDto(
    decimal Extraversion, decimal Agreeableness, decimal Conscientiousness,
    decimal EmotionalStability, decimal Openness,
    DateTime CompletedAt);

public record InterestsDto(string[] Slugs, DateTime? CompletedAt);

public record SpotifyDto(bool Connected, DateTime? ConnectedAt, string[] Scopes);
