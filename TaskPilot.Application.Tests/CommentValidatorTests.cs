using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Features.Comments.Validators;

namespace TaskPilot.Application.Tests;

public sealed class CommentValidatorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Comment_validators_reject_content_longer_than_2000_characters(bool isUpdate)
    {
        var content = new string('a', 2001);

        var result = isUpdate
            ? new UpdateCommentRequestValidator().Validate(new UpdateCommentRequest(content))
            : new CreateCommentRequestValidator().Validate(new CreateCommentRequest(content));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("2000", StringComparison.Ordinal));
    }
}
