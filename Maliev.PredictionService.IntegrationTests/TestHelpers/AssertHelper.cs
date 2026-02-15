namespace Maliev.PredictionService.IntegrationTests.TestHelpers;

/// <summary>
/// Helper methods for test assertions without FluentAssertions.
/// </summary>
public static class AssertHelper
{
    /// <summary>
    /// Asserts that a float value is approximately equal to an expected value within a tolerance.
    /// </summary>
    public static void AssertApproximately(float actual, float expected, float tolerance, string? userMessage = null)
    {
        var diff = Math.Abs(actual - expected);
        var message = userMessage ?? $"Expected {actual} to be approximately {expected} (±{tolerance})";

        if (diff > tolerance)
        {
            throw new Xunit.Sdk.XunitException($"{message}. Actual difference: {diff}");
        }
    }

    /// <summary>
    /// Asserts that a value is within a range (inclusive).
    /// </summary>
    public static void AssertInRange<T>(T actual, T min, T max, string? userMessage = null) where T : IComparable<T>
    {
        var message = userMessage ?? $"Expected {actual} to be between {min} and {max}";

        if (actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0)
        {
            throw new Xunit.Sdk.XunitException(message);
        }
    }
}
