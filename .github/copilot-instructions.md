# GitHub Copilot Instructions for crmHuman

## Project Overview
**crmHuman** is a Vietnamese HR Management System built with ASP.NET Core 7, featuring Employee Management, Candidate Recruitment, Document Management, and Reporting. It follows a 4-layer clean architecture with Razor Pages UI and Dapper ORM for database access.

## Architecture & Layer Structure

### 4-Layer Pattern (Presentation → Business → Data Access → Domain)
- **crmHuman** (Presentation): ASP.NET Core Razor Pages in `Pages/` folder. Each page has a `.cshtml` view file and corresponding `.cshtml.cs` code-behind.
- **VS.Human.Business** (Business Logic): Service classes implementing IServiceBusiness interfaces. All inherit from `BaseBusiness` which provides `IUnitOfWork` access and user claims extraction.
- **VS.Human.Rep** (Data Access): Repository pattern with `RepositoryBase<TModel>` for CRUD operations. `UnitOfWork` aggregates all repositories for transaction-like behavior.
- **VS.Human.Item** (Domain Models): Data transfer objects and entity models shared across layers.

### Dependency Injection Registration
Business services are registered as **Singletons** in `VS.Human.Business/Ioc.cs` via `services.Config()` called in `Program.cs`:
```csharp
services.AddSingleton<IEmpBusiness, EmployeeBusiness>();
services.AddSingleton<ICandidateBusiness, CandidateBusiness>();
// ... ~30+ services registered
```
Repositories are registered similarly in `VS.Human.Rep/Ioc.cs` via `services.ConfigRep()`.

## Key Patterns & Conventions

### Business Layer (VS.Human.Business)
- **Base Class**: All business classes inherit `BaseBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)`
- **UserId Extraction**: Use `GetUserId()` method to extract current user from ClaimsIdentity (claim key: `"userId"`)
- **User Data**: Use `UserClaimsHelper.ExtractUserData()` to get `UserDataView` with userId, UserName, RoleCode, FullName
- **Naming**: Business interfaces use inconsistent casing (e.g., `IEmpBusiness` vs `ICandidateBusiness` vs `IReoportBussiness` [sic]). Follow existing pattern in file.
- **Dapper Usage**: Repositories execute raw SQL via `SqlConnection`. Business logic should not directly use SQL.

### Razor Pages (crmHuman/Pages)
- **Base Classes**: 
  - `BaseModel2` (recommended): Inherits `PageModel`, has `[Authorize]` attribute, provides `GetInfoUser()` method
  - `BaseModel`: Inherits `BaseModel2`, includes `UserDataView` property
- **DI in Constructor**: Page models receive business interfaces as constructor parameters, stored as private fields
- **OnGet/OnPost Methods**: Follow ASP.NET Core naming: `Task<IActionResult> OnGet()`, `Task<IActionResult> OnPost[ActionName]()`
- **Request/Response**: Use `[BindProperty]` for model binding. Pages return `RedirectToPage()` or `Page()`

### Database Access (Dapper + SQL Server)
- **Connection**: Injected via `IUnitOfWork.GetConnection()` (from `RepositoryBase`)
- **Stored Procedures**: Heavy use of SPs for complex queries (e.g., `sp_Employee_getAll`, `sp_Employee_Export`)
- **Migrations**: Flyway-style versioning in `migrations/` folder (V001, V002, etc.). Auto-run via `DatabaseMigrationService` on startup
- **Models**: Query results mapped to domain models using Dapper's `.Query<T>()` extension

### Email & Notifications
- **Email Configuration**: `IEmailConfigBusiness` manages SMTP settings from database
- **Email Service**: `IEmailService.SendAsync()` sends via configured SMTP
- **Template Codes**: Email templates use string constants like `"SUPPORT_REQUEST_CREATE"` to retrieve template body from database
- **Notifications**: `INotificationBusiness` logs notifications separately from emails

