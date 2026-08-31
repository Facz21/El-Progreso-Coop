using ElProgreso.Coop.Application.Validation;
using ElProgreso.Coop.Domain.Enums;
using Xunit;

namespace ElProgreso.Coop.Tests;

public class ValidatorTests
{
    [Theory]
    [InlineData("Carlos Alberto Mendoza")]
    [InlineData("Juan Carlos Gomez Lopez")]
    [InlineData("María José Rodríguez Peña")]
    [InlineData("Álvaro Andrés González Vélez")]
    public void ValidateName_ValidNames_ShouldSucceed(string name)
    {
        var result = AssociateValidator.ValidateName(name);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateName_EmptyOrNull_ShouldFail(string? name)
    {
        var result = AssociateValidator.ValidateName(name);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("Carlos")]                    // 1 word
    [InlineData("Carlos Gomez")]              // 2 words
    [InlineData("Carlos 123 Gomez")]          // contains numbers
    [InlineData("Carlos A. Gomez")]           // dot or single letter
    [InlineData("Juan @ Gomez")]              // symbols
    public void ValidateName_InvalidNames_ShouldFail(string name)
    {
        var result = AssociateValidator.ValidateName(name);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("123456")]       // 6 digits (minimum)
    [InlineData("1020304050")]   // 10 digits (maximum)
    [InlineData("52345678")]     // 8 digits
    public void ValidateDocument_ValidCC_ShouldSucceed(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.CC, documentNumber);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("222")]           // Too short (3 digits)
    [InlineData("12345")]         // Too short (5 digits)
    [InlineData("10203040501")]   // Too long (11 digits)
    [InlineData("102030405A")]   // Contains letters
    [InlineData("")]
    public void ValidateDocument_InvalidCC_ShouldFail(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.CC, documentNumber);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("1020304050")]    // 10 digits
    [InlineData("10203040501")]   // 11 digits
    public void ValidateDocument_ValidTI_ShouldSucceed(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.TI, documentNumber);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("123456")]        // 6 digits
    [InlineData("E123456")]       // 7 alphanumeric
    [InlineData("CE12345678")]    // 10 alphanumeric
    public void ValidateDocument_ValidCE_ShouldSucceed(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.CE, documentNumber);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("900123456")]     // 9 digits
    [InlineData("900123456-1")]   // 9 digits with verification digit
    [InlineData("9001234567")]    // 10 digits
    public void ValidateDocument_ValidNIT_ShouldSucceed(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.NIT, documentNumber);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("PA123456")]      // 8 chars
    [InlineData("PAS123456789")]  // 12 chars
    public void ValidateDocument_ValidPAS_ShouldSucceed(string documentNumber)
    {
        var result = AssociateValidator.ValidateDocument(DocumentType.PAS, documentNumber);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("3001234567")]
    [InlineData("310-456-7890")]
    [InlineData("6012345678")]
    public void ValidatePhone_ValidPhone_ShouldSucceed(string phone)
    {
        var result = AssociateValidator.ValidatePhone(phone);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("30012345678901")]
    [InlineData("abc-defg")]
    [InlineData("")]
    public void ValidatePhone_InvalidPhone_ShouldFail(string phone)
    {
        var result = AssociateValidator.ValidatePhone(phone);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("asociado@email.com")]
    [InlineData("carlos.mendoza@empresa.com.co")]
    public void ValidateEmail_ValidEmail_ShouldSucceed(string email)
    {
        var result = AssociateValidator.ValidateEmail(email);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("correo-invalido")]
    [InlineData("@email.com")]
    [InlineData("asociado@")]
    [InlineData("")]
    public void ValidateEmail_InvalidEmail_ShouldFail(string email)
    {
        var result = AssociateValidator.ValidateEmail(email);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Calle 45 # 23-12")]
    [InlineData("Carrera 7 # 116-50, Bogotá")]
    public void ValidateAddress_ValidAddress_ShouldSucceed(string address)
    {
        var result = AssociateValidator.ValidateAddress(address);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Cl")]
    [InlineData("")]
    public void ValidateAddress_TooShort_ShouldFail(string address)
    {
        var result = AssociateValidator.ValidateAddress(address);
        Assert.False(result.IsValid);
    }
}
