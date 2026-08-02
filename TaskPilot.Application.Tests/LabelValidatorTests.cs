using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Features.Labels.Validators;

namespace TaskPilot.Application.Tests;

public sealed class LabelValidatorTests
{
    [Theory]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Create_validator_enforces_fifty_character_name_limit(int length, bool expectedValid)
    {
        var result = new CreateLabelRequestValidator().Validate(new CreateLabelRequest(new string('a', length), null));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Update_validator_enforces_fifty_character_name_limit(int length, bool expectedValid)
    {
        var result = new UpdateLabelRequestValidator().Validate(new UpdateLabelRequest(new string('a', length), null));

        Assert.Equal(expectedValid, result.IsValid);
    }
}
