# Ferre Link Manager

Ferre Link Manager is an internal web-based link management platform designed to centralize access to company applications, internal systems, and network resources.

The project aims to provide a secure and role-based environment where users can access authorized resources through a centralized interface.

## Project Goals

- Centralize company application and resource links.
- Manage links and categories through an administrative interface.
- Control link visibility based on user roles.
- Provide secure authentication and authorization.
- Maintain an audit trail of important operations.
- Monitor application health and performance.

## Technology Stack

### Backend
- .NET 8
- ASP.NET Core Minimal APIs
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server

### Frontend
- Angular
- Angular Material

### Architecture
- Clean Architecture
- Domain-Driven Design (DDD)
- Vertical Slice Architecture
- Event-Driven Design (EDD)
- Lightweight CQRS

### Monitoring
- OpenTelemetry
- SigNoz Cloud

## Key Architectural Decisions

- Modular monolith architecture.
- Feature-oriented API endpoint organization.
- Domain-focused business rules and aggregates.
- Separate command handling and read operations.
- Role-based resource access.
- Soft deletion for links and categories.

## Development Status

This project is currently under development. Architecture, implementation details, and features may evolve during development.

## License

This project is currently not licensed for public reuse.
