using System.Reflection;

namespace Aura.Tests.Support;

/// <summary>
/// Test helpers for setting private properties on domain entities.
/// EF Core normally sets navigation properties via reflection when loading;
/// in unit tests we need to do the same to simulate loaded data.
/// </summary>
public static class EntityTestExtensions
{
    public static T SetPrivateProperty<T>(this T entity, string propertyName, object? value) where T : class
    {
        var property = typeof(T).GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name}");

        property.SetValue(entity, value);
        return entity;
    }
}
