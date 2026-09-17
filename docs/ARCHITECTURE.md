# WEC ERP Architecture

```text
VB.NET Desktop Client
        |
      HTTPS
        v
ASP.NET Core ERP API
        |
        +---- Entity Framework Core ---- SQL Server
        |
        +---- Shopify / External APIs
```

## Security

- HTTPS for remote API traffic.
- No API keys, tokens, or database passwords in source control.
- Privileged third-party credentials remain server-side.
- Least-privilege database access.
- Authentication and authorization enforced by the API.
- Audit logs for sensitive business operations.
- Transactions for inventory and financial changes.

## Cloud path

Local development uses a local SQL Server and local ASP.NET Core API. Production can move the API and database to cloud infrastructure while keeping the domain and API contracts stable.
