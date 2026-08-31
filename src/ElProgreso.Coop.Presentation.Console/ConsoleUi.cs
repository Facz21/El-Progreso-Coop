using System.Globalization;
using ElProgreso.Coop.Application.Validation;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using Spectre.Console;

namespace ElProgreso.Coop.Presentation.Console;

public static class ConsoleUi
{
    private static readonly CultureInfo CopCulture = new("es-CO");

    public static bool IsCancelCommand(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim().ToLowerInvariant();
        return trimmed == "0" || trimmed == "cancelar" || trimmed == "volver" || trimmed == "cancel" || trimmed == "back";
    }

    public static void PrintHeader(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(title.ToUpperInvariant())}[/]")
            .LeftJustified()
            .RuleStyle("cyan"));
        AnsiConsole.WriteLine();
    }

    public static void PrintSubHeader(string title)
    {
        AnsiConsole.Write(new Rule($"[bold yellow]{Markup.Escape(title)}[/]")
            .LeftJustified()
            .RuleStyle("yellow"));
        AnsiConsole.WriteLine();
    }

    public static void PrintSuccessPanel(string title, string details)
    {
        var panel = new Panel(new Markup($"[green]{Markup.Escape(details)}[/]"))
        {
            Header = new PanelHeader($"[bold green]{Markup.Escape(title)}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
    }

    public static void PrintWarningPanel(string title, string details)
    {
        var panel = new Panel(new Markup($"[yellow]{Markup.Escape(details)}[/]"))
        {
            Header = new PanelHeader($"[bold yellow]{Markup.Escape(title)}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
    }

    public static void PrintErrorPanel(string title, string details)
    {
        var panel = new Panel(new Markup($"[red]{Markup.Escape(details)}[/]"))
        {
            Header = new PanelHeader($"[bold red]{Markup.Escape(title)}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
    }

    public static void PrintInfo(string message)
    {
        AnsiConsole.MarkupLine(message);
    }

    public static void DisplayPaginatedTable<T>(
        string title,
        IReadOnlyList<T> items,
        Func<Table> tableFactory,
        Action<Table, T, int> rowRenderer,
        int pageSize = 10)
    {
        if (items.Count == 0)
        {
            PrintWarningPanel("Sin Registros", "No hay elementos para mostrar.");
            Pause();
            return;
        }

        if (System.Console.IsInputRedirected)
        {
            var staticTable = tableFactory();
            for (int i = 0; i < items.Count; i++)
            {
                rowRenderer(staticTable, items[i], i + 1);
            }
            AnsiConsole.Write(staticTable);
            AnsiConsole.MarkupLine($"[grey]Total de registros: [bold]{items.Count}[/][/]");
            Pause();
            return;
        }

        int totalItems = items.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        int currentPage = 1;

        while (true)
        {
            AnsiConsole.Clear();
            PrintHeader(title);

            var table = tableFactory();
            int startIndex = (currentPage - 1) * pageSize;
            int count = Math.Min(pageSize, totalItems - startIndex);

            for (int i = 0; i < count; i++)
            {
                int globalIndex = startIndex + i;
                rowRenderer(table, items[globalIndex], globalIndex + 1);
            }

            AnsiConsole.Write(table);

            var startItemNum = startIndex + 1;
            var endItemNum = startIndex + count;
            AnsiConsole.MarkupLine($"[grey]Página [bold cyan]{currentPage}[/] de [bold cyan]{totalPages}[/] | Mostrando registros [bold]{startItemNum}[/] a [bold]{endItemNum}[/] de [bold]{totalItems}[/] totales[/]");
            AnsiConsole.WriteLine();

            if (totalPages <= 1)
            {
                Pause();
                return;
            }

            var navChoices = new List<string>();
            if (currentPage < totalPages)
            {
                navChoices.Add("Siguiente página >");
            }
            if (currentPage > 1)
            {
                navChoices.Add("< Página anterior");
            }
            navChoices.Add("Ir a número de página");
            navChoices.Add("0. Volver al menú");

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Navegación de Páginas:[/]")
                    .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                    .AddChoices(navChoices)
            );

            if (selected.StartsWith("Siguiente"))
            {
                currentPage++;
            }
            else if (selected.StartsWith("<"))
            {
                currentPage--;
            }
            else if (selected.StartsWith("Ir"))
            {
                var targetPage = AnsiConsole.Prompt(
                    new TextPrompt<int>($"Ingrese el número de página [bold cyan](1 - {totalPages})[/]:")
                        .PromptStyle("cyan")
                        .Validate(p => (p >= 1 && p <= totalPages)
                            ? Spectre.Console.ValidationResult.Success()
                            : Spectre.Console.ValidationResult.Error($"[red]Por favor ingrese un número de página válido entre 1 y {totalPages}.[/]"))
                );
                currentPage = targetPage;
            }
            else
            {
                break;
            }
        }
    }

    public static string PromptMenu(string title, IEnumerable<string> choices)
    {
        var choiceList = choices.ToList();

        if (System.Console.IsInputRedirected)
        {
            var line = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
            {
                return choiceList.LastOrDefault() ?? "0";
            }

            var match = choiceList.FirstOrDefault(c =>
                c.StartsWith(line + ".", StringComparison.OrdinalIgnoreCase) ||
                c.StartsWith(line + " ", StringComparison.OrdinalIgnoreCase) ||
                c.Equals(line, StringComparison.OrdinalIgnoreCase));

            return match ?? line;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold yellow]{Markup.Escape(title)}[/]")
                .PageSize(12)
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                .AddChoices(choiceList)
        );
    }

    public static DocumentType? PromptDocumentTypeWithCancel()
    {
        var options = new List<string>
        {
            "CC  - Cédula de Ciudadanía",
            "TI  - Tarjeta de Identidad",
            "CE  - Cédula de Extranjería",
            "NIT - Número de Identificación Tributaria",
            "PAS - Pasaporte",
            "0. Cancelar y volver al menú principal"
        };

        if (System.Console.IsInputRedirected)
        {
            var line = System.Console.ReadLine()?.Trim().ToUpperInvariant();
            if (IsCancelCommand(line)) return null;

            if (Enum.TryParse<DocumentType>(line, true, out var dt))
            {
                return dt;
            }
            return DocumentType.CC;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Seleccione el [bold green]Tipo de Documento[/]:")
                .PageSize(6)
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                .AddChoices(options)
        );

        if (choice.StartsWith("0.")) return null;
        if (choice.StartsWith("CC")) return DocumentType.CC;
        if (choice.StartsWith("TI")) return DocumentType.TI;
        if (choice.StartsWith("CE")) return DocumentType.CE;
        if (choice.StartsWith("NIT")) return DocumentType.NIT;
        if (choice.StartsWith("PAS")) return DocumentType.PAS;

        return null;
    }

    public static string? PromptDocumentNumberWithCancel(DocumentType documentType)
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                var val = AssociateValidator.ValidateDocument(documentType, input);
                if (val.IsValid)
                {
                    return input!;
                }
                PrintErrorPanel("Validación de Documento", val.ErrorMessage ?? "Documento inválido.");
            }
        }

        var prompt = new TextPrompt<string>($"Ingrese el [bold green]número de documento[/] ({documentType}) [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input =>
            {
                if (IsCancelCommand(input))
                {
                    return Spectre.Console.ValidationResult.Success();
                }

                var result = AssociateValidator.ValidateDocument(documentType, input);
                return result.IsValid
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error($"[red]{Markup.Escape(result.ErrorMessage ?? "Documento inválido")}[/]");
            });

        var value = AnsiConsole.Prompt(prompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static string? PromptAssociateNameWithCancel(string promptText = "Ingrese el [bold green]nombre completo[/] (mínimo 1 nombre y 2 apellidos)")
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                var val = AssociateValidator.ValidateName(input);
                if (val.IsValid)
                {
                    return input!;
                }
                PrintErrorPanel("Validación de Nombre", val.ErrorMessage ?? "Nombre inválido.");
            }
        }

        var prompt = new TextPrompt<string>($"{promptText} [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input =>
            {
                if (IsCancelCommand(input))
                {
                    return Spectre.Console.ValidationResult.Success();
                }

                var result = AssociateValidator.ValidateName(input);
                return result.IsValid
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error($"[red]{Markup.Escape(result.ErrorMessage ?? "Nombre inválido")}[/]");
            });

        var value = AnsiConsole.Prompt(prompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static string? PromptPhoneWithCancel(string promptText = "Ingrese el teléfono de contacto (7 a 10 dígitos)")
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                var val = AssociateValidator.ValidatePhone(input);
                if (val.IsValid)
                {
                    return input!;
                }
                PrintErrorPanel("Validación de Teléfono", val.ErrorMessage ?? "Teléfono inválido.");
            }
        }

        var prompt = new TextPrompt<string>($"{promptText} [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input =>
            {
                if (IsCancelCommand(input))
                {
                    return Spectre.Console.ValidationResult.Success();
                }

                var result = AssociateValidator.ValidatePhone(input);
                return result.IsValid
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error($"[red]{Markup.Escape(result.ErrorMessage ?? "Teléfono inválido")}[/]");
            });

        var value = AnsiConsole.Prompt(prompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static string? PromptEmailWithCancel(string promptText = "Ingrese el correo electrónico")
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                var val = AssociateValidator.ValidateEmail(input);
                if (val.IsValid)
                {
                    return input!;
                }
                PrintErrorPanel("Validación de Correo", val.ErrorMessage ?? "Correo inválido.");
            }
        }

        var prompt = new TextPrompt<string>($"{promptText} [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input =>
            {
                if (IsCancelCommand(input))
                {
                    return Spectre.Console.ValidationResult.Success();
                }

                var result = AssociateValidator.ValidateEmail(input);
                return result.IsValid
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error($"[red]{Markup.Escape(result.ErrorMessage ?? "Correo inválido")}[/]");
            });

        var value = AnsiConsole.Prompt(prompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static string? PromptAddressWithCancel(string promptText = "Ingrese la dirección de residencia")
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                var val = AssociateValidator.ValidateAddress(input);
                if (val.IsValid)
                {
                    return input!;
                }
                PrintErrorPanel("Validación de Dirección", val.ErrorMessage ?? "Dirección inválida.");
            }
        }

        var prompt = new TextPrompt<string>($"{promptText} [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input =>
            {
                if (IsCancelCommand(input))
                {
                    return Spectre.Console.ValidationResult.Success();
                }

                var result = AssociateValidator.ValidateAddress(input);
                return result.IsValid
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error($"[red]{Markup.Escape(result.ErrorMessage ?? "Dirección inválida")}[/]");
            });

        var value = AnsiConsole.Prompt(prompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static string? PromptStringWithCancel(string prompt)
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine();
                if (IsCancelCommand(input)) return null;

                if (!string.IsNullOrWhiteSpace(input))
                {
                    return input.Trim();
                }
                PrintErrorPanel("Entrada requerida", "El valor no puede estar vacío.");
            }
        }

        var textPrompt = new TextPrompt<string>($"{prompt} [grey](o '0' para volver)[/]:")
            .PromptStyle("cyan")
            .Validate(input => string.IsNullOrWhiteSpace(input)
                ? Spectre.Console.ValidationResult.Error("[red]El valor no puede estar vacío.[/]")
                : Spectre.Console.ValidationResult.Success());

        var value = AnsiConsole.Prompt(textPrompt).Trim();
        return IsCancelCommand(value) ? null : value;
    }

    public static decimal? PromptDecimalWithCancel(string prompt)
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim().Replace("$", "").Replace(",", ".");
                if (IsCancelCommand(input) || input == "0") return null;

                if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) && value > 0)
                {
                    return value;
                }
                PrintErrorPanel("Monto inválido", "Por favor ingrese un monto numérico válido mayor a cero (o 0 para cancelar).");
            }
        }

        var valueResult = AnsiConsole.Prompt(
            new TextPrompt<decimal>($"{prompt} [grey](o 0 para cancelar)[/]:")
                .PromptStyle("cyan")
                .Validate(amount => amount >= 0
                    ? Spectre.Console.ValidationResult.Success()
                    : Spectre.Console.ValidationResult.Error("[red]El monto no puede ser negativo.[/]"))
        );

        return valueResult == 0m ? null : valueResult;
    }

    public static DateTime? PromptDateWithCancel(string prompt)
    {
        if (System.Console.IsInputRedirected)
        {
            while (true)
            {
                var input = System.Console.ReadLine()?.Trim();
                if (IsCancelCommand(input)) return null;

                if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }
                if (DateTime.TryParse(input, CopCulture, DateTimeStyles.None, out var fallbackDate))
                {
                    return fallbackDate;
                }
                PrintErrorPanel("Fecha inválida", "Utilice el formato YYYY-MM-DD (ejemplo: 2026-08-31) o '0' para cancelar.");
            }
        }

        var result = AnsiConsole.Prompt(
            new TextPrompt<string>($"{prompt} [grey](YYYY-MM-DD o '0' para volver)[/]:")
                .PromptStyle("cyan")
                .Validate(input =>
                {
                    if (IsCancelCommand(input))
                    {
                        return Spectre.Console.ValidationResult.Success();
                    }

                    if (DateTime.TryParseExact(input?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        return Spectre.Console.ValidationResult.Success();
                    }
                    return Spectre.Console.ValidationResult.Error("[red]Formato inválido. Ingrese YYYY-MM-DD o '0' para cancelar.[/]");
                })
        );

        if (IsCancelCommand(result)) return null;
        return DateTime.ParseExact(result.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static bool PromptConfirmation(string prompt)
    {
        if (System.Console.IsInputRedirected)
        {
            var line = System.Console.ReadLine()?.Trim().ToLowerInvariant();
            return line == "s" || line == "si" || line == "sí" || line == "y" || line == "yes";
        }

        return AnsiConsole.Prompt(
            new ConfirmationPrompt(prompt)
            {
                DefaultValue = false,
                Yes = 's',
                No = 'n'
            }
        );
    }

    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Presione ENTER para continuar...[/]");

        if (System.Console.IsInputRedirected)
        {
            System.Console.ReadLine();
        }
        else
        {
            try
            {
                System.Console.ReadKey(intercept: true);
            }
            catch
            {
                System.Console.ReadLine();
            }
        }
    }

    public static string FormatCurrency(decimal amount)
    {
        return $"${amount:N2} COP";
    }

    public static string FormatUsd(decimal amount)
    {
        return $"${amount:N2} USD";
    }
}
