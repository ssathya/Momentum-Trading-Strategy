# GEMINI.md - Momentum Trading Strategy Developer Guide

Welcome! This document provides an architectural index, project guidelines, and operational procedures for the **Momentum Trading Strategy** repository. It serves as instructional context for AI agents and human developers working on this codebase.

---

## 1. Project Overview

The **Momentum Trading Strategy** is a C# .NET 10 solution that implements a quantitative momentum stock selection strategy based on the book *The best strategies to invest in stocks and ETFs while controlling risk* by Luca Giusti.

### The Core Momentum Strategy
*   **Trading Frequency:** Monthly.
*   **Cascading Selection Process:**
    1.  **Yearly Filter:** Select the top **50** tickers of the NASDAQ-100 (and other indexes/ETFs) based on the annual (**12-month**) percentage change.
    2.  **Half-Yearly Filter:** From the 50, select the top **30** tickers based on the half-yearly (**6-month**) percentage change.
    3.  **Quarterly Filter:** From the 30, select the top **10** best performing tickers based on the quarterly (**3-month**) percentage change.

These final 10 tickers represent the active portfolio selection for that monthly period.

---

## 2. Directory & Project Structure

The solution contains multiple modular C# projects organizing the system's pipeline:

```text
C:\Users\sridh\source\repos\Momentum Trading Strategy\
├── AppCommon/                 # Shared libraries, utilities, calendar services
│   ├── NYSECalendar/          # Non-working day & business trading day logic for NYSE
│   └── ServiceHandler.cs      # Database & Logging setup, configuration building
├── ComputeMomentum/           # Backend processor for momentum selection and slope indicators
├── ComputeSlopeSummary/       # Backend summary analyzer and Excel exporter
├── ModelGeneration/           # EF Core migration helper project (used for design-time build only)
├── Models/                    # Shared Entity Framework database models and DB Context
│   └── Migrations/            # EF Core database schema migrations
├── Notification/              # Email alert and reporting services
├── Presentation/              # Blazor Server Web App with Radzen components (Dashboard)
├── SecuritiesMaintain/        # Downloads / maintains index component lists (Dow, Nasdaq, S&P 500)
└── SecurityPriceMaintain/     # Retries and saves historical pricing (OHLCV) for assets
```

### Detailed Project Descriptions

1.  **`Models`**:
    *   Holds the EF Core database context (`AppDbContext`) and entities mapped to PostgreSQL.
    *   **Models Included:**
        *   `IndexComponent`: Tracks index constituents (S&P 500, Nasdaq, Dow, special ETFs) and updated date.
        *   `PriceByDate`: Daily OHLCV price bars per ticker.
        *   `SelectedTicker`: The selected top 10 momentum tickers for a specific historical date with closing prices and percentage gains.
        *   `TickerSlope`: Tracks mathematical regression slopes for tickers across different durations.
        *   `SlopeSummary`: Tracks aggregated changes in slope from the start to the end of a given period.
2.  **`AppCommon`**:
    *   `ServiceHandler`: Registers DB connections (`Npgsql` for PostgreSQL) with `IDbContextFactory`, configures Serilog, registers common HTTP clients with proper User-Agent headers, and builds the unified configurations.
    *   `NYSECalendar`: A custom module (`TradingCalendar` & `ExchangeCalendar`) mapping NYSE-specific market holidays and weekends to identify proper trading days.
3.  **`SecuritiesMaintain`**:
    *   A scheduled job/service updating the active constituent tickers of indices (Nasdaq-100, S&P 500, Dow Jones) and special ETFs into the database `IndexComponents` table.
4.  **`SecurityPriceMaintain`**:
    *   Retrieves daily historical stock prices (using Yahoo Finance APIs via `OoplesFinance.YahooFinanceAPI` or `YahooQuotesApi`) for all active index constituents and persists them inside `PriceByDate` using high-speed bulk inserts.
5.  **`ComputeMomentum`**:
    *   Main core processor executing monthly cascading momentum calculations on historical pricing data and saving the final chosen portfolio into the `SelectedTickers` table.
    *   Computes regression slopes using the `Skender.Stock.Indicators` library for various time periods (`Yearly`, `HalfYearly`, `Quarterly`, `Monthly`).
6.  **`ComputeSlopeSummary`**:
    *   Generates `SlopeSummary` records by analyzing how regression slopes change over periods (Monthly, Quarterly, etc.), and generates reports (including Excel export functionality).
