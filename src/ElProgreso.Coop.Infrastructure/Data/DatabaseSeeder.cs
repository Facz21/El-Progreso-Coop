using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly string[] FirstNames =
    {
        "Juan", "Carlos", "Andres", "Santiago", "Mateo", "Alejandro", "Daniel", "Sebastian", "Diego", "Felipe",
        "David", "Nicolas", "Julian", "Gabriel", "Esteban", "Maria", "Camila", "Laura", "Valentina", "Daniela",
        "Sofia", "Paula", "Natalia", "Mariana", "Andrea", "Juliana", "Carolina", "Gabriela", "Isabella", "Lucia"
    };

    private static readonly string[] MiddleNames =
    {
        "Alberto", "Fernando", "Eduardo", "Javier", "Enrique", "Manuel", "Ignacio", "Antonio", "Guillermo", "Rodrigo",
        "Fernanda", "Alejandra", "Victoria", "Teresa", "Elena", "Beatriz", "Patricia", "Cristina", "Eugenia", "Pilar"
    };

    private static readonly string[] Surnames =
    {
        "Rodriguez", "Gomez", "Mendoza", "Perez", "Lopez", "Garcia", "Hernandez", "Martinez", "Torres", "Diaz",
        "Alvarez", "Romero", "Ramirez", "Suarez", "Vargas", "Castro", "Rios", "Castillo", "Ortiz", "Silva",
        "Morales", "Jimenez", "Guerrero", "Navarro", "Rojas", "Cardona", "Mejia", "Giraldo", "Zapata", "Osorio",
        "Guzman", "Salazar", "Valencia", "Restrepo", "Pineda"
    };

    private static readonly string[] StreetTypes = { "Calle", "Carrera", "Avenida", "Diagonal", "Transversal" };
    private static readonly string[] Cities = { "Bogotá", "Medellín", "Cali", "Barranquilla", "Bucaramanga", "Pereira", "Manizales", "Cartagena" };
    private static readonly string[] EmailDomains = { "gmail.com", "outlook.com", "hotmail.com", "coopelprogreso.com", "yahoo.es" };

    public static async Task SeedIfEmptyAsync(
        IAssociateRepository associateRepo,
        ITransactionRepository transactionRepo)
    {
        var existing = (await associateRepo.GetAllAsync()).ToList();
        if (existing.Count >= 300)
        {
            return;
        }

        var existingDocs = existing.Select(a => a.Document).ToHashSet();

        // 1. Primary Sample Associates (if not already existing)
        if (!existingDocs.Contains("1020304050"))
        {
            var a1 = new Associate(
                "1020304050",
                "Carlos Alberto Mendoza Perez",
                DocumentType.CC,
                "3104567890",
                "carlos.mendoza@email.com",
                "Carrera 15 # 85-30, Bogotá",
                new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a1);
            var t1_1 = a1.CreateDeposit(5_000_000m, new DateTime(2026, 1, 20, 10, 30, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t1_1);
            var t1_2 = a1.CreateWithdrawal(1_200_000m, new DateTime(2026, 2, 10, 15, 45, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t1_2);
            existingDocs.Add("1020304050");
        }

        if (!existingDocs.Contains("1030405060"))
        {
            var a2 = new Associate(
                "1030405060",
                "Maria Camila Rodriguez Ortiz",
                DocumentType.CC,
                "3201234567",
                "maria.rodriguez@email.com",
                "Calle 53 # 45-12, Medellín",
                new DateTime(2026, 2, 1, 11, 15, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a2);
            var t2_1 = a2.CreateDeposit(4_500_000m, new DateTime(2026, 2, 5, 14, 0, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t2_1);
            var t2_2 = a2.CreateDeposit(2_000_000m, new DateTime(2026, 2, 20, 16, 30, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t2_2);
            existingDocs.Add("1030405060");
        }

        if (!existingDocs.Contains("1098765432"))
        {
            var a3 = new Associate(
                "1098765432",
                "Juan David Lopez Gomez",
                DocumentType.TI,
                "3159876543",
                "juan.lopez@email.com",
                "Avenida 6N # 28-15, Cali",
                new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a3);
            var t3_1 = a3.CreateDeposit(600_000m, new DateTime(2026, 3, 5, 9, 45, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t3_1);
            var t3_2 = a3.CreateWithdrawal(150_000m, new DateTime(2026, 3, 15, 11, 20, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t3_2);
            existingDocs.Add("1098765432");
        }

        if (!existingDocs.Contains("E1234567"))
        {
            var a4 = new Associate(
                "E1234567",
                "Jean Pierre Dubois Dupont",
                DocumentType.CE,
                "3007654321",
                "jean.dubois@email.com",
                "Carrera 7 # 116-50, Bogotá",
                new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a4);
            var t4_1 = a4.CreateDeposit(3_700_000m, new DateTime(2026, 3, 12, 10, 15, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t4_1);
            var t4_2 = a4.CreateWithdrawal(1_500_000m, new DateTime(2026, 4, 1, 16, 0, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t4_2);
            existingDocs.Add("E1234567");
        }

        if (!existingDocs.Contains("900123456-1"))
        {
            var a5 = new Associate(
                "900123456-1",
                "Agropecuaria El Porvenir SAS Gomez Perez",
                DocumentType.NIT,
                "6013456789",
                "contacto@elporvenir.com.co",
                "Vía Siberia Km 3, Cundinamarca",
                new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a5);
            var t5_1 = a5.CreateDeposit(10_000_000m, new DateTime(2026, 1, 10, 9, 30, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t5_1);
            var t5_2 = a5.CreateDeposit(5_000_000m, new DateTime(2026, 2, 15, 11, 0, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t5_2);
            var t5_3 = a5.CreateWithdrawal(2_600_000m, new DateTime(2026, 3, 10, 14, 20, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t5_3);
            var t5_4 = a5.CreateWithdrawal(10_000m, new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc));
            await transactionRepo.AddAsync(t5_4);
            existingDocs.Add("900123456-1");
        }

        if (!existingDocs.Contains("PA9876543"))
        {
            var a6 = new Associate(
                "PA9876543",
                "Ana Sofia Ramirez Fernandez",
                DocumentType.PAS,
                "3183456789",
                "ana.ramirez@email.com",
                "Diagonal 45 # 104-20, Cartagena",
                new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
            );
            await associateRepo.AddAsync(a6);
            existingDocs.Add("PA9876543");
        }

        // 2. Procedurally generate remaining associates up to exactly 300
        int currentCount = existingDocs.Count;
        int targetTotal = 300;
        var rng = new Random(42); // deterministic seed for consistent, realistic data

        int sequentialId = 1;
        var baseDate = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);

        while (currentCount < targetTotal)
        {
            var docTypeRoll = rng.Next(100);
            DocumentType docType;
            string docNum;

            if (docTypeRoll < 75) // 75% CC
            {
                docType = DocumentType.CC;
                docNum = (1010000000L + sequentialId).ToString();
            }
            else if (docTypeRoll < 85) // 10% TI
            {
                docType = DocumentType.TI;
                docNum = (1090000000L + sequentialId).ToString();
            }
            else if (docTypeRoll < 92) // 7% CE
            {
                docType = DocumentType.CE;
                docNum = $"E{7000000 + sequentialId}";
            }
            else if (docTypeRoll < 97) // 5% NIT
            {
                docType = DocumentType.NIT;
                docNum = $"900{sequentialId:D6}-{(sequentialId % 9) + 1}";
            }
            else // 3% PAS
            {
                docType = DocumentType.PAS;
                docNum = $"PA{8000000 + sequentialId}";
            }

            sequentialId++;

            if (existingDocs.Contains(docNum))
            {
                continue;
            }

            // Generate Name (min 3 words)
            var fn = FirstNames[rng.Next(FirstNames.Length)];
            var mn = MiddleNames[rng.Next(MiddleNames.Length)];
            var s1 = Surnames[rng.Next(Surnames.Length)];
            var s2 = Surnames[rng.Next(Surnames.Length)];
            var fullName = $"{fn} {mn} {s1} {s2}";

            // Generate Contact Info
            var phonePrefix = rng.Next(310, 325);
            var phoneSuffix = rng.Next(1000000, 9999999);
            var phone = $"{phonePrefix}{phoneSuffix:D7}";

            var cleanFn = fn.ToLowerInvariant();
            var cleanS1 = s1.ToLowerInvariant();
            var domain = EmailDomains[rng.Next(EmailDomains.Length)];
            var email = $"{cleanFn}.{cleanS1}{sequentialId}@{domain}";

            var stType = StreetTypes[rng.Next(StreetTypes.Length)];
            var stNum = rng.Next(1, 190);
            var stHouse1 = rng.Next(1, 150);
            var stHouse2 = rng.Next(1, 99);
            var city = Cities[rng.Next(Cities.Length)];
            var address = $"{stType} {stNum} # {stHouse1}-{stHouse2}, {city}";

            var regDaysOffset = rng.Next(0, 365);
            var regDate = baseDate.AddDays(regDaysOffset).AddHours(rng.Next(8, 17)).AddMinutes(rng.Next(0, 59));

            var associate = new Associate(docNum, fullName, docType, phone, email, address, regDate);
            await associateRepo.AddAsync(associate);
            existingDocs.Add(docNum);
            currentCount++;

            // Transaction pattern:
            // ~20% dormant (0 txs)
            // ~55% regular active (1 to 3 deposits, 0 to 1 withdrawal)
            // ~25% high value savers (multiple deposits, large balances)
            var profileRoll = rng.Next(100);

            if (profileRoll < 20)
            {
                // Dormant - 0 transactions
                continue;
            }

            if (profileRoll < 75)
            {
                // Regular active saver
                int depositCount = rng.Next(1, 4);
                var currentTxDate = regDate.AddDays(rng.Next(1, 10));

                for (int d = 0; d < depositCount; d++)
                {
                    decimal depAmount = rng.Next(1, 30) * 100_000m; // 100k to 3M
                    var dep = associate.CreateDeposit(depAmount, currentTxDate);
                    await transactionRepo.AddAsync(dep);
                    currentTxDate = currentTxDate.AddDays(rng.Next(5, 20));
                }

                // Maybe 1 withdrawal if balance > 300k
                if (associate.Balance > 300_000m && rng.Next(100) < 60)
                {
                    decimal maxWithdraw = Math.Min(associate.Balance - 50_000m, 1_500_000m);
                    if (maxWithdraw > 100_000m)
                    {
                        decimal rawWith = maxWithdraw * (decimal)(0.3 + rng.NextDouble() * 0.5);
                        decimal withAmount = Math.Round(rawWith / 10_000m) * 10_000m;
                        if (withAmount > 0)
                        {
                            var comm = Transaction.CalculateCommission(TransactionType.Withdrawal, withAmount);
                            if (associate.Balance >= withAmount + comm)
                            {
                                var w = associate.CreateWithdrawal(withAmount, currentTxDate.AddDays(rng.Next(2, 10)));
                                await transactionRepo.AddAsync(w);
                            }
                        }
                    }
                }
            }
            else
            {
                // High-value corporate or prime associate
                int depositCount = rng.Next(2, 5);
                var currentTxDate = regDate.AddDays(rng.Next(1, 5));

                for (int d = 0; d < depositCount; d++)
                {
                    decimal depAmount = rng.Next(20, 100) * 200_000m; // 4M to 20M
                    var dep = associate.CreateDeposit(depAmount, currentTxDate);
                    await transactionRepo.AddAsync(dep);
                    currentTxDate = currentTxDate.AddDays(rng.Next(5, 15));
                }

                if (associate.Balance > 2_000_000m && rng.Next(100) < 70)
                {
                    decimal withAmount = rng.Next(12, 35) * 100_000m; // 1.2M to 3.5M (triggers 8k fee)
                    var comm = Transaction.CalculateCommission(TransactionType.Withdrawal, withAmount);
                    if (associate.Balance >= withAmount + comm)
                    {
                        var w = associate.CreateWithdrawal(withAmount, currentTxDate.AddDays(rng.Next(5, 15)));
                        await transactionRepo.AddAsync(w);
                    }
                }
            }
        }
    }
}
