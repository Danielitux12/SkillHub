# SkillHub

SkillHub is a workshop and training management platform designed for scheduling short in-person workshops (such as photography, cooking, public speaking, and design) and handling participant enrollments with strict domain rules.

The platform provides a centralized system that prevents scheduling conflicts, duplicate registrations, and capacity overruns, while enabling bulk Excel enrollments, PDF certificate generation, and participant self-service workflows.

## Tech Stack

- **Backend Framework**: .NET 10 (C#)
- **Architecture**: Clean Architecture (Onion / Hexagonal Principles)
- **API**: ASP.NET Core Web API with RESTful conventions
- **Database & ORM**: PostgreSQL with Entity Framework Core (Code-First & Migrations, via Npgsql.EntityFrameworkCore.PostgreSQL)
- **Security & Identity**: ASP.NET Core Identity with JWT (JSON Web Tokens) and Role/Ownership Authorization Policies
- **Document Generation**: QuestPDF (PDF Certificate generation)
- **Spreadsheet Processing**: ClosedXML / MiniExcel (Bulk enrollment import)
- **Frontend Client**: Angular SPA (Single Page Application)

## Solution Architecture & Layer Structure

The project follows Clean Architecture with strict inward dependency flow:

```
SkillHub/
├── SkillHub.Domain/          # Core business entities, enums, domain exceptions, and invariants
├── SkillHub.Application/     # Use cases, interfaces (repositories, PDF, Excel), DTOs, and application logic
├── SkillHub.Infrastructure/  # EF Core DbContext, entity configurations, identity, external services, and repositories
├── SkillHub.API/             # Controllers, middlewares, JWT authentication, authorization policies, and database seeding
├── SkillHub.Web/
│   └── skillhub-angular/
│       └── src/app/
│           ├── services/     # Modelos (interfaces TS), llamadas HTTP a la API, interceptor JWT, lógica de negocio del cliente (AuthService, WorkshopService, EnrollmentService)
│           └── features/     # Componentes/páginas por funcionalidad
│               ├── auth/         # Login, registro
│               ├── workshops/    # Catálogo, detalle
│               └── enrollments/  # Mis inscripciones, inscribirse, cancelar, descargar constancia
└── SkillHub.sln              # Visual Studio solution file
```

### Layer Responsibilities

- **`SkillHub.Domain`**: Contains business models (`Instructor`, `Workshop`, `Participant`, `Enrollment`), status enums, domain exceptions, and encapsulation of business rules (capacity validation, schedule overlap checks, cancellation limits).
- **`SkillHub.Application`**: Defines service and repository contracts (`IWorkshopRepository`, `IEnrollmentRepository`, `IPdfGenerator`, `IExcelImportService`), data transfer objects (DTOs), and application use cases.
- **`SkillHub.Infrastructure`**: Implements persistence via Entity Framework Core, database migrations, ASP.NET Core Identity store, repository implementations, PDF certificate generation, and Excel file parsing.
- **`SkillHub.API`**: Exposes RESTful endpoints, handles HTTP requests/responses, configures JWT Bearer authentication, registers dependency injection, and seeds initial data.
- **`SkillHub.Web`**: Presentation layer with Angular SPA (`skillhub-angular`) covering participant self-service flows (authentication, workshop browsing, enrollment, cancellation, and certificate download).

---

## Getting Started & Setup

<!-- TODO: Detailed setup steps and database execution instructions will be completed in a later milestone -->
*TODO: Add steps to configure and run the solution, including database setup and migrations.*

---

## Employee Credentials

<!-- TODO: Default seeded employee credentials will be provided in a later milestone -->
*TODO: Add seeded employee account credentials.*

---

## Author

<!-- TODO: Author information will be provided in a later milestone -->
*TODO: Add author full name, clan, email, and ID document.*
