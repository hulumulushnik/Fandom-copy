# Fandom account module

## What is included

- Registration with unique username and email checks.
- Cookie-based login and logout with `User` and `Admin` roles.
- Email confirmation and resend flow.
- Password reset by email and password change from the profile.
- Responsive account pages in a shared Fandom visual style.

## Run locally

1. Update `ConnectionStrings:DefaultConnection` in `appsettings.json` if LocalDB is unavailable.
2. For real emails, fill in the `Smtp` section. With an empty SMTP host, messages are safely logged instead, so the flow can still be tested locally.
3. Set `App:ClientBaseUrl` to the address the app actually uses, then run `dotnet run` from the project directory.

The database schema is created automatically for a new local database. For production, replace `EnsureCreated()` with EF Core migrations before first deployment.
