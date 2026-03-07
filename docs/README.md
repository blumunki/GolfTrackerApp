# Documentation Index

This directory contains technical documentation for the Golf Tracker App.

## Contents

### [ARCHITECTURE.md](ARCHITECTURE.md)
Comprehensive system architecture document covering:
- System overview and design principles
- Component architecture (web + mobile project structure)
- Data flow patterns (web direct vs mobile API)
- Database schema and provider differences (SQLite dev / SQL Server prod)
- Service layer design
- API design and authentication schemes
- CSS architecture
- Mobile routing
- Deployment

### [mobile-development-setup.md](mobile-development-setup.md)
Mobile development environment setup:
- User secrets configuration for OAuth credentials
- `generate-dev-config.sh` usage
- Troubleshooting common issues

## Contributing to Documentation

When adding features or making significant architectural changes:
1. Update [ARCHITECTURE.md](ARCHITECTURE.md) to reflect the change
2. If adding new database schema, follow the dual-provider checklist in Section 5.1
3. Keep this index up to date
