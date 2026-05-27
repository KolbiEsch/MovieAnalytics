# MovieAnalytics

A web application that visualizes your [Letterboxd](https://letterboxd.com) movie diary. Upload your Letterboxd diary export and get detailed statistics about your watching habits — broken down by genre, language, release year, weekday, and more.

Built with ASP.NET Core 6, Entity Framework Core, and the TMDB API.

---

## Features

- Upload your Letterboxd diary CSV to generate personal movie statistics
- Visualize data by year, genre, language, release decade, and day of the week
- See total films watched, hours spent, rewatch counts, and more
- Google OAuth login
- Demo account to explore the app without signing up
- Background TMDB enrichment with a live progress indicator for large libraries

---

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6)
- [Node.js](https://nodejs.org) (for SCSS compilation)
- A [TMDB API key](https://www.themoviedb.org/settings/api) (free)
- A [Google OAuth client](https://console.cloud.google.com) (for login)
- An SMTP email account (for account confirmation emails)

---

## Local Setup

### 1. Clone the repository

```bash
git clone https://github.com/KolbiEsch/MovieAnalytics.git
cd MovieAnalytics/MovieAnalyticsWeb
```

### 2. Set user secrets

The app requires several secrets that are never stored in the codebase. Set them using the .NET user secrets manager:

```bash
dotnet user-secrets set "TMDB-Key" "your-tmdb-api-key"
dotnet user-secrets set "AuthID" "your-google-client-id"
dotnet user-secrets set "AuthSecret" "your-google-client-secret"
dotnet user-secrets set "SMTP-Password" "your-smtp-password"
```

### 3. Create the files directory

```bash
mkdir wwwroot/files
```

### 4. Apply database migrations

Local development uses SQLite - no setup required, the database file is created automatically on first run.

```bash
dotnet ef database update
```

If `dotnet ef` is not found, install the EF Core tools first:

```bash
dotnet tool install --global dotnet-ef --version 6.*
```

### 5. Run the app

```bash
dotnet run
```

Navigate to `https://localhost:7115` in your browser.

---

## Getting Your Letterboxd Data

1. Log in to [letterboxd.com](https://letterboxd.com)
2. Go to **Settings → Data → Export Your Data**
3. Click **Export Your Data** and download the zip file
4. Unzip and locate `diary.csv`
5. Upload `diary.csv` on the MovieAnalytics home page

---

## Expected CSV Format

The app expects the standard Letterboxd diary export format:

```
Date,Name,Year,Letterboxd URI,Rating,Rewatch,Tags,Watched Date
2024-01-05,The Dark Knight,2008,https://boxd.it/...,5,,, 2024-01-05
```

---

## Database

The app uses different databases depending on the environment:

| Environment | Database | Location |
|-------------|----------|----------|
| Development | SQLite | `./Data/MovieAnalytics.db` |
| Production | Azure SQL Server | Azure SQL Database |

### Local (SQLite)
No setup required. The SQLite database is created automatically via `EnsureCreated()` on first startup using the connection string in `appsettings.Development.json`.

### Production (Azure SQL Server)
The production app connects to an Azure SQL Database. The connection string is stored in Azure App Service environment variables and never committed to the repository.

To apply migrations to Azure SQL, set the environment to Production and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet ef database update --connection "your-azure-sql-connection-string"
```

Migrations run automatically on every deployment via `db.Database.Migrate()` in `Program.cs`.

---

## Azure Deployment

The app is configured for deployment to Azure App Service.

### Environment Variables

Set the following in Azure Portal → App Service → **Environment Variables**:

| Name | Value |
|------|-------|
| `TMDB-Key` | Your TMDB API key |
| `AuthID` | Your Google OAuth client ID |
| `AuthSecret` | Your Google OAuth client secret |
| `SMTP-Password` | Your SMTP password |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Your Azure SQL connection string |

> The double underscore `__` in `ConnectionStrings__DefaultConnection` is how Azure App Service maps nested JSON config keys at runtime.

### Deploying

```bash
dotnet publish -c Release
```

Then zip the contents of `bin/Release/net6.0/publish/` and deploy via:
- **Kudu Zip Deploy:** `https://<your-app>.scm.azurewebsites.net/ZipDeployUI`
- **Azure CLI:** `az webapp deploy --resource-group <rg> --name <app> --src-path publish.zip --type zip`

### Google OAuth Redirect URI

Add your Azure URL to the allowed redirect URIs in the Google Cloud Console:

```
https://your-app-name.azurewebsites.net/signin-google
```

---

## Project Structure

```
MovieAnalyticsWeb/
├── Controllers/
│   ├── HomeController.cs       # Main app routes and data endpoints
│   └── DemoController.cs       # Demo account creation and cleanup
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Service.cs              # Core business logic and TMDB enrichment
│   └── TMDBApiClient.cs        # TMDB API wrapper
├── Migrations/                 # EF Core migrations (SQL Server)
├── Models/
│   ├── DiaryMovieData.cs       # Letterboxd diary CSV model
│   ├── AggregateMovieData.cs   # Enriched movie data model
│   ├── MovieStatistics.cs      # Computed statistics model
│   └── ApplicationUser.cs
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml        # Upload page
│   │   └── VisualizeData.cshtml
│   └── Shared/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── files/                  # User uploaded and generated CSVs (not committed)
└── appsettings.json
```

---

## Tech Stack

- **Backend:** ASP.NET Core 6, Entity Framework Core 6
- **Database:** SQLite (local), Azure SQL Server (production)
- **Frontend:** Vanilla JS, Chart.js, SCSS
- **Auth:** ASP.NET Core Identity, Google OAuth
- **Data:** TMDB API, Letterboxd CSV export
- **Hosting:** Azure App Service (Windows)

---

## Notes

- TMDB enrichment runs as a background job. For large libraries (1000+ films) this can take several minutes. A progress indicator is shown while enrichment is in progress.
- The demo account is isolated per session — multiple users can run demos simultaneously without affecting each other.
- Uploaded CSV files and the SQLite database are stored outside `wwwroot` on Azure to persist across deployments.
- Migrations are generated targeting SQL Server. Local development uses `EnsureCreated()` with SQLite instead of running migrations.
---

## License

MIT
