namespace DigitaleDelta.Contracts.Configuration;

/// <summary>
/// Position of the wildcard in a string. Used to map to SQL string compare functions.
/// </summary>
public enum WildCardPosition
{
    /// <summary>
    /// Place at the left
    /// </summary>
    Left = 1,
    /// <summary>
    /// Place at the right
    /// </summary>
    Right = 2,
    /// <summary>
    /// Place at both ends (contains
    /// </summary>
    LeftAndRight = 3
}