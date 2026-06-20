using System.Security.Cryptography;

namespace Materia.Infrastructure.Auth;

/// <summary>
/// Generates cryptographically random temporary passwords that always satisfy the configured
/// Identity policy (length ≥ 8, at least one uppercase letter and one digit). Ambiguous
/// characters (0/O, 1/l/I) are excluded so the password is easy to read and re-type, and the
/// alphabet contains no HTML-sensitive characters so it survives HTML-encoding in the email.
/// </summary>
internal static class TemporaryPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no I, O
    private const string Lower = "abcdefghijkmnpqrstuvwxyz"; // no l, o
    private const string Digits = "23456789";                // no 0, 1
    private const int Length = 14;

    public static string Generate()
    {
        var all = Upper + Lower + Digits;
        var chars = new char[Length];

        // Guarantee the policy is met regardless of the random draw.
        chars[0] = Pick(Upper);
        chars[1] = Pick(Lower);
        chars[2] = Pick(Digits);
        for (var i = 3; i < Length; i++)
            chars[i] = Pick(all);

        Shuffle(chars);
        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];

    private static void Shuffle(char[] chars)
    {
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
