# Fandom account module

## What is included

- Registration with unique username and email checks.
- Cookie-based login and logout with `User` and `Admin` roles.
- Real Google and Facebook OAuth sign-in/sign-up flows via ASP.NET Core Authentication.
- Email confirmation and resend flow.
- Password reset by email and password change from the profile.
- Responsive account pages in a shared Fandom visual style.

## Run locally

1. Update `ConnectionStrings:DefaultConnection` in `appsettings.json` if LocalDB is unavailable.
2. For real emails, fill in the `Smtp` section. With an empty SMTP host, messages are safely logged instead, so the flow can still be tested locally.
3. Set `App:ClientBaseUrl` to the address the app actually uses.
4. Create OAuth apps in Google Cloud Console and Meta for Developers, then set:
   - `Authentication:Google:ClientId`
   - `Authentication:Google:ClientSecret`
   - `Authentication:Facebook:AppId`
   - `Authentication:Facebook:AppSecret`
5. Add these redirect URIs in the provider dashboards:
   - `https://your-domain/signin-google`
   - `https://your-domain/signin-facebook`
6. Run `dotnet run` from the project directory.

The database schema is created automatically for a new local database. For production, replace `EnsureCreated()` with EF Core migrations before first deployment.
