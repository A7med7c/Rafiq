using FluentAssertions;
using Rafiq.Application.Features.Auth.Commands.Register;

namespace Rafiq.Application.Tests.Auth;

public sealed class RegisterCommandValidatorTests
{
    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsNoErrors()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("patient@example.com", "+201001234567", "Password1!", "Password1!", "Patient");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPasswordIsWeak_ReturnsErrors()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("patient@example.com", "+201001234567", "password", "password", "Patient");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