7.  **`Notification`**:
    *   Handles SMTP-based email notifications about selected tickers or service statuses using `MailKit` and local settings.
8.  **`Presentation`**:
    *   The frontend dashboard. An interactive ASP.NET Core Blazor Server application using **Radzen Components** to display the portfolio selection history, monthly returns charts, and individual stock performance.
9.  **`ModelGeneration`**:
    *   A helper project to facilitate running Entity Framework migrations against a local/remote PostgreSQL instance without pulling design-time tooling into the production lambdas/workers.

---

## 3. Technology Stack & Key Dependencies

*   **Runtime:** .NET 10.0
*   **Database:** PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
*   **ORM:** Entity Framework Core with high-speed batch operations via `EFCore.BulkExtensions`
*   **Web Framework:** Blazor Server with **Radzen Blazor Components**
*   **Log System:** Serilog (writing to Console and daily rotating `.log` files in `Path.GetTempPath()`)
*   **Indicators & Pricing:**
    *   `Skender.Stock.Indicators` for indicators (e.g., Slope of Regression Line, Quote model mapping)
    *   `YahooQuotesApi` / `OoplesFinance.YahooFinanceAPI` for historical price fetching

---

## 4. Configuration & AWS Integration

The application relies on a combination of local configuration and cloud parameters:

1.  **Configuration Provider Order (`AppCommon/ServiceHandler.cs`):**
    *   `appsettings.json`
    *   `appsettings.{Environment}.json`
    *   Environment Variables
    *   **AWS Systems Manager Parameter Store** (loads path `/Momentum`, refreshed every 5 minutes)
2.  **Main Settings Key:**
    *   `ConnectionString`: Connection string for the PostgreSQL database.
3.  **Authentication:**
    *   AWS services leverage `DefaultAzureCredential` or AWS CLI environment configurations for Systems Manager authorization.

---

## 5. Build, Run, and Maintenance Workflows

All commands should be executed from the solution root:

### Building the Entire Solution
```powershell
dotnet build --configuration Debug
```

### Database Management (Migrations)
EF Core migrations are managed within the `Models` project. For running/design-time migrations, target the `ModelGeneration` helper project:
```powershell
# Add a migration
dotnet ef migrations add <MigrationName> --project Models --startup-project ModelGeneration

# Apply migrations to database
dotnet ef database update --project Models --startup-project ModelGeneration
```

### Running System Components
*   **Run Dashboard (Web UI):**
    ```powershell
    dotnet run --project Presentation/Presentation.csproj
    ```
*   **Run Securities Component Maintainer:**
    ```powershell
    dotnet run --project SecuritiesMaintain/SecuritiesMaintain.csproj
    ```
*   **Run Price History Maintainer:**
    ```powershell
    dotnet run --project SecurityPriceMaintain/SecurityPriceMaintain.csproj
    ```
*   **Run Momentum Calculations Engine:**
    ```powershell
    dotnet run --project ComputeMomentum/ComputeMomentum.csproj
    ```
*   **Run Slope Summaries Generation:**
    ```powershell
    dotnet run --project ComputeSlopeSummary/ComputeSlopeSummary.csproj
    ```

---

## 6. Coding Conventions & Standards

When contributing to this repository, adhere strictly to these conventions:

*   **Database Access:**
    *   Prefer `IDbContextFactory<AppDbContext>` to avoid DbContext concurrency issues, particularly in Blazor Server and asynchronous task queues.
    *   Make use of `EFCore.BulkExtensions` (e.g., `BulkInsertAsync`) when processing large price datasets to maximize efficiency.
*   **Logging:**
    *   Utilize structured logging via Serilog. Inject `ILogger<T>` where available. Avoid manual `Console.WriteLine` in services; keep console logs inside top-level execution files (`Program.cs`).
*   **Error Handling:**
    *   Implement try-catch blocks with proper diagnostics.
    *   In network pricing queries, handle rate limits and missing ticker values gracefully (e.g., using fallback dates and logging skipped components).
*   **Asynchronous Patterns:**
    *   Always use `Async` suffixes for asynchronous methods.
    *   Avoid blocking calls (`.Result`, `.Wait()`); utilize `await` natively.
*   **Date & Time Standards:**
    *   Always use universal time (`.ToUniversalTime()`) or specific market calendars when persisting dates to database tables to prevent time zone misalignment.
    *   Utilize `TradingCalendar.GetTradingDay(date)` to validate and align transaction dates to the NYSE market schedule.
