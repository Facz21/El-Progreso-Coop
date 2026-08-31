using System.Text.RegularExpressions;
using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Application.Validation;

/// <summary>
/// Provides validation logic for Colombian identity documents, names, and contact information.
/// </summary>
public static class AssociateValidator
{
    private static readonly Regex LetterWordRegex = new(@"^[a-zA-ZáéíóúüñÁÉÍÓÚÜÑ]+$", RegexOptions.Compiled);
    private static readonly Regex CcRegex = new(@"^\d{6,10}$", RegexOptions.Compiled);
    private static readonly Regex TiRegex = new(@"^\d{10,11}$", RegexOptions.Compiled);
    private static readonly Regex CeRegex = new(@"^[a-zA-Z0-9]{6,10}$", RegexOptions.Compiled);
    private static readonly Regex NitRegex = new(@"^\d{9,10}(-\d)?$", RegexOptions.Compiled);
    private static readonly Regex PasRegex = new(@"^[a-zA-Z0-9]{6,16}$", RegexOptions.Compiled);
    private static readonly Regex PhoneDigitsRegex = new(@"^\d{7,10}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates that the full name contains at least 3 words (1 given name + 2 surnames) with alphabetic characters only.
    /// </summary>
    public static ValidationResult ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Failure("El nombre completo es obligatorio.");
        }

        var words = name.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 3)
        {
            return ValidationResult.Failure("El nombre debe contener al menos un nombre de pila y dos apellidos (mínimo 3 palabras).");
        }

        foreach (var word in words)
        {
            if (word.Length < 2)
            {
                return ValidationResult.Failure($"La palabra '{word}' es demasiado corta. Cada componente del nombre debe tener al menos 2 caracteres.");
            }

            if (!LetterWordRegex.IsMatch(word))
            {
                return ValidationResult.Failure($"El nombre contiene caracteres inválidos en '{word}'. Solo se permiten letras del alfabeto.");
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates the document number format based on the specified Colombian document type (CC, TI, CE, NIT, PAS).
    /// </summary>
    public static ValidationResult ValidateDocument(DocumentType documentType, string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return ValidationResult.Failure("El número de documento es obligatorio.");
        }

        var trimmed = documentNumber.Trim();

        return documentType switch
        {
            DocumentType.CC => CcRegex.IsMatch(trimmed)
                ? ValidationResult.Success()
                : ValidationResult.Failure("La Cédula de Ciudadanía (CC) debe tener entre 6 y 10 dígitos numéricos (ej. 1020304050)."),

            DocumentType.TI => TiRegex.IsMatch(trimmed)
                ? ValidationResult.Success()
                : ValidationResult.Failure("La Tarjeta de Identidad (TI) debe tener entre 10 y 11 dígitos numéricos (ej. 1098765432)."),

            DocumentType.CE => CeRegex.IsMatch(trimmed)
                ? ValidationResult.Success()
                : ValidationResult.Failure("La Cédula de Extranjería (CE) debe tener entre 6 y 10 caracteres alfanuméricos."),

            DocumentType.NIT => NitRegex.IsMatch(trimmed)
                ? ValidationResult.Success()
                : ValidationResult.Failure("El NIT debe tener entre 9 y 10 dígitos, con dígito de verificación opcional (ej. 900123456 o 900123456-1)."),

            DocumentType.PAS => PasRegex.IsMatch(trimmed)
                ? ValidationResult.Success()
                : ValidationResult.Failure("El Pasaporte (PAS) debe tener entre 6 y 16 caracteres alfanuméricos (ej. PA1234567)."),

            _ => ValidationResult.Failure("Tipo de documento no soportado.")
        };
    }

    /// <summary>
    /// Validates Colombian phone numbers (7 to 10 digits).
    /// </summary>
    public static ValidationResult ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ValidationResult.Failure("El número de teléfono es obligatorio.");
        }

        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        if (!PhoneDigitsRegex.IsMatch(digitsOnly))
        {
            return ValidationResult.Failure("El teléfono debe contener entre 7 y 10 dígitos numéricos (ej. 3001234567 o 6012345678).");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates RFC standard email address format.
    /// </summary>
    public static ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Failure("El correo electrónico es obligatorio.");
        }

        var trimmed = email.Trim();
        if (!EmailRegex.IsMatch(trimmed))
        {
            return ValidationResult.Failure("El formato del correo electrónico es inválido (ej. asociado@correo.com).");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates residential street address minimum length requirements.
    /// </summary>
    public static ValidationResult ValidateAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return ValidationResult.Failure("La dirección de residencia es obligatoria.");
        }

        var trimmed = address.Trim();
        if (trimmed.Length < 5)
        {
            return ValidationResult.Failure("La dirección debe tener al menos 5 caracteres (ej. Calle 45 # 23-12).");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates full associate profile data across all fields.
    /// </summary>
    public static ValidationResult Validate(
        DocumentType documentType,
        string? documentNumber,
        string? name,
        string? phone = null,
        string? email = null,
        string? address = null)
    {
        var docResult = ValidateDocument(documentType, documentNumber);
        if (!docResult.IsValid) return docResult;

        var nameResult = ValidateName(name);
        if (!nameResult.IsValid) return nameResult;

        if (phone != null)
        {
            var phoneResult = ValidatePhone(phone);
            if (!phoneResult.IsValid) return phoneResult;
        }

        if (email != null)
        {
            var emailResult = ValidateEmail(email);
            if (!emailResult.IsValid) return emailResult;
        }

        if (address != null)
        {
            var addressResult = ValidateAddress(address);
            if (!addressResult.IsValid) return addressResult;
        }

        return ValidationResult.Success();
    }
}
