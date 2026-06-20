using System.Buffers.Text;
using System.Text;

namespace Materia.Infrastructure.Auth;

/// <summary>
/// Makes ASP.NET Identity tokens safe to embed in URLs. Identity tokens contain
/// characters (<c>+ / =</c>) that are mangled by query-string handling, so they are
/// Base64Url-encoded before going into an email link and decoded on the way back.
/// </summary>
internal static class IdentityTokenCodec
{
    public static string Encode(string token) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    public static string Decode(string encodedToken) =>
        Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encodedToken));
}
