\# Ferre Link Manager — Project Context



\## 1. Project Overview



Ferre Link Manager is an internal web-based link management platform designed to centralize access to company applications, internal systems, and network resources.



The application allows administrators to manage links and categories, control access permissions, and monitor important operations. Users can only view and access resources authorized for their roles.



\*\*Project type:\*\* Modular Monolith  

\*\*Backend:\*\* .NET 8 / ASP.NET Core  

\*\*Frontend:\*\* Angular  

\*\*Database:\*\* Microsoft SQL Server



The project is intended to be developed as a production-oriented portfolio project with an emphasis on maintainability, security, and architectural clarity.



\---



\## 2. Technology Stack



\### Backend

\- .NET 8

\- ASP.NET Core Minimal APIs

\- Entity Framework Core

\- ASP.NET Core Identity

\- C#



\### Frontend

\- Angular

\- Angular Material

\- TypeScript



\### Database

\- Microsoft SQL Server

\- Entity Framework Core Migrations



\### Monitoring and Observability

\- OpenTelemetry

\- SigNoz Cloud



\### Testing

\- xUnit

\- ASP.NET Core Integration Testing

\- Architecture Tests



Use stable, well-maintained, commonly adopted packages compatible with the selected .NET version. Prefer official Microsoft packages where appropriate.



Avoid unnecessary dependencies, commercial packages, abandoned libraries, MediatR, and FluentAssertions.



\---



\## 3. Architecture



The project combines the following architectural approaches:



\- Clean Architecture

\- Domain-Driven Design (DDD)

\- Vertical Slice Architecture

\- Event-Driven Design (EDD)

\- Lightweight CQRS



\### Architectural Style



Use a Modular Monolith.



Do not introduce microservices, distributed infrastructure, or unnecessary complexity.



\### Clean Architecture Dependency Rules



The intended dependency direction is:



\- Domain has no project dependencies.

\- Application references Domain.

\- Infrastructure references Application and Domain.

\- API references Application and Infrastructure.



Domain must not depend on Entity Framework Core, ASP.NET Core Identity, or other infrastructure technologies.



\### Domain-Driven Design



Use domain-focused modeling.



Main domain concepts:



\- Link: Aggregate Root

\- LinkUrl: Value Object

\- LinkRoleAccess: Child Entity

\- Category: Aggregate Root

\- CategoryName: Value Object



Aggregates should reference other aggregates through their IDs rather than holding direct object references.



Business rules belong in the Domain layer, not in API endpoints or database configurations.



\### Vertical Slice Architecture



Organize application features by use case rather than creating large technical folders containing every handler, DTO, and validator.



Example feature organization:



\- Links/CreateLink

\- Links/UpdateLink

\- Links/DeleteLink

\- Links/GetMyLinks

\- Categories/CreateCategory

\- Categories/DeleteCategory

\- Users/Register

\- Users/Login



Each feature should contain only the files it actually needs.



Avoid creating unnecessary abstractions or duplicate classes.



\### Lightweight CQRS



Use separate approaches for commands and queries.



Commands:

\- Execute business operations.

\- Use handlers and repositories.

\- Apply domain business rules.

\- Persist changes through IUnitOfWork.



Queries:

\- Use IReadDbContext.

\- Use EF Core AsNoTracking.

\- Project directly into response DTOs.

\- Avoid unnecessary repository abstractions for read-only operations.



Do not introduce a separate read database.



Do not use MediatR. Register handlers with dependency injection and invoke them directly.



\---



\## 4. Intended Solution Structure



Solution name:



Ferre.LinkManager.sln



Backend projects:



\- Ferre.LinkManager.Domain

\- Ferre.LinkManager.Application

\- Ferre.LinkManager.Infrastructure

\- Ferre.LinkManager.Api



Test projects:



\- Ferre.LinkManager.Domain.Tests

\- Ferre.LinkManager.Application.Tests

\- Ferre.LinkManager.Api.IntegrationTests

\- Ferre.LinkManager.ArchitectureTests

\- Ferre.LinkManager.Infrastructure.Tests

Frontend:



\- ferre-link-manager-web



Keep the solution organized and maintain consistent namespaces.