### Authentication & Authorization
- **Cookie-Based**: Uses `CookieAuthenticationDefaults.AuthenticationScheme`
- **Claims**: User context stored in `ClaimsIdentity` with keys: `userId`, `UserName`, `RoleCode`, `FullName`, `LineCode`
- **Access Policy**: `EmployeeSystemAccessPolicy.HasSystemAccess(employee)` checks if employee has system permissions on every request
- **Logout Tracking**: `UserActive.DataActiveOnline.MarkLogout()` tracks active sessions

## Build & Deployment Commands

### Development Build
```bash
cd crmHuman
dotnet build ./crmHuman.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

### Watch Mode (Auto-rebuild on file changes)
```bash
dotnet watch run --project ./crmHuman/crmHuman.csproj
```

### Publish/Release Build
```bash
dotnet publish ./crmHuman/crmHuman.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

### Clean
```bash
dotnet clean ./crmHuman/crmHuman.csproj
```

## Critical Developer Workflows

### Adding a New Feature
1. **Define Models**: Add request/response DTOs in `VS.Human.Item/Model/` (e.g., `YourFeatureRequest`, `YourFeatureResponse`)
2. **Data Access**: Create or update repository in `VS.Human.Rep/YourFeatureRep.cs` implementing `IYourFeatureRep`
3. **Register Repo**: Add singleton in `VS.Human.Rep/Ioc.cs` `ConfigRep()` method
4. **Business Logic**: Create `VS.Human.Business/Imp/YourFeatureBusiness.cs` inheriting `BaseBusiness`, implement `IYourFeatureBusiness`
5. **Register Service**: Add singleton in `VS.Human.Business/Ioc.cs` `Config()` method
6. **Razor Page**: Create `crmHuman/Pages/YourFeature.cshtml` and `YourFeature.cshtml.cs` inheriting `BaseModel2`
7. **Inject Service**: Add `IYourFeatureBusiness` to page model constructor

### Accessing Current User
```csharp
// In Business Layer
var userId = GetUserId(); // Extracts from ClaimsIdentity via IHttpContextAccessor

// In Razor Page (BaseModel2)
var userData = UserClaimsHelper.ExtractUserData(User.Identity as ClaimsIdentity);
```

### Working with Repositories & Unit of Work
```csharp
// Business service receives IUnitOfWork, access via _unitOfWork
var employee = await _unitOfWork.EmployeeRep.GetById(userId);
var list = await _unitOfWork.CandidateRep.GetAll(request);
```

## Project-Specific Quirks & Gotchas

1. **Naming Inconsistencies**: Business interface names use both "Business" and "Bussiness" (typo in codebase). Search existing code before naming new services.
2. **Singleton Repositories**: All repositories are singletons, not per-request. State must be managed carefully if added.
3. **No Async/Await Everywhere**: Some older code uses synchronous `.Result` - prefer `async/await` for new code.
4. **Vietnamese Culture Default**: `vi-VN` culture enforced in `Program.cs` - all date formats are `dd/MM/yyyy`
5. **IHttpContextAccessor Required**: Business layer needs `IHttpContextAccessor` injected to extract user claims from current request
6. **Hard-coded MD5 Keys**: `BaseBusiness.getMD5()` uses hardcoded prefix/suffix "KPMG_EV"/"KPMG_PM" for hashing
7. **Database Migrations Auto-run**: `DatabaseMigrationService` runs on startup - database must be initialized before first run

## Testing & Validation

- **No unit tests visible** in main codebase; focus on integration testing through Razor Pages
- **Display Models**: `DisplayModel/` folder contains view-specific models for rendering
- **Excel Export**: Heavy use of EPPlus for employee/candidate export templates
- **File Uploads**: Template imports use `IFormFile` in page models, processed via `IEmployeeImportBusiness`

## References
- Main README: `.../README.md` - Technology stack, features, metrics
- Architecture Docs: `.../docs/ARCHITECTURE.md` - Repository pattern details
- Database Migrations: `.../migrations/` - Flyway-style versioning (V001+)
- Config: `Program.cs` - DI setup, auth configuration, culture settings

---

**Last Updated**: April 2026 | **Branch**: dev_optimize
