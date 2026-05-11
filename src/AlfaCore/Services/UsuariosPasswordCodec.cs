using System.Globalization;
using System.Text;

namespace AlfaCore.Services;

public sealed class UsuariosPasswordCodec
{
    public string Encode(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var sb = new StringBuilder(plainText.Length * 3);
        var length = plainText.Length;

        for (var index = plainText.Length - 1; index >= 0; index--)
        {
            var value = plainText[index] + length;
            sb.Append(value.ToString("000", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public string Decode(string encodedText)
    {
        if (string.IsNullOrWhiteSpace(encodedText))
            return string.Empty;

        var normalized = encodedText.Trim();
        if (normalized.Length % 3 != 0)
            return string.Empty;

        var exact = DecodeUsingOffset(normalized, normalized.Length / 3);
        if (LooksPrintable(exact))
            return exact;

        var legacy = DecodeUsingOffset(normalized, normalized.Length * 2 / 3);
        return LooksPrintable(legacy) ? legacy : exact;
    }

    private static string DecodeUsingOffset(string encodedText, int offset)
    {
        var sb = new StringBuilder(encodedText.Length / 3);

        for (var index = encodedText.Length; index >= 3; index -= 3)
        {
            var block = encodedText.Substring(index - 3, 3);
            if (!int.TryParse(block, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return string.Empty;

            var value = parsed - offset;
            if (value < char.MinValue || value > char.MaxValue)
                return string.Empty;

            sb.Append((char)value);
        }

        return sb.ToString();
    }

    private static bool LooksPrintable(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var ch in value)
        {
            if (char.IsControl(ch))
                return false;
        }

        return true;
    }
}