The exact internal folder structure should be designed according to the architecture and explained before implementation.



\---



\## 5. Backend Responsibilities



\### Domain

Contains:

\- Entities

\- Aggregate Roots

\- Value Objects

\- Domain Events

\- Domain Business Rules



Must remain independent of infrastructure.



\### Application

Contains:

\- Use Cases

\- Commands and Queries

\- Handlers

\- DTOs

\- Application Events

\- Interfaces and Abstractions

\- Validation



Important abstractions:



\- IUnitOfWork

\- IReadDbContext

\- ICurrentUser

\- IIdentityService

\- IEmailSender

\- IEventPublisher

\- IEventHandler



\### Infrastructure

Contains:

\- Entity Framework Core DbContext

\- Entity Configurations

\- Migrations

\- Repository Implementations

\- Identity Implementations

\- Database Seeding

\- Event Publisher

\- Email Sender Implementations



\### API

Use Minimal APIs, not MVC Controllers.



Organize endpoint definitions by feature.



Example:



\- Features/Links

\- Features/Categories

\- Features/Users



Program.cs should primarily handle dependency injection, middleware configuration, and explicit endpoint mapping.



\---



\## 6. Authentication and Authorization



Use ASP.NET Core Identity with cookie-based authentication.



Roles:

\- Admin

\- User



Registration is public.



Newly registered users must automatically receive the User role. Users must never be able to select or assign themselves the Admin role.



Create the first Admin through secure database seeding. Never hardcode administrator passwords in source code or commit them to Git.



\### Cookie Configuration



\- Secure cookies

\- HttpOnly

\- SameSite Strict

\- Eight-hour expiration

\- Sliding expiration enabled

\- No persistent remember-me functionality initially

\- Security Stamp validation interval: one minute



Return HTTP 401 for unauthenticated API requests and HTTP 403 for unauthorized requests.



Use ASP.NET Core antiforgery protection for cookie-authenticated state-changing requests.



Angular should send the antiforgery token using the configured XSRF cookie/header mechanism.



\### Email Verification



Email verification is required.



There is no company SMTP configuration currently available.



Use an IEmailSender abstraction.



For development, implement ConsoleEmailSender, which writes verification URLs to the application console.



Do not log verification tokens through telemetry or expose them in request URLs.



Email delivery must happen outside the database transaction. If delivery fails, the user should remain registered and be able to request another verification email.



\---



\## 7. Link and Category Management



Administrators can:



\- Create links.

\- Update links.

\- Delete links.

\- Create categories.

\- Update categories.

\- Manage role-based link visibility.



Users can only access links authorized for their roles.



Each link must have at least one permitted role.



\### URL Restrictions



The first release supports only:



\- HTTP

\- HTTPS



Do not support:

\- UNC paths

\- Mapped network drives

\- file:// URLs



\### Soft Deletion



Links and categories use IsDeleted.



Use EF Core global query filters for soft-deleted records.



Deleted records should not appear in ordinary queries.



Administrators should be able to see inactive but non-deleted links.



Users should only see links that are active, non-deleted, belong to an active and non-deleted category, and are authorized for their roles.



Do not allow category deletion while non-deleted links reference that category. This includes inactive links.



Users are deactivated through IsActive rather than being deleted.



\### Safe Redirect



Use:



GET /api/links/{id:guid}/go



The destination URL must be retrieved from the database.



Never accept a redirect destination directly from the client.



Validate the stored URL and allow only HTTP and HTTPS.



Require appropriate authentication and authorization before redirecting.



\---



\## 8. Event-Driven Design



Use in-process events within the modular monolith.



Distinguish between:



\### Domain Events

Represent important business events originating from domain aggregates.



Example:

\- LinkDeleted

\- LinkCreated



\### Application Events

Represent application-level operations or use-case events.



Example:

\- UserRegistered



\### Event Handlers

React to published events.



Examples:

\- Send verification email.

\- Create audit records.



Do not perform external side effects inside domain aggregate methods or database transactions.



Publish events at appropriate points after successful persistence.



Audit logging should be implemented as an event consumer/handler, not confused with the event itself.



