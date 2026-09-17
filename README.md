# WEC ERP

Cloud-ready ERP for Water Equipment Trading Co. (WEC).

## Architecture

- Desktop client: Visual Basic .NET / Windows Forms
- Backend: ASP.NET Core Web API
- Database: Microsoft SQL Server
- Data access: Entity Framework Core
- API transport: HTTPS + JSON
- Authentication: token-based authentication
- Integrations: isolated backend services for Shopify and other external APIs

## Development principles

1. No dummy business operations.
2. No secrets or passwords in source control.
3. Business logic stays outside UI forms.
4. Desktop clients communicate with the backend API.
5. Database changes are versioned through migrations.
6. Inventory and financial operations are transactional and auditable.
7. Features are treated as implemented only after they are built and tested.

## Planned modules

- Identity, users, roles and permissions
- Products, brands, categories and units
- Customers and suppliers
- Warehouses and inventory
- Purchasing
- Sales, quotations and invoices
- Payments and returns
- Service and maintenance
- Pool construction projects
- Reporting and audit logs
- Shopify and other API integrations

## Project layout

```text
src/
  WecErp.Desktop/
  WecErp.Api/
  WecErp.Application/
  WecErp.Domain/
  WecErp.Infrastructure/
tests/
  WecErp.Application.Tests/
  WecErp.Api.Tests/
database/
docs/
```

The system is developed locally first and designed for direct migration to cloud hosting later.
