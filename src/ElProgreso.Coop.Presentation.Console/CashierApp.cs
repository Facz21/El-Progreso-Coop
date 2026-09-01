using ElProgreso.Coop.Application.DTOs;
using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;
using Spectre.Console;

namespace ElProgreso.Coop.Presentation.Console;

public class CashierApp
{
    private readonly IBankingService _bankingService;
    private readonly IManagementReportService _reportService;
    private readonly IExchangeRateService _exchangeRateService;

    public CashierApp(
        IBankingService bankingService,
        IManagementReportService reportService,
        IExchangeRateService exchangeRateService)
    {
        _bankingService = bankingService;
        _reportService = reportService;
        _exchangeRateService = exchangeRateService;
    }

    public async Task RunAsync()
    {
        bool exit = false;
        var menuOptions = new[]
        {
            "1. Registrar Asociado",
            "2. Listar Todos los Asociados",
            "3. Buscar Asociado (por Documento o Nombre)",
            "4. Actualizar Datos de Asociado",
            "5. Eliminar Asociado",
            "6. Realizar Consignación (Depósito)",
            "7. Realizar Retiro",
            "8. Consultar Saldo y Conversión USD (TRM)",
            "9. Consultar Historial de Transacciones",
            "10. Reportes Gerenciales",
            "0. Salir del Sistema"
        };

        while (!exit)
        {
            try
            {
                ConsoleUi.PrintHeader("Cooperativa Financiera El Progreso - Módulo de Caja");
                var selection = ConsoleUi.PromptMenu("Seleccione una operación:", menuOptions);

                if (selection.StartsWith("1."))
                {
                    await RegisterAssociateAsync();
                }
                else if (selection.StartsWith("2."))
                {
                    await ListAssociatesAsync();
                }
                else if (selection.StartsWith("3."))
                {
                    await SearchAssociatesAsync();
                }
                else if (selection.StartsWith("4."))
                {
                    await UpdateAssociateAsync();
                }
                else if (selection.StartsWith("5."))
                {
                    await DeleteAssociateAsync();
                }
                else if (selection.StartsWith("6."))
                {
                    await DepositAsync();
                }
                else if (selection.StartsWith("7."))
                {
                    await WithdrawAsync();
                }
                else if (selection.StartsWith("8."))
                {
                    await ViewBalanceAsync();
                }
                else if (selection.StartsWith("9."))
                {
                    await ViewTransactionsAsync();
                }
                else if (selection.StartsWith("10."))
                {
                    await ManagementReportsMenuAsync();
                }
                else if (selection.StartsWith("0."))
                {
                    exit = true;
                    ConsoleUi.PrintSuccessPanel("Sesión Finalizada", "Gracias por utilizar el sistema de la Cooperativa El Progreso. ¡Hasta pronto!");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                ConsoleUi.Pause();
            }
        }
    }

    private async Task RegisterAssociateAsync()
    {
        ConsoleUi.PrintHeader("Registro de Nuevo Asociado");
        var docType = ConsoleUi.PromptDocumentTypeWithCancel();
        if (docType == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var document = ConsoleUi.PromptDocumentNumberWithCancel(docType.Value);
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var name = ConsoleUi.PromptAssociateNameWithCancel("Ingrese el nombre completo (1 nombre y 2 apellidos)");
        if (name == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var phone = ConsoleUi.PromptPhoneWithCancel("Ingrese el teléfono de contacto (ej. 3001234567)");
        if (phone == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var email = ConsoleUi.PromptEmailWithCancel("Ingrese el correo electrónico (ej. asociado@correo.com)");
        if (email == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var address = ConsoleUi.PromptAddressWithCancel("Ingrese la dirección de residencia (ej. Calle 45 # 23-12)");
        if (address == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        try
        {
            var associate = await _bankingService.RegisterAssociateAsync(
                document,
                name,
                docType.Value,
                phone,
                email,
                address
            );

            var details = $"Tipo Documento:  {associate.DocumentType}\n" +
                          $"Documento:       {associate.Document}\n" +
                          $"Nombre Completo: {associate.Name}\n" +
                          $"Teléfono:        {associate.Phone}\n" +
                          $"Correo:          {associate.Email}\n" +
                          $"Dirección:       {associate.Address}\n" +
                          $"Fecha Registro:  {associate.RegistrationDate:yyyy-MM-dd HH:mm}\n" +
                          $"Saldo Inicial:   {ConsoleUi.FormatCurrency(associate.Balance)}";

            ConsoleUi.PrintSuccessPanel("Asociado Registrado Satisfactoriamente", details);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        ConsoleUi.Pause();
    }

    private async Task ListAssociatesAsync()
    {
        var filterOptions = new[]
        {
            "1. Listar todos (Sin filtros, orden alfabético A - Z)",
            "2. Filtrar por Tipo de Documento (CC, TI, CE, NIT, PAS)",
            "3. Filtrar por Estado de Saldo (> $0 COP o $0 COP)",
            "4. Filtrar por Actividad (Con Movimientos o Cuentas Inactivas)",
            "5. Listado con Ordenamiento Personalizado",
            "0. Volver al menú principal"
        };

        ConsoleUi.PrintHeader("Listado y Filtros de Asociados");
        var selectedOption = ConsoleUi.PromptMenu("Seleccione el criterio de visualización:", filterOptions);

        if (selectedOption.StartsWith("0."))
        {
            return;
        }

        AssociateFilterCriteria criteria = new();
        string viewTitle = "Listado General de Asociados";

        if (selectedOption.StartsWith("1."))
        {
            criteria = new AssociateFilterCriteria(SortBy: AssociateSortField.NameAsc);
            viewTitle = "Listado General de Asociados (Orden: Nombre A - Z)";
        }
        else if (selectedOption.StartsWith("2."))
        {
            var docType = ConsoleUi.PromptDocumentTypeWithCancel();
            if (docType == null) return;

            criteria = new AssociateFilterCriteria(DocumentType: docType.Value, SortBy: AssociateSortField.NameAsc);
            viewTitle = $"Listado de Asociados (Tipo: {docType.Value})";
        }
        else if (selectedOption.StartsWith("3."))
        {
            var balanceChoices = new[]
            {
                "1. Asociados con saldo positivo (> $0 COP)",
                "2. Asociados con saldo en cero ($0 COP)",
                "0. Cancelar y volver"
            };
            var balanceSelection = ConsoleUi.PromptMenu("Seleccione el filtro de saldo:", balanceChoices);
            if (balanceSelection.StartsWith("0.")) return;

            if (balanceSelection.StartsWith("1."))
            {
                criteria = new AssociateFilterCriteria(BalanceFilter: BalanceFilter.PositiveBalance, SortBy: AssociateSortField.BalanceDesc);
                viewTitle = "Asociados con Saldo Positivo (Orden: Saldo Mayor a Menor)";
            }
            else
            {
                criteria = new AssociateFilterCriteria(BalanceFilter: BalanceFilter.ZeroBalance, SortBy: AssociateSortField.NameAsc);
                viewTitle = "Asociados con Saldo en Cero ($0 COP)";
            }
        }
        else if (selectedOption.StartsWith("4."))
        {
            var activityChoices = new[]
            {
                "1. Asociados con movimientos (1 o más transacciones)",
                "2. Asociados inactivos (0 transacciones)",
                "0. Cancelar y volver"
            };
            var activitySelection = ConsoleUi.PromptMenu("Seleccione el filtro de actividad:", activityChoices);
            if (activitySelection.StartsWith("0.")) return;

            if (activitySelection.StartsWith("1."))
            {
                criteria = new AssociateFilterCriteria(BalanceFilter: BalanceFilter.ActiveWithTransactions, SortBy: AssociateSortField.NameAsc);
                viewTitle = "Asociados Activos (Con Movimientos Registrados)";
            }
            else
            {
                criteria = new AssociateFilterCriteria(BalanceFilter: BalanceFilter.InactiveDormant, SortBy: AssociateSortField.RegistrationDateDesc);
                viewTitle = "Asociados Inactivos (Sin Transacciones)";
            }
        }
        else if (selectedOption.StartsWith("5."))
        {
            var sortChoices = new[]
            {
                "1. Saldo: Mayor a Menor",
                "2. Saldo: Menor a Mayor",
                "3. Nombre: A - Z (Alfabético)",
                "4. Nombre: Z - A (Inverso)",
                "5. Fecha de Registro: Más reciente primero",
                "6. Fecha de Registro: Más antiguo primero",
                "7. Número de Documento: Ascendente",
                "0. Cancelar y volver"
            };
            var sortSelection = ConsoleUi.PromptMenu("Seleccione el ordenamiento deseado:", sortChoices);
            if (sortSelection.StartsWith("0.")) return;

            var sortField = sortSelection[..2] switch
            {
                "1." => AssociateSortField.BalanceDesc,
                "2." => AssociateSortField.BalanceAsc,
                "3." => AssociateSortField.NameAsc,
                "4." => AssociateSortField.NameDesc,
                "5." => AssociateSortField.RegistrationDateDesc,
                "6." => AssociateSortField.RegistrationDateAsc,
                "7." => AssociateSortField.DocumentAsc,
                _ => AssociateSortField.NameAsc
            };

            criteria = new AssociateFilterCriteria(SortBy: sortField);
            viewTitle = $"Listado de Asociados (Orden: {sortSelection[3..]})";
        }

        var associates = (await _bankingService.GetFilteredAssociatesAsync(criteria)).ToList();

        ConsoleUi.DisplayPaginatedTable(
            viewTitle,
            associates,
            () =>
            {
                var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
                table.AddColumn(new TableColumn("[bold]TIPO[/]").Centered());
                table.AddColumn(new TableColumn("[bold]DOCUMENTO[/]"));
                table.AddColumn(new TableColumn("[bold]NOMBRE DEL ASOCIADO[/]"));
                table.AddColumn(new TableColumn("[bold]FECHA REGISTRO[/]"));
                table.AddColumn(new TableColumn("[bold]SALDO ACTUAL (COP)[/]").RightAligned());
                return table;
            },
            (table, a, _) =>
            {
                table.AddRow(
                    $"[blue]{a.DocumentType}[/]",
                    $"[bold]{Markup.Escape(a.Document)}[/]",
                    Markup.Escape(a.Name),
                    $"{a.RegistrationDate:yyyy-MM-dd HH:mm}",
                    $"[green]{ConsoleUi.FormatCurrency(a.Balance)}[/]"
                );
            },
            pageSize: 10
        );
    }

    private async Task SearchAssociatesAsync()
    {
        ConsoleUi.PrintHeader("Búsqueda de Asociados");
        var query = ConsoleUi.PromptStringWithCancel("Ingrese documento o nombre para buscar");
        if (query == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var results = (await _bankingService.SearchAssociatesAsync(query)).ToList();

        if (results.Count == 0)
        {
            ConsoleUi.PrintWarningPanel("Sin Resultados", $"No se encontraron asociados que coincidan con '{query}'.");
            ConsoleUi.Pause();
            return;
        }

        ConsoleUi.DisplayPaginatedTable(
            $"Resultados de Búsqueda para '{query}'",
            results,
            () =>
            {
                var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
                table.AddColumn(new TableColumn("[bold]TIPO[/]").Centered());
                table.AddColumn(new TableColumn("[bold]DOCUMENTO[/]"));
                table.AddColumn(new TableColumn("[bold]NOMBRE[/]"));
                table.AddColumn(new TableColumn("[bold]TELÉFONO[/]"));
                table.AddColumn(new TableColumn("[bold]TRANSACCIONES[/]").Centered());
                table.AddColumn(new TableColumn("[bold]SALDO ACTUAL[/]").RightAligned());
                return table;
            },
            (table, a, _) =>
            {
                table.AddRow(
                    $"[blue]{a.DocumentType}[/]",
                    $"[bold]{Markup.Escape(a.Document)}[/]",
                    Markup.Escape(a.Name),
                    $"[cyan]{Markup.Escape(string.IsNullOrEmpty(a.Phone) ? "-" : a.Phone)}[/]",
                    a.Transactions.Count.ToString(),
                    $"[green]{ConsoleUi.FormatCurrency(a.Balance)}[/]"
                );
            },
            pageSize: 10
        );

        // Submenu: select an associate to manage directly
        var choices = new List<string>();
        for (int i = 0; i < results.Count; i++)
        {
            var a = results[i];
            choices.Add($"{i + 1}. {a.Name} ({a.DocumentType} {a.Document}) - Saldo: {ConsoleUi.FormatCurrency(a.Balance)}");
        }
        choices.Add("0. Volver al menú principal");

        ConsoleUi.PrintHeader($"Gestión de Asociados Encontrados ({results.Count})");
        var selected = ConsoleUi.PromptMenu("Seleccione un asociado para ver sus datos u operar:", choices);

        if (selected.StartsWith("0."))
        {
            return;
        }

        var indexStr = selected.Split('.')[0];
        if (int.TryParse(indexStr, out int selectedIdx) && selectedIdx >= 1 && selectedIdx <= results.Count)
        {
            var selectedAssociate = results[selectedIdx - 1];
            await AssociateActionsMenuAsync(selectedAssociate);
        }
    }

    private async Task AssociateActionsMenuAsync(Associate initialAssociate)
    {
        bool back = false;
        var document = initialAssociate.Document;

        while (!back)
        {
            var associate = await _bankingService.GetAssociateByDocumentAsync(document) ?? initialAssociate;

            ConsoleUi.PrintHeader($"Gestión de Asociado: {associate.Name}");
            var actionOptions = new[]
            {
                "1. Ver Ficha Completa y Datos de Contacto",
                "2. Consultar Saldo y Conversión USD (TRM)",
                "3. Ver Historial de Movimientos",
                "4. Realizar Consignación a este Asociado",
                "5. Realizar Retiro a este Asociado",
                "6. Modificar Datos de este Asociado",
                "0. Volver a la Búsqueda / Menú Principal"
            };

            var choice = ConsoleUi.PromptMenu($"Seleccione una operación para {associate.Name} ({associate.Document}):", actionOptions);

            if (choice.StartsWith("1."))
            {
                DisplayAssociateDetailsCard(associate);
                ConsoleUi.Pause();
            }
            else if (choice.StartsWith("2."))
            {
                await ViewBalanceForDocumentAsync(associate.Document);
            }
            else if (choice.StartsWith("3."))
            {
                await ViewTransactionsForDocumentAsync(associate.Document);
            }
            else if (choice.StartsWith("4."))
            {
                await DepositForAssociateAsync(associate);
            }
            else if (choice.StartsWith("5."))
            {
                await WithdrawForAssociateAsync(associate);
            }
            else if (choice.StartsWith("6."))
            {
                await UpdateAssociateForDocumentAsync(associate.Document);
            }
            else if (choice.StartsWith("0."))
            {
                back = true;
            }
        }
    }

    private void DisplayAssociateDetailsCard(Associate associate)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(4));
        grid.AddColumn(new GridColumn());

        grid.AddRow("[grey]Tipo de Documento:[/]", $"[blue]{associate.DocumentType}[/]");
        grid.AddRow("[grey]Número de Documento:[/]", $"[bold]{Markup.Escape(associate.Document)}[/]");
        grid.AddRow("[grey]Nombre Completo:[/]", Markup.Escape(associate.Name));
        grid.AddRow("[grey]Teléfono de Contacto:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Phone) ? "(No registrado)" : associate.Phone)}[/]");
        grid.AddRow("[grey]Correo Electrónico:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Email) ? "(No registrado)" : associate.Email)}[/]");
        grid.AddRow("[grey]Dirección de Residencia:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Address) ? "(No registrado)" : associate.Address)}[/]");
        grid.AddRow("[grey]Fecha de Registro:[/]", $"{associate.RegistrationDate:yyyy-MM-dd HH:mm}");
        grid.AddRow("[grey]Total de Transacciones:[/]", associate.Transactions.Count.ToString());
        grid.AddRow("[grey]Saldo Actual (COP):[/]", $"[bold green]{ConsoleUi.FormatCurrency(associate.Balance)}[/]");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold cyan]FICHA DE DATOS - {Markup.Escape(associate.Name).ToUpper()}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(2, 1, 2, 1)
        };

        AnsiConsole.Write(panel);
    }

    private async Task UpdateAssociateAsync()
    {
        ConsoleUi.PrintHeader("Actualización de Datos de Asociado");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado a actualizar");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        await UpdateAssociateForDocumentAsync(document);
    }

    private async Task UpdateAssociateForDocumentAsync(string document)
    {
        var associate = await _bankingService.GetAssociateByDocumentAsync(document);
        if (associate == null)
        {
            ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
            ConsoleUi.Pause();
            return;
        }

        AnsiConsole.MarkupLine($"Asociado: [bold]{Markup.Escape(associate.Name)}[/] ({associate.DocumentType} {associate.Document})");
        AnsiConsole.MarkupLine($"Teléfono:  [cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Phone) ? "(No registrado)" : associate.Phone)}[/]");
        AnsiConsole.MarkupLine($"Correo:    [cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Email) ? "(No registrado)" : associate.Email)}[/]");
        AnsiConsole.MarkupLine($"Dirección: [cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Address) ? "(No registrado)" : associate.Address)}[/]\n");

        var updateOptions = new[]
        {
            "1. Actualizar Nombre Completo",
            "2. Actualizar Teléfono de Contacto",
            "3. Actualizar Correo Electrónico",
            "4. Actualizar Dirección de Residencia",
            "5. Actualizar Todos los Datos de Contacto (Teléfono, Correo y Dirección)",
            "6. Actualizar Perfil Completo (Nombre y Contacto)",
            "0. Cancelar y volver"
        };

        var selected = ConsoleUi.PromptMenu("Seleccione el dato que desea modificar:", updateOptions);
        if (selected.StartsWith("0.")) return;

        try
        {
            if (selected.StartsWith("1."))
            {
                var newName = ConsoleUi.PromptAssociateNameWithCancel("Ingrese el nuevo nombre completo (1 nombre y 2 apellidos)");
                if (newName == null) return;

                var updated = await _bankingService.UpdateAssociateNameAsync(document, newName);
                ConsoleUi.PrintSuccessPanel("Nombre Actualizado", $"El nombre fue actualizado a '{updated.Name}'.");
            }
            else if (selected.StartsWith("2."))
            {
                var newPhone = ConsoleUi.PromptPhoneWithCancel("Ingrese el nuevo número de teléfono");
                if (newPhone == null) return;

                var updated = await _bankingService.UpdateAssociatePhoneAsync(document, newPhone);
                ConsoleUi.PrintSuccessPanel("Teléfono Actualizado", $"El teléfono fue actualizado a '{updated.Phone}'.");
            }
            else if (selected.StartsWith("3."))
            {
                var newEmail = ConsoleUi.PromptEmailWithCancel("Ingrese el nuevo correo electrónico");
                if (newEmail == null) return;

                var updated = await _bankingService.UpdateAssociateEmailAsync(document, newEmail);
                ConsoleUi.PrintSuccessPanel("Correo Actualizado", $"El correo fue actualizado a '{updated.Email}'.");
            }
            else if (selected.StartsWith("4."))
            {
                var newAddress = ConsoleUi.PromptAddressWithCancel("Ingrese la nueva dirección de residencia");
                if (newAddress == null) return;

                var updated = await _bankingService.UpdateAssociateAddressAsync(document, newAddress);
                ConsoleUi.PrintSuccessPanel("Dirección Actualizada", $"La dirección fue actualizada a '{updated.Address}'.");
            }
            else if (selected.StartsWith("5."))
            {
                var newPhone = ConsoleUi.PromptPhoneWithCancel("Ingrese el nuevo número de teléfono");
                if (newPhone == null) return;

                var newEmail = ConsoleUi.PromptEmailWithCancel("Ingrese el nuevo correo electrónico");
                if (newEmail == null) return;

                var newAddress = ConsoleUi.PromptAddressWithCancel("Ingrese la nueva dirección de residencia");
                if (newAddress == null) return;

                var updated = await _bankingService.UpdateAssociateContactInfoAsync(document, newPhone, newEmail, newAddress);
                ConsoleUi.PrintSuccessPanel("Datos de Contacto Actualizados", 
                    $"Teléfono: {updated.Phone}\nCorreo: {updated.Email}\nDirección: {updated.Address}");
            }
            else if (selected.StartsWith("6."))
            {
                var newName = ConsoleUi.PromptAssociateNameWithCancel("Ingrese el nuevo nombre completo");
                if (newName == null) return;

                var newPhone = ConsoleUi.PromptPhoneWithCancel("Ingrese el nuevo número de teléfono");
                if (newPhone == null) return;

                var newEmail = ConsoleUi.PromptEmailWithCancel("Ingrese el nuevo correo electrónico");
                if (newEmail == null) return;

                var newAddress = ConsoleUi.PromptAddressWithCancel("Ingrese la nueva dirección de residencia");
                if (newAddress == null) return;

                var updated = await _bankingService.UpdateAssociateProfileAsync(document, newName, newPhone, newEmail, newAddress);
                ConsoleUi.PrintSuccessPanel("Perfil Completo Actualizado", 
                    $"Nombre: {updated.Name}\nTeléfono: {updated.Phone}\nCorreo: {updated.Email}\nDirección: {updated.Address}");
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        ConsoleUi.Pause();
    }

    private async Task DeleteAssociateAsync()
    {
        ConsoleUi.PrintHeader("Eliminación de Asociado");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado a eliminar");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        try
        {
            var associate = await _bankingService.GetAssociateByDocumentAsync(document);
            if (associate == null)
            {
                ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
                ConsoleUi.Pause();
                return;
            }

            AnsiConsole.MarkupLine($"Asociado a eliminar: [bold yellow]{Markup.Escape(associate.Name)}[/] ({associate.DocumentType} {associate.Document})");
            var confirmed = ConsoleUi.PromptConfirmation("¿Está seguro de que desea eliminar este asociado de la cooperativa?");

            if (confirmed)
            {
                await _bankingService.DeleteAssociateAsync(document);
                ConsoleUi.PrintSuccessPanel("Asociado Eliminado", $"El asociado con documento '{document}' fue eliminado satisfactoriamente.");
            }
            else
            {
                ConsoleUi.PrintWarningPanel("Operación Cancelada", "La eliminación del asociado fue cancelada.");
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        ConsoleUi.Pause();
    }

    private async Task DepositAsync()
    {
        ConsoleUi.PrintHeader("Consignación (Depósito)");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var associate = await _bankingService.GetAssociateByDocumentAsync(document);
        if (associate == null)
        {
            ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
            ConsoleUi.Pause();
            return;
        }

        await DepositForAssociateAsync(associate);
    }

    private async Task DepositForAssociateAsync(Associate associate)
    {
        ConsoleUi.PrintHeader($"Consignación para: {associate.Name}");
        AnsiConsole.MarkupLine($"Asociado:     [bold]{Markup.Escape(associate.Name)}[/] ({associate.DocumentType} {associate.Document})");
        AnsiConsole.MarkupLine($"Saldo Actual: [bold green]{ConsoleUi.FormatCurrency(associate.Balance)}[/]");

        var amount = ConsoleUi.PromptDecimalWithCancel("Ingrese el monto a consignar (COP)");
        if (amount == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando...");
            ConsoleUi.Pause();
            return;
        }

        try
        {
            var tx = await _bankingService.DepositAsync(associate.Document, amount.Value);
            var refreshed = await _bankingService.GetAssociateByDocumentAsync(associate.Document);

            var details = $"Id Transacción:   {tx.Id}\n" +
                          $"Fecha y Hora:     {tx.Date:yyyy-MM-dd HH:mm:ss}\n" +
                          $"Monto Consignado: {ConsoleUi.FormatCurrency(tx.Amount)}\n" +
                          $"Nuevo Saldo:      {ConsoleUi.FormatCurrency(refreshed?.Balance ?? 0m)}";

            ConsoleUi.PrintSuccessPanel("Consignación Exitosa", details);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        ConsoleUi.Pause();
    }

    private async Task WithdrawAsync()
    {
        ConsoleUi.PrintHeader("Retiro de Fondos");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        var associate = await _bankingService.GetAssociateByDocumentAsync(document);
        if (associate == null)
        {
            ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
            ConsoleUi.Pause();
            return;
        }

        await WithdrawForAssociateAsync(associate);
    }

    private async Task WithdrawForAssociateAsync(Associate associate)
    {
        ConsoleUi.PrintHeader($"Retiro para: {associate.Name}");
        AnsiConsole.MarkupLine($"Asociado:     [bold]{Markup.Escape(associate.Name)}[/] ({associate.DocumentType} {associate.Document})");
        AnsiConsole.MarkupLine($"Saldo Actual: [bold green]{ConsoleUi.FormatCurrency(associate.Balance)}[/]");

        var amount = ConsoleUi.PromptDecimalWithCancel("Ingrese el monto a retirar (COP)");
        if (amount == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando...");
            ConsoleUi.Pause();
            return;
        }

        if (amount.Value > Transaction.HighWithdrawalThreshold)
        {
            ConsoleUi.PrintWarningPanel("Aviso de Comisión", 
                $"Retiros superiores a {ConsoleUi.FormatCurrency(Transaction.HighWithdrawalThreshold)} tienen una comisión automática de {ConsoleUi.FormatCurrency(Transaction.WithdrawalCommissionFee)}.\n" +
                $"Total que se debitará: {ConsoleUi.FormatCurrency(amount.Value + Transaction.WithdrawalCommissionFee)}");
        }

        try
        {
            var tx = await _bankingService.WithdrawAsync(associate.Document, amount.Value);
            var refreshed = await _bankingService.GetAssociateByDocumentAsync(associate.Document);

            var details = $"Id Transacción:   {tx.Id}\n" +
                          $"Fecha y Hora:     {tx.Date:yyyy-MM-dd HH:mm:ss}\n" +
                          $"Monto Retirado:   {ConsoleUi.FormatCurrency(tx.Amount)}\n" +
                          $"Comisión Cobrada: {ConsoleUi.FormatCurrency(tx.Commission)}\n" +
                          $"Total Debitado:   {ConsoleUi.FormatCurrency(tx.Amount + tx.Commission)}\n" +
                          $"Nuevo Saldo:      {ConsoleUi.FormatCurrency(refreshed?.Balance ?? 0m)}";

            ConsoleUi.PrintSuccessPanel("Retiro Realizado con Éxito", details);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        ConsoleUi.Pause();
    }

    private async Task ViewBalanceAsync()
    {
        ConsoleUi.PrintHeader("Consulta de Saldo y Conversión TRM (USD)");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        await ViewBalanceForDocumentAsync(document);
    }

    private async Task ViewBalanceForDocumentAsync(string document)
    {
        var associate = await _bankingService.GetAssociateByDocumentAsync(document);
        if (associate == null)
        {
            ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
            ConsoleUi.Pause();
            return;
        }

        ExchangeRateResult exchangeRate;
        if (!System.Console.IsInputRedirected)
        {
            exchangeRate = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow bold"))
                .StartAsync("Consultando Tasa Representativa del Mercado (TRM) oficial...", async _ =>
                {
                    return await _exchangeRateService.GetUsdExchangeRateAsync();
                });
        }
        else
        {
            exchangeRate = await _exchangeRateService.GetUsdExchangeRateAsync();
        }

        var balanceCop = associate.Balance;
        string usdText;
        string trmText;

        if (exchangeRate.IsSuccess && exchangeRate.Rate > 0)
        {
            var balanceUsd = balanceCop / exchangeRate.Rate;
            usdText = $"[bold green]{ConsoleUi.FormatUsd(balanceUsd)}[/]";
            trmText = $"[cyan]{ConsoleUi.FormatCurrency(exchangeRate.Rate)} / USD[/] (Vigencia: {exchangeRate.ValidFrom:yyyy-MM-dd} a {exchangeRate.ValidTo?.ToString("yyyy-MM-dd") ?? "N/A"})";
        }
        else
        {
            usdText = "[yellow]No disponible[/]";
            trmText = $"[red]Error al consultar TRM ({Markup.Escape(exchangeRate.ErrorMessage ?? "Desconocido")})[/]";
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(4));
        grid.AddColumn(new GridColumn());

        grid.AddRow("[grey]Tipo Documento:[/]", $"[blue]{associate.DocumentType}[/]");
        grid.AddRow("[grey]Documento:[/]", $"[bold]{Markup.Escape(associate.Document)}[/]");
        grid.AddRow("[grey]Nombre Completo:[/]", Markup.Escape(associate.Name));
        grid.AddRow("[grey]Teléfono:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Phone) ? "(No registrado)" : associate.Phone)}[/]");
        grid.AddRow("[grey]Correo Electrónico:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Email) ? "(No registrado)" : associate.Email)}[/]");
        grid.AddRow("[grey]Dirección:[/]", $"[cyan]{Markup.Escape(string.IsNullOrEmpty(associate.Address) ? "(No registrado)" : associate.Address)}[/]");
        grid.AddRow("[grey]Fecha Registro:[/]", $"{associate.RegistrationDate:yyyy-MM-dd HH:mm}");
        grid.AddRow("[grey]Total Transacciones:[/]", associate.Transactions.Count.ToString());
        grid.AddRow("[grey]Saldo en Pesos (COP):[/]", $"[bold green]{ConsoleUi.FormatCurrency(balanceCop)}[/]");
        grid.AddRow("[grey]Tasa TRM Vigente:[/]", trmText);
        grid.AddRow("[grey]Saldo Equivalente (USD):[/]", usdText);

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("[bold cyan]ESTADO DE CUENTA DE ASOCIADO[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(2, 1, 2, 1)
        };

        AnsiConsole.Write(panel);
        ConsoleUi.Pause();
    }

    private async Task ViewTransactionsAsync()
    {
        ConsoleUi.PrintHeader("Historial de Transacciones por Asociado");
        var document = ConsoleUi.PromptStringWithCancel("Ingrese el documento del asociado");
        if (document == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú principal...");
            ConsoleUi.Pause();
            return;
        }

        await ViewTransactionsForDocumentAsync(document);
    }

    private async Task ViewTransactionsForDocumentAsync(string document)
    {
        try
        {
            var associate = await _bankingService.GetAssociateByDocumentAsync(document);
            if (associate == null)
            {
                ConsoleUi.PrintErrorPanel("No Encontrado", $"No se encontró ningún asociado con el documento '{document}'.");
                ConsoleUi.Pause();
                return;
            }

            var transactions = (await _bankingService.GetAssociateTransactionsAsync(document)).ToList();

            ConsoleUi.DisplayPaginatedTable(
                $"Transacciones de: {associate.Name} ({associate.DocumentType} {associate.Document}) - Saldo: {ConsoleUi.FormatCurrency(associate.Balance)}",
                transactions,
                () =>
                {
                    var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
                    table.AddColumn(new TableColumn("[bold]FECHA Y HORA[/]"));
                    table.AddColumn(new TableColumn("[bold]TIPO[/]").Centered());
                    table.AddColumn(new TableColumn("[bold]MONTO (COP)[/]").RightAligned());
                    table.AddColumn(new TableColumn("[bold]COMISIÓN[/]").RightAligned());
                    table.AddColumn(new TableColumn("[bold]ID TRANSACCIÓN[/]").RightAligned());
                    return table;
                },
                (table, t, _) =>
                {
                    var isDeposit = t.Type == TransactionType.Deposit;
                    var typeMarkup = isDeposit ? "[green]Consignación[/]" : "[red]Retiro[/]";
                    var amountMarkup = isDeposit ? $"[green]+{ConsoleUi.FormatCurrency(t.Amount)}[/]" : $"[red]-{ConsoleUi.FormatCurrency(t.Amount)}[/]";
                    var commMarkup = t.Commission > 0 ? $"[yellow]{ConsoleUi.FormatCurrency(t.Commission)}[/]" : "[grey]$0,00[/]";

                    table.AddRow(
                        $"{t.Date:yyyy-MM-dd HH:mm:ss}",
                        typeMarkup,
                        amountMarkup,
                        commMarkup,
                        $"[grey]{t.Id}[/]"
                    );
                },
                pageSize: 10
            );
        }
        catch (Exception ex)
        {
            HandleException(ex);
            ConsoleUi.Pause();
        }
    }

    private async Task ManagementReportsMenuAsync()
    {
        bool back = false;
        var reportChoices = new[]
        {
            "1. Resumen General de la Cooperativa",
            "2. Top 5 Asociados con Mayor Saldo",
            "3. Asociados Inactivos (0 Transacciones)",
            "4. Resumen Consolidado por Rango de Fechas",
            "5. Top 10 Transacciones de Mayor Valor",
            "6. Resumen de Movimientos por Asociado",
            "0. Regresar al Menú Principal"
        };

        while (!back)
        {
            ConsoleUi.PrintHeader("Módulo de Reportes Gerenciales");
            var option = ConsoleUi.PromptMenu("Seleccione un reporte para generar:", reportChoices);

            if (option.StartsWith("1."))
            {
                await ReportCooperativeOverviewAsync();
            }
            else if (option.StartsWith("2."))
            {
                await ReportTop5AssociatesAsync();
            }
            else if (option.StartsWith("3."))
            {
                await ReportDormantAssociatesAsync();
            }
            else if (option.StartsWith("4."))
            {
                await ReportDateRangeSummaryAsync();
            }
            else if (option.StartsWith("5."))
            {
                await ReportTop10LargestTransactionsAsync();
            }
            else if (option.StartsWith("6."))
            {
                await ReportCashierMovementsAsync();
            }
            else if (option.StartsWith("0."))
            {
                back = true;
            }
        }
    }

    private async Task ReportCooperativeOverviewAsync()
    {
        ConsoleUi.PrintHeader("Reporte 1: Resumen General de la Cooperativa");
        var report = await _reportService.GetCooperativeOverviewAsync();

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(4));
        grid.AddColumn(new GridColumn());

        grid.AddRow("[grey]Total de Asociados Registrados:[/]", $"[bold cyan]{report.TotalAssociates}[/]");
        grid.AddRow("[grey]Saldo Total en Custodia:[/]", $"[bold green]{ConsoleUi.FormatCurrency(report.TotalCooperativeBalance)}[/]");
        grid.AddRow("[grey]Saldo Promedio por Asociado:[/]", $"[bold yellow]{ConsoleUi.FormatCurrency(report.AverageBalance)}[/]");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("[bold cyan]CONSOLIDADO COOPERATIVO[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(2, 1, 2, 1)
        };

        AnsiConsole.Write(panel);
        ConsoleUi.Pause();
    }

    private async Task ReportTop5AssociatesAsync()
    {
        ConsoleUi.PrintHeader("Reporte 2: Top 5 Asociados con Mayor Saldo");
        var top = (await _reportService.GetTop5AssociatesByBalanceAsync()).ToList();

        if (top.Count == 0)
        {
            ConsoleUi.PrintWarningPanel("Sin Registros", "No hay asociados registrados para generar el ranking.");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan1);

            table.AddColumn(new TableColumn("[bold]#[/]").Centered());
            table.AddColumn(new TableColumn("[bold]DOCUMENTO[/]"));
            table.AddColumn(new TableColumn("[bold]NOMBRE DEL ASOCIADO[/]"));
            table.AddColumn(new TableColumn("[bold]MOVIMIENTOS[/]").Centered());
            table.AddColumn(new TableColumn("[bold]SALDO TOTAL (COP)[/]").RightAligned());

            int rank = 1;
            foreach (var item in top)
            {
                table.AddRow(
                    rank.ToString(),
                    $"[bold]{Markup.Escape(item.Document)}[/]",
                    Markup.Escape(item.Name),
                    item.TransactionCount.ToString(),
                    $"[bold green]{ConsoleUi.FormatCurrency(item.Balance)}[/]"
                );
                rank++;
            }

            AnsiConsole.Write(table);
        }
        ConsoleUi.Pause();
    }

    private async Task ReportDormantAssociatesAsync()
    {
        var dormant = (await _reportService.GetDormantAssociatesAsync()).ToList();

        ConsoleUi.DisplayPaginatedTable(
            "Reporte 3: Asociados Inactivos (0 Transacciones)",
            dormant,
            () =>
            {
                var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Yellow);
                table.AddColumn(new TableColumn("[bold]DOCUMENTO[/]"));
                table.AddColumn(new TableColumn("[bold]NOMBRE DEL ASOCIADO[/]"));
                table.AddColumn(new TableColumn("[bold]FECHA DE REGISTRO[/]"));
                return table;
            },
            (table, a, _) =>
            {
                table.AddRow(
                    $"[bold]{Markup.Escape(a.Document)}[/]",
                    Markup.Escape(a.Name),
                    $"{a.RegistrationDate:yyyy-MM-dd HH:mm}"
                );
            },
            pageSize: 10
        );
    }

    private async Task ReportDateRangeSummaryAsync()
    {
        ConsoleUi.PrintHeader("Reporte 4: Resumen Consolidado por Rango de Fechas");
        var startDate = ConsoleUi.PromptDateWithCancel("Ingrese la fecha inicial");
        if (startDate == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú de reportes...");
            ConsoleUi.Pause();
            return;
        }

        var endDate = ConsoleUi.PromptDateWithCancel("Ingrese la fecha final");
        if (endDate == null)
        {
            ConsoleUi.PrintWarningPanel("Operación Cancelada", "Regresando al menú de reportes...");
            ConsoleUi.Pause();
            return;
        }

        if (endDate.Value < startDate.Value)
        {
            ConsoleUi.PrintErrorPanel("Rango Inválido", "La fecha final no puede ser anterior a la fecha inicial.");
            ConsoleUi.Pause();
            return;
        }

        var report = await _reportService.GetDateRangeSummaryAsync(startDate.Value, endDate.Value);

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(4));
        grid.AddColumn(new GridColumn());

        grid.AddRow("[grey]Período Analizado:[/]", $"[cyan]{report.StartDate:yyyy-MM-dd}[/] al [cyan]{report.EndDate:yyyy-MM-dd}[/]");
        grid.AddRow("[grey]Total Transacciones:[/]", $"[bold]{report.TotalTransactions}[/]");
        grid.AddRow("[grey]Consignaciones Realizadas:[/]", $"[green]{report.DepositCount}[/] por un total de [bold green]{ConsoleUi.FormatCurrency(report.TotalDeposited)}[/]");
        grid.AddRow("[grey]Retiros Realizados:[/]", $"[red]{report.WithdrawalCount}[/] por un total de [bold red]{ConsoleUi.FormatCurrency(report.TotalWithdrawn)}[/]");
        grid.AddRow("[grey]Comisiones Recaudadas:[/]", $"[yellow]{ConsoleUi.FormatCurrency(report.TotalCommissions)}[/]");
        grid.AddRow("[grey]Flujo Neto del Período:[/]", $"[bold cyan]{ConsoleUi.FormatCurrency(report.NetDifference)}[/]");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("[bold cyan]BALANCE DE CAJA EN EL PERÍODO[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(2, 1, 2, 1)
        };

        AnsiConsole.Write(panel);
        ConsoleUi.Pause();
    }

    private async Task ReportTop10LargestTransactionsAsync()
    {
        ConsoleUi.PrintHeader("Reporte 5: Top 10 Transacciones de Mayor Valor");
        var list = (await _reportService.GetTop10LargestTransactionsAsync()).ToList();

        if (list.Count == 0)
        {
            ConsoleUi.PrintWarningPanel("Sin Transacciones", "No hay transacciones registradas en el sistema.");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan1);

            table.AddColumn(new TableColumn("[bold]#[/]").Centered());
            table.AddColumn(new TableColumn("[bold]FECHA Y HORA[/]"));
            table.AddColumn(new TableColumn("[bold]TIPO[/]").Centered());
            table.AddColumn(new TableColumn("[bold]ASOCIADO[/]"));
            table.AddColumn(new TableColumn("[bold]MONTO (COP)[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]COMISIÓN[/]").RightAligned());

            int rank = 1;
            foreach (var t in list)
            {
                var isDeposit = t.Type == TransactionType.Deposit;
                var typeMarkup = isDeposit ? "[green]Consignación[/]" : "[red]Retiro[/]";
                var commMarkup = t.Commission > 0 ? $"[yellow]{ConsoleUi.FormatCurrency(t.Commission)}[/]" : "[grey]$0,00[/]";

                table.AddRow(
                    rank.ToString(),
                    $"{t.Date:yyyy-MM-dd HH:mm}",
                    typeMarkup,
                    Markup.Escape(t.AssociateName),
                    $"[bold]{ConsoleUi.FormatCurrency(t.Amount)}[/]",
                    commMarkup
                );
                rank++;
            }

            AnsiConsole.Write(table);
        }
        ConsoleUi.Pause();
    }

    private async Task ReportCashierMovementsAsync()
    {
        var list = (await _reportService.GetCashierMovementSummaryPerAssociateAsync()).ToList();

        ConsoleUi.DisplayPaginatedTable(
            "Reporte 6: Resumen de Movimientos por Asociado",
            list,
            () =>
            {
                var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
                table.AddColumn(new TableColumn("[bold]DOCUMENTO[/]"));
                table.AddColumn(new TableColumn("[bold]NOMBRE DEL ASOCIADO[/]"));
                table.AddColumn(new TableColumn("[bold]TXS[/]").Centered());
                table.AddColumn(new TableColumn("[bold]TOTAL INGRESOS[/]").RightAligned());
                table.AddColumn(new TableColumn("[bold]TOTAL EGRESOS[/]").RightAligned());
                table.AddColumn(new TableColumn("[bold]SALDO ACTUAL[/]").RightAligned());
                return table;
            },
            (table, item, _) =>
            {
                table.AddRow(
                    $"[bold]{Markup.Escape(item.AssociateDocument)}[/]",
                    Markup.Escape(item.AssociateName),
                    $"[cyan]{item.TransactionCount}[/]",
                    $"[green]{ConsoleUi.FormatCurrency(item.TotalDeposited)}[/]",
                    $"[red]{ConsoleUi.FormatCurrency(item.TotalWithdrawn + item.TotalCommissions)}[/]",
                    $"[bold green]{ConsoleUi.FormatCurrency(item.CurrentBalance)}[/]"
                );
            },
            pageSize: 10
        );
    }

    private void HandleException(Exception ex)
    {
        switch (ex)
        {
            case InsufficientFundsException ife:
                ConsoleUi.PrintErrorPanel("Fondos Insuficientes",
                    $"Saldo actual: {ConsoleUi.FormatCurrency(ife.CurrentBalance)}\n" +
                    $"Total requerido: {ConsoleUi.FormatCurrency(ife.RequestedAmount + ife.Commission)} (Monto: {ConsoleUi.FormatCurrency(ife.RequestedAmount)} + Comisión: {ConsoleUi.FormatCurrency(ife.Commission)}).");
                break;
            case InvalidTransactionAmountException itae:
                ConsoleUi.PrintErrorPanel("Monto Inválido", itae.Message);
                break;
            case AssociateNotFoundException anfe:
                ConsoleUi.PrintErrorPanel("Asociado No Encontrado", $"No se encontró ningún asociado con el documento '{anfe.Document}'.");
                break;
            case AssociateHasTransactionsException ahte:
                ConsoleUi.PrintErrorPanel("No se puede Eliminar", $"El asociado '{ahte.Document}' tiene transacciones registradas en su historial y no puede ser eliminado.");
                break;
            case DomainException de:
                ConsoleUi.PrintErrorPanel("Regla de Negocio", de.Message);
                break;
            default:
                ConsoleUi.PrintErrorPanel("Error Inesperado", ex.Message);
                break;
        }
    }
}
