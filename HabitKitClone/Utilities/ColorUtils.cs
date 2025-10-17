namespace HabitKitClone.Utilities;

public static class ColorUtils
{
    /// <summary>
    /// Converts a hex color string to RGBA format with specified opacity
    /// </summary>
    /// <param name="hex">Hex color string (e.g., "#FF0000" or "FF0000")</param>
    /// <param name="opacity">Opacity value between 0.0 and 1.0</param>
    /// <returns>RGBA color string (e.g., "rgba(255, 0, 0, 0.5)")</returns>
    public static string HexToRgba(string hex, double opacity)
    {
        if (string.IsNullOrEmpty(hex))
            return "rgba(0, 0, 0, 0)";

        // Remove # if present
        hex = hex.TrimStart('#');

        // Ensure we have a valid hex color
        if (hex.Length != 6)
            return "rgba(0, 0, 0, 0)";

        try
        {
            // Parse hex values
            var r = Convert.ToInt32(hex.Substring(0, 2), 16);
            var g = Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = Convert.ToInt32(hex.Substring(4, 2), 16);

            // Clamp opacity between 0 and 1
            opacity = Math.Max(0.0, Math.Min(1.0, opacity));

            return $"rgba({r}, {g}, {b}, {opacity:F1})";
        }
        catch
        {
            return "rgba(0, 0, 0, 0)";
        }
    }
}
