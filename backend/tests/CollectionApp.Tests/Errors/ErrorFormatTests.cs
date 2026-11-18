using System.Net;
using System.Security.Claims;
using System.Text.Json;
using CollectionApp.Api.Http.Exceptions;
using CollectionApp.Application.Security;
using CollectionApp.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace CollectionApp.Tests.Errors;

/// <summary>
/// Error body format shared by every endpoint (plan.md "Écarts de l'existant", contract `Error`).
/// </summary>
public class ErrorFormatTests
{
    private static DefaultHttpContext CreateContext(bool logRead = false)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (logRead)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimPrincipalTypes.Claim, ClaimPrincipalValues.LogRead)], "Test"));
        }
        return context;
    }

    private static async Task<JsonElement> WriteAsync(DefaultHttpContext context, Exception exception)
    {
        var httpException = new HttpException(context, exception);
        await HttpExceptionTools.SetExceptionResponse(context, httpException.Body);
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(await new StreamReader(context.Response.Body).ReadToEndAsync()).RootElement;
    }

    private static ValidationException CreateValidationException() => new(
    [
        new ValidationFailure("Email", "L'adresse e-mail n'est pas valide"),
        new ValidationFailure("PasswordConfirmation", "La confirmation ne correspond pas au mot de passe"),
    ]);

    [Fact]
    public void ValidationException_MapsTo400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, ExceptionStatusMapper.GetStatusCode(CreateValidationException()));
    }

    [Fact]
    public async Task ValidationException_BodyListsFieldsInCamelCase()
    {
        var context = CreateContext();

        var body = await WriteAsync(context, CreateValidationException());

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("ValidationException", body.GetProperty("code").GetString());
        Assert.Equal("Les données saisies sont invalides", body.GetProperty("message").GetString());
        var fields = body.GetProperty("fields").EnumerateArray()
            .Select(f => (f.GetProperty("field").GetString(), f.GetProperty("message").GetString()))
            .ToList();
        Assert.Equal(
            [("email", "L'adresse e-mail n'est pas valide"), ("passwordConfirmation", "La confirmation ne correspond pas au mot de passe")],
            fields);
    }

    [Fact]
    public async Task OtherException_HasNoFields()
    {
        var body = await WriteAsync(CreateContext(), new NotFoundException("Introuvable"));

        Assert.False(body.TryGetProperty("fields", out _));
    }

    [Fact]
    public async Task WithoutLogRead_NoStackTraceNorDetails()
    {
        var body = await WriteAsync(CreateContext(), new InternalException("Boom"));

        Assert.False(body.TryGetProperty("stackTrace", out _));
        Assert.False(body.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task WithLogRead_StackTraceAndDetailsPresent()
    {
        var body = await WriteAsync(CreateContext(logRead: true), new InternalException("Boom"));

        Assert.True(body.TryGetProperty("stackTrace", out _));
        Assert.True(body.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task LockedException_WithRetryAfter_Returns423AndRetryAfterHeader()
    {
        var context = CreateContext();

        await WriteAsync(context, new LockedException("Compte temporairement bloqué", TimeSpan.FromSeconds(870)));

        Assert.Equal(423, context.Response.StatusCode);
        Assert.Equal("870", context.Response.Headers.RetryAfter.ToString());
    }

    [Fact]
    public async Task LockedException_RetryAfterRoundedUp()
    {
        var context = CreateContext();

        await WriteAsync(context, new LockedException("Compte temporairement bloqué", TimeSpan.FromSeconds(869.2)));

        Assert.Equal("870", context.Response.Headers.RetryAfter.ToString());
    }
}
