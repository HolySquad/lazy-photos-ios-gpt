# Lazy Photos Documentation

This folder contains comprehensive planning and architecture documentation for building Lazy Photos - a self-hosted Google Photos clone using .NET MAUI for mobile clients and ASP.NET Core for the backend.

## Documentation Structure

Follow these documents in order for a complete understanding of the project:

1. **[01-requirements.md](01-requirements.md)** - Project vision, user stories, and scope
2. **[02-architecture-backend.md](02-architecture-backend.md)** - Backend architecture, API design, and infrastructure
3. **[03-data-model-sync.md](03-data-model-sync.md)** - Database schema, sync strategy, and offline support
4. **[04-maui-foundation.md](04-maui-foundation.md)** - Mobile app structure, MVVM patterns, and cross-platform services
5. **[05-core-features.md](05-core-features.md)** - Feature specifications and API endpoints
6. **[06-performance-ux.md](06-performance-ux.md)** - Performance optimization and UX polish
7. **[07-security-compliance.md](07-security-compliance.md)** - Security measures and compliance requirements
8. **[08-testing-release.md](08-testing-release.md)** - Testing strategy, CI/CD, and release planning

## Additional Resources

- **[openapi-mvp.yaml](openapi-mvp.yaml)** - OpenAPI specification for the REST API

## Quick Links

| Topic | Document |
|-------|----------|
| Tech Stack | [02-architecture-backend.md](02-architecture-backend.md#technology-stack) |
| Database Schema | [03-data-model-sync.md](03-data-model-sync.md#database-schema) |
| API Endpoints | [05-core-features.md](05-core-features.md#api-endpoints) |
| Raspberry Pi Setup | [02-architecture-backend.md](02-architecture-backend.md#raspberry-pi-deployment) |
| Development Phases | [08-testing-release.md](08-testing-release.md#development-phases) |

## Project Principles

- **Self-hosted-first**: Runs efficiently on Raspberry Pi (ARM64) with external SSD storage
- **Privacy-focused**: Your photos stay on your hardware
- **Performance-optimized**: Smooth experience on iPhone 8-class devices
- **Cross-platform**: iOS, Android, and Web clients from shared codebase
- **Google Photos compatible**: Import your existing library via Takeout or API
