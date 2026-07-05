using FluentAssertions;
using Materia.Application.Queries.Inventory;

namespace Materia.Tests.Inventory;

public class SemanticProductSearchQueryValidatorTests
{
    private readonly SemanticProductSearchQueryValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQuery_IsInvalid(string query)
    {
        var result = await _validator.ValidateAsync(new SemanticProductSearchQuery(query));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public async Task TopKOutOfRange_IsInvalid(int topK)
    {
        var result = await _validator.ValidateAsync(new SemanticProductSearchQuery("semen", TopK: topK));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public async Task MinScoreOutOfRange_IsInvalid(double minScore)
    {
        var result = await _validator.ValidateAsync(
            new SemanticProductSearchQuery("semen", MinScore: minScore));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidQuery_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new SemanticProductSearchQuery("semen anti bocor", TopK: 5, MinScore: 0.5));
        result.IsValid.Should().BeTrue();
    }
}
