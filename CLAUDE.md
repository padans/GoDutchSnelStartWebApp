# GoDutchSnelStartWebApp — Projectdocumentatie

## Wat doet deze applicatie?

Een ASP.NET Core 10 web-API die banktransacties uit **GoDutch** (betaalplatform) automatisch importeert in **SnelStart** (boekhoudpakket). Daarnaast ondersteunt de app **MyPos**-transacties en biedt het een multi-tenant architectuur.

## Solution-structuur

| Project | Laag | Rol |
|---|---|---|
| `GoDutchSnelStartWebApp.Domain` | Domain | Entiteiten, enums, value objects, geen externe dependencies |
| `GoDutchSnelStartWebApp.Application` | Application | Services, interfaces, DTO's, use cases, DI-registratie (`AddApplicationServices`) |
| `GoDutchSnelStartWebApp.Infrastructure` | Infrastructure | Repositories (ADO.NET/stored procs), externe HTTP-clients, achtergrondworkers |
| `GoDutchSnelStartWebApp.Web` | Presentation | REST API controllers, middleware, Swagger |
| `GoDutchSnelStartWebApp.Portal` | Presentation | Blazor-portal (heeft ProjectReference naar Application) |
| `GoDutchSnelStartWebApp.Tests` | Tests | xUnit-tests, Moq |

## Afhankelijkheidsrichting (Clean Architecture)

```
Web → Application → Domain
          ↑
   Infrastructure
```

- DI: `AddApplicationServices()` (Application-laag) registreert alle Application-services; `AddInfrastructureServices()` (Infrastructure-laag) registreert alleen repositories, HTTP-clients, generators, encryptie en achtergrondworkers.
- Infrastructure verwijst zowel naar Application als (transitief overbodig) direct naar Domain.

## Technologie

- .NET 10 / ASP.NET Core 10
- ADO.NET met stored procedures (geen ORM)
- SQL Server via `ISqlConnectionFactory` / `SqlConnectionFactory`
- Serilog (structured logging)
- DPAPI voor wachtwoordencryptie (`DpapiSecretEncryptionService`) — Windows-only
- xUnit + Moq voor tests

## Domeinentiteiten

- `Tenant` — klant/organisatie
- `BankAccount` — bankrekening per tenant
- `BankAccountSetting` — SnelStart-instellingen per bankrekening
- `TenantGoDutchConnection` — GoDutch API-koppeling per tenant
- `TenantSnelStartConnection` — SnelStart API-koppeling per tenant
- `SnelStartAdministration` — SnelStart-administratie per tenant
- `BankAccountSnelStartLink` — koppeling tussen bankrekening en SnelStart-administratie (incl. auto-sync planning)
- `GoDutchImportRun` — logboek per sync-run (private ctor + `Start()` / `Reconstitute()` / `MarkSucceeded/Skipped/Failed()`)
- `MyPosRawTransaction` / `TenantMyPosConnection` / `MyPosTransactionTypeMapping` / `MyPosExportBatch` — MyPos-module

### Enums (Domain/Enums)

`TenantStatus`, `ImportRunStatus`, `ImportRunTriggerSource`, `SnelStartConnectionType`, `SnelStartExportFormat`, `MyPosExportBatchStatus`, `MyPosExportTarget`, `AppModule`. Repositories parsen met `Enum.Parse(..., ignoreCase: true)`.

### Value Objects (Domain)

`SnelStartGrootboekRef(Guid Id, string Nummer, string Naam)` en `SnelStartDagboekRef(Guid Id, string Code, string Naam)` — toegepast op `BankAccount`, `TenantMyPosConnection`, `MyPosExportBatch`, `MyPosTransactionTypeMapping`, `MyPosExportBatchLine`.

## SnelStart-authenticatie

Elke SnelStart B2B-call gebruikt twee sleutels:

- **Maatwerksleutel** (client key) — per tenant, versleuteld opgeslagen (`TenantSnelStartConnection.ClientKeyEncrypted`, of `BankAccountSetting.SnelStartClientKey`). Wordt ingewisseld voor een bearer-token.
- **Subscription key** (`Ocp-Apim-Subscription-Key`) — **app-breed**, één waarde voor alle tenants. **Enige bron: configuratie** `SnelStartGlobal:SubscriptionKey` (in `appsettings.Production.json` op de server, niet in source control). Wordt nooit per tenant of per bankrekening opgeslagen.

Alle SnelStart-verbruikers (`ConnectionTestService`, `SnelStartLookupService`, `SnelStartBankStatementImporter`, `SnelStartLatestBookingDateService`, `MyPosExportBatchExportService`) lezen de subscription key via `SnelStartGlobalOptions.RequireSubscriptionKey()`. De DB-kolommen `TenantSnelStartConnection.SubscriptionKeyEncrypted` en `BankAccountSetting.SnelStartSubscriptionKeyEncrypted` zijn dood (leeggemaakt in migratie `20260831_SnelStart_SubscriptionKey_ConfigOnly.sql`) en wachten op een latere drop-migratie; de DTO-velden `SubscriptionKey` / `SnelStartSubscriptionKey` worden server-side genegeerd.

---

## Clean Architecture-analyse (2026-06-25) — afgehandeld

Een eerdere analyse legde 7 laagschendingen/anti-patterns (FOUT 1–7) en 4 kwaliteitspunten (VERBETERING 8–11) vast. **Alles is geïmplementeerd in commit `b5a4c0d` "Clean Architecture verbeteringen: FOUT 1-7, VERBETERING 8-11" (2026-06-28)**, aangevuld in `5cf216a`. Zie de commit-message voor de details per punt.

Nog openstaande, bewust uitgestelde punten:

- **VERBETERING 9** — Infrastructure verwijst nog direct naar Domain (transitief al via Application). Acceptabel; niet gewijzigd.
- **VERBETERING 10** — NL/EN door elkaar: code Engels, functionele log-berichten Nederlands. Geen volledige opschoning gedaan.
- **VERBETERING 11** — Geen IBAN-validatie als value object in Domain.
