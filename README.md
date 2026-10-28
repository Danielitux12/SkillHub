# SkillHub

SkillHub is a workshop and training management platform designed for scheduling short in-person workshops (such as photography, cooking, public speaking, and design) and handling participant enrollments with strict domain rules.

The platform provides a centralized system that prevents scheduling conflicts, duplicate registrations, and capacity overruns, while enabling bulk Excel enrollments, PDF certificate generation, and participant self-service workflows.

---

## Tech Stack

- **Backend Framework**: .NET 10 (C#)
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, API)
- **API**: ASP.NET Core Web API with RESTful conventions
- **Database & ORM**: PostgreSQL with Entity Framework Core (Code-First & Migrations via `Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Security & Identity**: ASP.NET Core Identity with JWT (JSON Web Tokens) and Role-Based Authorization Policies
- **Document Generation**: QuestPDF (Dynamic PDF certificate generation)
- **Spreadsheet Processing**: ClosedXML (Bulk enrollment and data import via Excel)
- **Documentation**: Swagger / OpenAPI with JWT Bearer support
- **Frontend Client**: ASP.NET Core MVC (Classic MVC presentation with Razor Views, ViewModels, Bootstrap 5, and Cookie Authentication in `SkillHub.Web`)

---

## Solution Architecture & Layer Structure

The project follows Clean Architecture with strict inward dependency flow:

```
SkillHub/
├── SkillHub.Domain/          # Core business entities, enums, domain exceptions, and invariants
├── SkillHub.Application/     # Use cases, interfaces (repositories, PDF, Excel), DTOs, and application logic
├── SkillHub.Infrastructure/  # EF Core DbContext, entity configurations, identity, external services, and repositories
├── SkillHub.API/             # RESTful API controllers, middlewares, JWT authentication, and Swagger documentation
├── SkillHub.Web/             # ASP.NET Core MVC presentation layer (Controllers, Razor Views, ViewModels, and Cookie Auth)
└── SkillHub.sln              # Visual Studio solution file
```

### Layer Responsibilities

- **`SkillHub.Domain`**:
  - **Entities**: `Workshop`, `Instructor`, `Participant`, `Enrollment`.
  - **Enums**: `EnrollmentStatus` (`Active`, `Cancelled`), `Roles` (`Employee`, `Participant`).
  - **Domain Invariants & Business Rules**:
    - **Capacity & Availability**: Workshops enforce maximum capacity and calculate active spots in real-time.
    - **Schedule Conflict Prevention**: Detects and forbids overlapping workshop schedules for participants.
    - **Duplicate Enrollment Prevention**: Disallows multiple active enrollments by the same participant in a workshop.
    - **Cancellation Window**: Participants can cancel enrollments only up to 24 hours prior to the workshop start time. Employees can cancel at any time prior to workshop start. Cancelled enrollments cannot be re-cancelled.
    - **Temporal Integrity**: Workshop start dates must be strictly in the future relative to creation time, and end dates must occur after start dates.
  - **Domain Exceptions**: `WorkshopCapacityExceededException`, `DuplicateEnrollmentException`, `ScheduleConflictException`, `CancellationNotAllowedException`, `WorkshopAlreadyStartedException`, `InvalidWorkshopDatesException`, `DomainValidationException`.

- **`SkillHub.Application`**:
  - **Service Contracts**: `IWorkshopService`, `IEnrollmentService`, `ICertificateService`, `IReportService`, `IAuthService` (register, login, change password), `IPdfGenerator`, `IExcelImportService`.
  - **Repository Interfaces & Unit of Work**: `IWorkshopRepository`, `IEnrollmentRepository`, `IParticipantRepository`, `IInstructorRepository`, `IUnitOfWork`.
  - **Data Transfer Objects (DTOs)**: Request and response records for authentication (register, login, change password), workshop management, enrollment lifecycle, and reporting.

- **`SkillHub.Infrastructure`**:
  - **Persistence**: `SkillHubDbContext` configured for PostgreSQL with relational mappings and constraints.
  - **ASP.NET Core Identity**: User store configured with custom `ApplicationUser` (`IdentityUser<Guid>`), handling password hashing, password validation, security stamp refresh, and credential updates.
  - **Repositories**: EF Core implementations of all repository interfaces.
  - **Document Generation**: `QuestPdfCertificateGenerator` producing stylized attendance and completion certificates in PDF format.
  - **Excel Processing**: `ExcelImportService` utilizing ClosedXML for bulk participant enrollments.
  - **Database Seeding**: `DbSeeder` executing automatic role creation, fixed default employee initialization, and optional spreadsheet ingestion.

- **`SkillHub.API`**:
  - **RESTful Controllers**: HTTP endpoints for authentication (registration, login, change password), workshops, enrollments, and operational reports.
  - **Security & Authorization**: JWT Bearer token authentication with role-based policies (`EmployeeOnly`, `ParticipantOnly`).
  - **Exception Handling**: Global ExceptionHandlingMiddleware translating domain and application exceptions into consistent JSON error responses (`{ statusCode, message }`).
  - **OpenAPI / Swagger**: Configured with bearer token authorization for interactive API exploration.
  - **Lifecycle Automation**: Automatic database migration and seed execution upon application startup.

- **`SkillHub.Web`**:
  - **Classic MVC Presentation Layer**: Built with ASP.NET Core MVC and Razor Views, directly consuming Application layer services in accordance with Clean Architecture principles.
  - **Features**:
    - **Home**: Informative landing page with platform highlights, value proposition, and role credentials.
    - **Account**: Login, participant registration, self-service password changing (`/Account/ChangePassword`), user profile dropdown navigation, logout, and access-denied handling with Identity cookie authentication and automatic sign-in refresh.
    - **Workshops**: Responsive catalog of workshops with search/filter, capacity indicators, detailed view, creation form for employees, and participant rosters.
    - **Enrollments**: Self-service enrollment with strict conflict and capacity validations, "Mis Inscripciones" portal, 24h cancellation rule enforcement, PDF certificate downloads, and bulk Excel import (`.xlsx`) with row-by-row error reporting.
    - **Reports**: Analytics report ranking workshops with the highest enrollment demand and occupancy rates.


---

## API Endpoints Reference

Base route: `/api/v1`

### Authentication (`/api/v1/auth`)
| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Public | Register a new participant account |
| `POST` | `/api/v1/auth/login` | Public | Authenticate user credentials and retrieve a JWT Bearer token |
| `POST` | `/api/v1/auth/change-password` | Authenticated | Change user password via Identity (validates current password and updates hash) |


### Workshops (`/api/v1/workshops`)
| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/workshops` | Public | Retrieve all upcoming workshops with available capacity |
| `GET` | `/api/v1/workshops/{id}` | Public | Retrieve detailed workshop information by ID |
| `POST` | `/api/v1/workshops` | `EmployeeOnly` | Create and schedule a new workshop |
| `GET` | `/api/v1/workshops/{id}/enrollments` | `EmployeeOnly` | List all enrollments for a specific workshop |

### Enrollments (`/api/v1/enrollments`)
| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/v1/enrollments` | `ParticipantOnly` | Enroll the authenticated participant into an eligible workshop |
| `GET` | `/api/v1/enrollments/mine` | `ParticipantOnly` | Retrieve all enrollments associated with the authenticated participant |
| `PATCH` | `/api/v1/enrollments/{id}/cancel` | `Authenticated (owner participant or employee)` | Cancel an active enrollment (enforces 24-hour advance cancellation rule) |
| `POST` | `/api/v1/enrollments/import` | `EmployeeOnly` | Bulk import enrollments from an uploaded Excel file (`.xlsx`) |
| `GET` | `/api/v1/enrollments/{id}/certificate` | Authenticated | Download the generated PDF participation certificate |

### Reports (`/api/v1/reports`)
| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/reports/top-workshops` | `EmployeeOnly` | Retrieve statistical report of top workshops ordered by active enrollments |

---

## Getting Started & Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [PostgreSQL](https://www.postgresql.org/) (v14 or higher) or [Docker / Docker Desktop](https://www.docker.com/)

### 1. Database Configuration

Ensure PostgreSQL is running on `localhost:5432`. If using Docker:

```bash
docker run --name skillhub-db \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=SkillHubDb \
  -p 5432:5432 \
  -d postgres
```

The database connection string is located in [`SkillHub.API/appsettings.Development.json`](SkillHub.API/appsettings.Development.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=SkillHubDb;Username=postgres;Password=postgres"
}
```

### 2. Run the Backend API

From the root of the repository:

```bash
dotnet run --project SkillHub.API
```

Upon startup, the application automatically:
1. Applies any pending Entity Framework Core migrations (`context.Database.MigrateAsync()`).
2. Seeds default application roles (`Employee`, `Participant`).
3. Seeds the default staff employee account.

### 3. Access Swagger / OpenAPI

Once the API is running, navigate to:
- **Swagger UI**: [http://localhost:5118/swagger](http://localhost:5118/swagger)

### 4. Run the Classic MVC Web Application

To run the web presentation portal directly:

```bash
dotnet run --project SkillHub.Web
```

Once running, navigate to:
- **SkillHub Web Portal**: [http://localhost:5063](http://localhost:5063)

The MVC application also performs automatic migrations and database seeding upon initial boot.

---

## Seeded Employee Credentials

The platform is pre-seeded with the following staff credentials for administrative tasks:

- **Role**: `Employee`
- **Email**: `empleado@skillhub.com`
- **Password**: `Password123!`

---

## Author

* **Name**: Daniel Isaac Jaraba
* **Repository**: SkillHub
* **Full Name**: Daniel Isaac Jaraba 
* **Clan**: C# 
* **Email**: danielillo3609@gmail.com 
* **ID Document**: 1043141337