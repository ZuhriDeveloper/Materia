using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

public class FakeAccountLinkBuilder : IAccountLinkBuilder
{
    public string BuildEmailConfirmationLink(string userId, string encodedToken) =>
        $"https://test/confirm-email?userId={userId}&token={encodedToken}";

    public string BuildPasswordResetLink(string email, string encodedToken) =>
        $"https://test/reset-password?email={email}&token={encodedToken}";
}
