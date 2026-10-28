using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public const string DefaultEmployeeEmail = "empleado@skillhub.com";
    public const string DefaultEmployeePassword = "Password123!";
    public const string DefaultParticipantPassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider, string? excelPath = null)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SkillHubDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbSeeder");

        // 1. Seed Roles
        await EnsureRoleAsync(roleManager, Roles.Employee);
        await EnsureRoleAsync(roleManager, Roles.Participant);

        // 2. Seed Default Fixed Employee
        await EnsureDefaultEmployeeAsync(context, userManager);

        // 3. Seed Sample Data (3 items for each entity: Instructors, Participants, Workshops, Enrollments)
        await EnsureSampleDataAsync(context, userManager, logger);

        // 4. Seed from Excel if provided and database is empty
        var resolvedPath = ResolveExcelPath(excelPath);
        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            logger?.LogInformation("Found seed Excel at {Path}. Processing seed data...", resolvedPath);
            await SeedFromExcelAsync(context, userManager, resolvedPath, logger);
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }

    private static async Task EnsureDefaultEmployeeAsync(SkillHubDbContext context, UserManager<ApplicationUser> userManager)
    {
        var existingUser = await userManager.FindByEmailAsync(DefaultEmployeeEmail);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = DefaultEmployeeEmail,
                Email = DefaultEmployeeEmail,
                FullName = "SkillHub Staff Instructor",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DefaultEmployeePassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, Roles.Employee);

                var instructorExists = await context.Instructors.AnyAsync(i => i.Email == DefaultEmployeeEmail);
                if (!instructorExists)
                {
                    var instructor = new Instructor(
                        fullName: user.FullName,
                        email: user.Email,
                        specialty: "General Coordinator & Training",
                        id: user.Id
                    );
                    await context.Instructors.AddAsync(instructor);
                    await context.SaveChangesAsync();
                }
            }
        }
    }

    private static async Task EnsureSampleDataAsync(
        SkillHubDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger? logger)
    {
        // 1. Ensure 3 Instructors (including the default staff employee)
        var additionalInstructors = new[]
        {
            (Email: "laura.gomez@skillhub.com", Name: "Laura Gómez", Specialty: "Gastronomía y Pastelería Artesanal"),
            (Email: "andres.torres@skillhub.com", Name: "Andrés Torres", Specialty: "Oratoria y Comunicación Efectiva")
        };

        foreach (var inst in additionalInstructors)
        {
            var user = await userManager.FindByEmailAsync(inst.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = inst.Email,
                    Email = inst.Email,
                    FullName = inst.Name,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, DefaultEmployeePassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Employee);
                }
            }

            var instructorExists = await context.Instructors.AnyAsync(i => i.Email == inst.Email);
            if (!instructorExists)
            {
                var instructor = new Instructor(
                    fullName: inst.Name,
                    email: inst.Email,
                    specialty: inst.Specialty,
                    id: user.Id
                );
                await context.Instructors.AddAsync(instructor);
            }
        }
        await context.SaveChangesAsync();

        // 2. Ensure 3 Participants
        var sampleParticipants = new[]
        {
            (Email: "juan.perez@skillhub.com", Name: "Juan Pérez", Phone: "+57 300 123 4567"),
            (Email: "maria.rodriguez@skillhub.com", Name: "María Rodríguez", Phone: "+57 310 987 6543"),
            (Email: "pedro.gomez@skillhub.com", Name: "Pedro Gómez", Phone: "+57 320 555 8899")
        };

        foreach (var part in sampleParticipants)
        {
            var user = await userManager.FindByEmailAsync(part.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = part.Email,
                    Email = part.Email,
                    FullName = part.Name,
                    PhoneNumber = part.Phone,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, DefaultParticipantPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Participant);
                }
            }

            var participantExists = await context.Participants.AnyAsync(p => p.Email == part.Email);
            if (!participantExists)
            {
                var participant = new Participant(
                    fullName: part.Name,
                    email: part.Email,
                    phoneNumber: part.Phone,
                    id: user.Id
                );
                await context.Participants.AddAsync(participant);
            }
        }
        await context.SaveChangesAsync();

        // 3. Ensure 3 Workshops
        var instructor1 = await context.Instructors.FirstOrDefaultAsync(i => i.Email == DefaultEmployeeEmail);
        var instructor2 = await context.Instructors.FirstOrDefaultAsync(i => i.Email == "laura.gomez@skillhub.com");
        var instructor3 = await context.Instructors.FirstOrDefaultAsync(i => i.Email == "andres.torres@skillhub.com");

        if (instructor1 != null && instructor2 != null && instructor3 != null)
        {
            var now = DateTime.UtcNow;

            var sampleWorkshops = new[]
            {
                (
                    Code: "FOT-101",
                    Title: "Fotografía Urbana e Iluminación Natural",
                    Description: "Aprende a capturar la esencia de la ciudad utilizando luz natural, técnicas de encuadre y composición para fotografía de calle.",
                    InstructorId: instructor1.Id,
                    StartDate: DateTime.SpecifyKind(now.AddDays(5).Date.AddHours(9), DateTimeKind.Utc),
                    EndDate: DateTime.SpecifyKind(now.AddDays(5).Date.AddHours(13), DateTimeKind.Utc),
                    Capacity: 15
                ),
                (
                    Code: "COC-201",
                    Title: "Cocina Mediterránea y Pastas Frescas",
                    Description: "Taller gastronómico presencial donde prepararás masas tradicionales desde cero, salsas emblemáticas y técnicas de emplatado.",
                    InstructorId: instructor2.Id,
                    StartDate: DateTime.SpecifyKind(now.AddDays(8).Date.AddHours(14), DateTimeKind.Utc),
                    EndDate: DateTime.SpecifyKind(now.AddDays(8).Date.AddHours(18), DateTimeKind.Utc),
                    Capacity: 12
                ),
                (
                    Code: "ORA-301",
                    Title: "Oratoria de Alto Impacto y Storytelling",
                    Description: "Domina el manejo de la voz, expresión corporal y técnicas de estructuración de discursos para presentaciones efectivas.",
                    InstructorId: instructor3.Id,
                    StartDate: DateTime.SpecifyKind(now.AddDays(12).Date.AddHours(10), DateTimeKind.Utc),
                    EndDate: DateTime.SpecifyKind(now.AddDays(12).Date.AddHours(14), DateTimeKind.Utc),
                    Capacity: 20
                )
            };

            foreach (var w in sampleWorkshops)
            {
                var workshopExists = await context.Workshops.AnyAsync(wk => wk.Code == w.Code);
                if (!workshopExists)
                {
                    var workshop = new Workshop(
                        code: w.Code,
                        title: w.Title,
                        description: w.Description,
                        instructorId: w.InstructorId,
                        startDate: w.StartDate,
                        endDate: w.EndDate,
                        capacity: w.Capacity,
                        referenceTime: now
                    );
                    await context.Workshops.AddAsync(workshop);
                }
            }
            await context.SaveChangesAsync();
        }

        // 4. Ensure 3 Enrollments
        var juan = await context.Participants.FirstOrDefaultAsync(p => p.Email == "juan.perez@skillhub.com");
        var maria = await context.Participants.FirstOrDefaultAsync(p => p.Email == "maria.rodriguez@skillhub.com");
        var workshopFot = await context.Workshops.FirstOrDefaultAsync(w => w.Code == "FOT-101");
        var workshopCoc = await context.Workshops.FirstOrDefaultAsync(w => w.Code == "COC-201");

        if (juan != null && maria != null && workshopFot != null && workshopCoc != null)
        {
            var enrollmentsToSeed = new[]
            {
                (ParticipantId: juan.Id, WorkshopId: workshopFot.Id, Date: DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-2), DateTimeKind.Utc)),
                (ParticipantId: maria.Id, WorkshopId: workshopFot.Id, Date: DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc)),
                (ParticipantId: maria.Id, WorkshopId: workshopCoc.Id, Date: DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-12), DateTimeKind.Utc))
            };

            foreach (var item in enrollmentsToSeed)
            {
                var enrollmentExists = await context.Enrollments.AnyAsync(e =>
                    e.ParticipantId == item.ParticipantId && e.WorkshopId == item.WorkshopId);

                if (!enrollmentExists)
                {
                    var enrollment = new Enrollment(
                        participantId: item.ParticipantId,
                        workshopId: item.WorkshopId,
                        enrollmentDate: item.Date
                    );
                    await context.Enrollments.AddAsync(enrollment);
                }
            }
            await context.SaveChangesAsync();
        }

        logger?.LogInformation("Sample seed data (3 items per management entity) created successfully.");
    }

    private static async Task SeedFromExcelAsync(
        SkillHubDbContext context,
        UserManager<ApplicationUser> userManager,
        string excelPath,
        ILogger? logger)
    {
        using var workbook = new XLWorkbook(excelPath);

        // A. Instructors
        var instructorsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Instructor", StringComparison.OrdinalIgnoreCase));
        if (instructorsSheet != null)
        {
            var headerRow = instructorsSheet.FirstRowUsed();
            if (headerRow != null)
            {
                var nameCol = FindColumn(headerRow, "FullName", "Name", "Nombre");
                var emailCol = FindColumn(headerRow, "Email", "Correo");
                var specCol = FindColumn(headerRow, "Specialty", "Especialidad");
                var passCol = FindColumn(headerRow, "Password", "Contraseña");

                var lastRow = instructorsSheet.LastRowUsed()?.RowNumber() ?? 0;
                for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                {
                    var row = instructorsSheet.Row(r);
                    if (row.IsEmpty()) continue;

                    var email = row.Cell(emailCol).GetString().Trim().ToLowerInvariant();
                    var name = row.Cell(nameCol).GetString().Trim();
                    var specialty = row.Cell(specCol).GetString().Trim();
                    var rawPassword = passCol > 0 ? row.Cell(passCol).GetString().Trim() : null;
                    var password = !string.IsNullOrWhiteSpace(rawPassword) ? rawPassword : DefaultEmployeePassword;

                    if (string.IsNullOrWhiteSpace(email)) continue;

                    var existingInstructor = await context.Instructors.AnyAsync(i => i.Email == email);
                    if (!existingInstructor)
                    {
                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                Id = Guid.NewGuid(),
                                UserName = email,
                                Email = email,
                                FullName = string.IsNullOrWhiteSpace(name) ? "Instructor" : name,
                                EmailConfirmed = true
                            };
                            await userManager.CreateAsync(user, password);
                            await userManager.AddToRoleAsync(user, Roles.Employee);
                        }

                        var instructor = new Instructor(
                            fullName: user.FullName,
                            email: email,
                            specialty: string.IsNullOrWhiteSpace(specialty) ? "General" : specialty,
                            id: user.Id
                        );
                        await context.Instructors.AddAsync(instructor);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        // B. Participants
        var participantsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Participan", StringComparison.OrdinalIgnoreCase));
        if (participantsSheet != null)
        {
            var headerRow = participantsSheet.FirstRowUsed();
            if (headerRow != null)
            {
                var nameCol = FindColumn(headerRow, "FullName", "Name", "Nombre");
                var emailCol = FindColumn(headerRow, "Email", "Correo");
                var phoneCol = FindColumn(headerRow, "PhoneNumber", "Phone", "Telefono", "Celular");
                var passCol = FindColumn(headerRow, "Password", "Contraseña");

                var lastRow = participantsSheet.LastRowUsed()?.RowNumber() ?? 0;
                for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                {
                    var row = participantsSheet.Row(r);
                    if (row.IsEmpty()) continue;

                    var email = row.Cell(emailCol).GetString().Trim().ToLowerInvariant();
                    var name = row.Cell(nameCol).GetString().Trim();
                    var phone = phoneCol > 0 ? row.Cell(phoneCol).GetString().Trim() : null;
                    var rawPassword = passCol > 0 ? row.Cell(passCol).GetString().Trim() : null;
                    var password = !string.IsNullOrWhiteSpace(rawPassword) ? rawPassword : DefaultParticipantPassword;

                    if (string.IsNullOrWhiteSpace(email)) continue;

                    var participantExists = await context.Participants.AnyAsync(p => p.Email == email);
                    if (!participantExists)
                    {
                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                Id = Guid.NewGuid(),
                                UserName = email,
                                Email = email,
                                FullName = string.IsNullOrWhiteSpace(name) ? "Participant" : name,
                                PhoneNumber = phone,
                                EmailConfirmed = true
                            };
                            await userManager.CreateAsync(user, password);
                            await userManager.AddToRoleAsync(user, Roles.Participant);
                        }

                        var participant = new Participant(
                            fullName: user.FullName,
                            email: email,
                            phoneNumber: phone,
                            id: user.Id
                        );
                        await context.Participants.AddAsync(participant);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        // C. Workshops
        var workshopsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Workshop", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Taller", StringComparison.OrdinalIgnoreCase));
        if (workshopsSheet != null)
        {
            var headerRow = workshopsSheet.FirstRowUsed();
            if (headerRow != null)
            {
                var codeCol = FindColumn(headerRow, "Code", "Codigo", "WorkshopCode");
                var titleCol = FindColumn(headerRow, "Title", "Titulo", "Nombre");
                var descCol = FindColumn(headerRow, "Description", "Descripcion");
                var instructorEmailCol = FindColumn(headerRow, "InstructorEmail", "EmailInstructor", "Instructor");
                var startCol = FindColumn(headerRow, "StartDateTime", "StartDate", "FechaInicio", "Start");
                var endCol = FindColumn(headerRow, "EndDateTime", "EndDate", "FechaFin", "End");
                var capCol = FindColumn(headerRow, "Capacity", "Cupo", "CupoMaximo");

                var lastRow = workshopsSheet.LastRowUsed()?.RowNumber() ?? 0;
                for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                {
                    var row = workshopsSheet.Row(r);
                    if (row.IsEmpty()) continue;

                    var code = row.Cell(codeCol).GetString().Trim().ToUpperInvariant();
                    var title = row.Cell(titleCol).GetString().Trim();
                    var desc = descCol > 0 ? row.Cell(descCol).GetString().Trim() : title;
                    var instructorEmail = row.Cell(instructorEmailCol).GetString().Trim().ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(code)) continue;

                    var workshopExists = await context.Workshops.AnyAsync(w => w.Code == code);
                    if (!workshopExists)
                    {
                        var instructor = await context.Instructors.FirstOrDefaultAsync(i => i.Email == instructorEmail);
                        if (instructor == null)
                        {
                            instructor = await context.Instructors.FirstOrDefaultAsync();
                        }

                        if (instructor == null) continue;

                        DateTime startDate;
                        try
                        {
                            startDate = row.Cell(startCol).GetDateTime();
                        }
                        catch
                        {
                            startDate = DateTime.UtcNow.AddDays(7);
                        }

                        DateTime endDate;
                        try
                        {
                            endDate = row.Cell(endCol).GetDateTime();
                        }
                        catch
                        {
                            endDate = startDate.AddHours(3);
                        }

                        if (endDate <= startDate)
                            endDate = startDate.AddHours(2);

                        if (startDate <= DateTime.UtcNow)
                            startDate = DateTime.UtcNow.AddDays(5);
                        if (endDate <= startDate)
                            endDate = startDate.AddHours(2);

                        int capacity;
                        try
                        {
                            capacity = (int)row.Cell(capCol).GetDouble();
                        }
                        catch
                        {
                            capacity = 20;
                        }
                        if (capacity <= 0) capacity = 20;

                        var workshop = new Workshop(
                            code: code,
                            title: string.IsNullOrWhiteSpace(title) ? code : title,
                            description: string.IsNullOrWhiteSpace(desc) ? title : desc,
                            instructorId: instructor.Id,
                            startDate: DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
                            endDate: DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
                            capacity: capacity,
                            referenceTime: DateTime.UtcNow
                        );

                        await context.Workshops.AddAsync(workshop);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }

    private static int FindColumn(IXLRow headerRow, params string[] possibleNames)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            foreach (var name in possibleNames)
            {
                if (header.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return cell.Address.ColumnNumber;
            }
        }
        return -1;
    }

    private static string? ResolveExcelPath(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return path;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedData", "SkillHub_Data.xlsx"),
            Path.Combine(AppContext.BaseDirectory, "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "SkillHub.API", "SeedData", "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "SkillHub.API", "SeedData", "SkillHub_Data.xlsx")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