Audit records should store action-time snapshots of important data, such as the link title and URL. Do not rely on joining a potentially deleted Link record to reconstruct historical information.



\---



\## 9. Audit Logging



Maintain an audit trail for important operations.



Examples:

\- Link created.

\- Link updated.

\- Link deleted.

\- Category created.

\- Category deleted.

\- Important user-management operations.



Audit records should contain relevant action information and user identifiers.



Avoid storing unnecessary sensitive information.



\---



\## 10. Database



Use one SQL Server database for both ASP.NET Core Identity and domain entities.



DbContext should extend:



IdentityDbContext<ApplicationUser, ApplicationRole, Guid>



Suggested connection string for local development:



Server=localhost;Database=FerreLinkManager;Trusted\_Connection=True;TrustServerCertificate=True



Use User Secrets or another secure configuration mechanism for sensitive settings.



Never commit passwords, tokens, or production connection credentials.



The existing databases on the development machine must not be modified. Create a separate FerreLinkManager database.



\---



\## 11. Frontend and Hosting



Use Angular with Angular Material.



During development:

\- Angular development server runs on localhost:4200.

\- Configure an Angular proxy for /api requests to the backend.

\- Prefer same-origin API communication.

\- Do not introduce unnecessary CORS configuration.



For production, the Angular build should be served from the ASP.NET Core API application's wwwroot directory.



Use HTTPS.



Provide confirmation dialogs for important operations, including:

\- Create

\- Update

\- Delete

\- Logout

\- Redirect



\---



\## 12. Monitoring and Observability



SigNoz Cloud is the selected monitoring platform.



Use OpenTelemetry for:

\- Application traces

\- Metrics

\- Structured logs



Avoid collecting:

\- Passwords

\- Authentication cookies

\- Authorization headers

\- Verification tokens

\- Request or response bodies

\- SQL parameter values

\- Sensitive personal information



Use UserId instead of email addresses in telemetry wherever possible.



Verify SigNoz Cloud account availability, pricing, and plan limitations before relying on a specific feature.



\---



\## 13. Development Environment



Current local environment:



\- Windows

\- Visual Studio Community 2022

\- .NET SDK 8.0.405

\- .NET SDK 9.0.101

\- Node.js 24.15.0

\- npm 11.2.0

\- Git 2.45.1

\- SQL Server default instance running locally

\- GitHub Desktop



Docker is not part of the initial development environment.



The solution and GitHub repository have been created. The actual application projects have not yet been implemented.



.NET 8 is the initial target. Its support ends on November 10, 2026. Plan an upgrade to .NET 10 LTS before production deployment.



\---



\## 14. Development Rules for AI Assistants



Follow these rules throughout development:



1\. Do not generate the entire project in one response.

2\. Work incrementally, one phase or feature at a time.

3\. Explain architectural decisions before implementing them.

4\. Prefer complete, compilable files when providing implementation code.

5\. Always specify the exact project and folder where a file belongs.

6\. Explain project references and package dependencies.

7\. Avoid unnecessary packages and abstractions.

8\. Do not silently change previously agreed architectural decisions.

9\. Do not introduce microservices or distributed infrastructure.

10\. Never hardcode secrets or credentials.

11\. Consider security in every relevant implementation.

12\. Explain important code so the developer can understand and maintain it.

13\. After each implementation stage, provide build/test commands and verification steps.

14\. Wait for the developer's confirmation or test results before moving to the next stage.

15\. Do not assume an operation succeeded without verification.



The developer wants to understand the architecture and implementation, not merely receive generated code. Explain important concepts with practical examples.



\---



\## 15. Current Project Status



Completed:

\- GitHub repository created.

\- Repository is private.

\- Solution file created.

\- Initial Git commit pushed.

\- .gitignore created.

\- README.md created.



Not yet completed:

\- Backend project creation.

\- Frontend project creation.

\- Architecture implementation.

\- Database creation.

\- Authentication implementation.

\- Feature development.

\- Monitoring integration.

\- Tests.



Next planned phase:



\*\*Phase 1 — Create the backend solution structure in Visual Studio, configure project references, and establish the initial Clean Architecture foundation.\*\*



Do not skip ahead. Start with environment and solution verification before creating projects.

