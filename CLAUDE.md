# GoDutchSnelStartWebApp — Projectdocumentatie

## Wat doet deze applicatie?

Een ASP.NET Core 10 web-API die banktransacties uit **GoDutch** (betaalplatform) automatisch importeert in **SnelStart** (boekhoudpakket). Daarnaast ondersteunt de app **MyPos**-transacties en biedt het een multi-tenant architectuur.

## Solution-structuur

| Project | Laag | Rol |
|---|---|---|
| `GoDutchSnelStartWebApp.Domain` | Domain | Entiteiten, geen externe dependencies |
| `GoDutchSnelStartWebApp.Application` | Application | Services, interfaces, DTO's, use cases |
| `GoDutchSnelStartWebApp.Infrastructure` | Infrastructure | Repositories (ADO.NET/stored procs), externe HTTP-clients, achtergrondworkers |
| `GoDutchSnelStartWebApp.Web` | Presentation | REST API controllers, middleware, Swagger |
| `GoDutchSnelStartWebApp.Portal` | Presentation | Blazor-portal (nog niet gekoppeld) |
| `GoDutchSnelStartWebApp.Tests` | Tests | xUnit-tests, Moq |

## Afhankelijkheidsrichting (Clean Architecture)

```
Web → Application → Domain
          ↑
   Infrastructure
```

Infrastructure hangt ook direct van Domain af (overbodig — via Application al transitief).

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
- `BankAccountSetting` — SnelStart-instellingen per bankrekening (legacy GoDutch-velden aanwezig maar deprecated)
- `TenantGoDutchConnection` — GoDutch API-koppeling per tenant
- `TenantSnelStartConnection` — SnelStart API-koppeling per tenant
- `SnelStartAdministration` — SnelStart-administratie per tenant
- `BankAccountSnelStartLink` — koppeling tussen bankrekening en SnelStart-administratie (incl. auto-sync planning)
- `GoDutchImportRun` — logboek per sync-run
- `MyPosRawTransaction` / `TenantMyPosConnection` / `MyPosTransactionTypeMapping` / `MyPosExportBatch` — MyPos-module

## Bekende technische schuld / te verbeteren punten

Zie de gedetailleerde analyse hieronder.

---

# Clean Architecture — Bevindingen en verbeteringen

## FOUT 1 — Controller injecteert repository direct (laagschending)

**Bestand:** `GoDutchSnelStartWebApp.Web/Controllers/BankAccountsController.cs`, regel 15–16 en 64–88

```csharp
private readonly IGoDutchImportRunRepository _importRunRepository;
```

De `BankAccountsController` injecteert `IGoDutchImportRunRepository` direct en roept deze aan voor het `GET /sync-status` endpoint. Controllers (Presentation) mogen **niet** direct met repository-interfaces werken. Die behoren tot de Application-laag, maar het orkestratieniveau hoort in een Application-service thuis.

**Oplossing:** Voeg een methode `GetSyncStatusAsync(Guid tenantId, Guid bankAccountId)` toe aan `IBankAccountService` of een nieuwe `IGoDutchImportRunService`, en verwijder de directe repository-injectie uit de controller.

---

## FOUT 2 — Dubbele DI-registratie van IConnectionTestService

**Bestanden:**
- `GoDutchSnelStartWebApp.Web/Program.cs`, regel 63
- `GoDutchSnelStartWebApp.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`, regel 107

`IConnectionTestService` wordt twee keer geregistreerd: eenmaal in `Program.cs` en nogmaals in `AddInfrastructureServices`. De tweede registratie overschrijft de eerste. Dit kan onverwacht gedrag geven en is verwarrend.

**Oplossing:** Verwijder de registratie uit `Program.cs` en laat de Infrastructure-methode de enige bron van waarheid zijn. Of andersom: alle Application-services in `Program.cs`, alleen Infrastructure-implementaties in `AddInfrastructureServices`.

---

## FOUT 3 — Application-services worden geregistreerd in Infrastructure DI

**Bestand:** `GoDutchSnelStartWebApp.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

Regels 96–108 registreren Application-laag services:
```csharp
services.AddScoped<ITenantGoDutchConnectionService, TenantGoDutchConnectionService>();
services.AddScoped<ISnelStartAdministrationService, SnelStartAdministrationService>();
services.AddScoped<IBankAccountSnelStartLinkService, BankAccountSnelStartLinkService>();
services.AddScoped<IBankAccountResyncService, BankAccountResyncService>();
services.AddScoped<IConnectionTestService, ConnectionTestService>();
services.AddScoped<IMyPosExportBatchService, MyPosExportBatchService>();
// ... etc
```

Dit is een laagschending: de Infrastructure-laag mag geen kennis hebben van Application-services. Infrastructure hoort alleen Infrastructure-implementaties te registreren (repositories, HTTP-clients, encryptie, achtergrondworkers).

**Oplossing:** Maak een aparte extensiemethode `AddApplicationServices()` in een `ApplicationServiceRegistration.cs` in het Application-project (of in de Web/Program.cs), en verplaats alle Application-service-registraties daarheen.

---

## FOUT 4 — Anemic Domain Model (geen encapsulatie)

**Bestanden:** Alle entiteiten in `GoDutchSnelStartWebApp.Domain/Entities/`

Alle domain-entiteiten hebben volledig publieke setters:
```csharp
public string Status { get; set; } = "Draft";
public bool IsActive { get; set; }
```

Er is geen bedrijfslogica, geen validatie en geen invariant-bescherming in het domein. Elke laag kan elk veld vrij overschrijven. Dit is een "Anemic Domain Model" — een bekende anti-pattern.

**Oplossing (stapsgewijs):**
- Stel setters in op `private set` of `init`
- Voeg factory-methoden of constructors toe: `Tenant.Create(name, customerCode, ...)` die invarianten afdwingen
- Voeg gedragsmethoden toe: `tenant.Activate()`, `tenant.Suspend()`, `importRun.MarkCompleted(...)` i.p.v. direct properties setten

---

## FOUT 5 — Magic strings i.p.v. enums/value objects

Meerdere plaatsen in de codebase gebruiken raw strings voor domeinstatus-waarden:

| Entiteit/klasse | Veld | Gebruikte strings |
|---|---|---|
| `Tenant` | `Status` | "Draft", "Trial", "Active", "Suspended", "Cancelled" |
| `GoDutchImportRun` | `Status` | "Started", "Skipped", "Succeeded", "Failed" |
| `GoDutchImportRun` | `TriggerSource` | "BackgroundWorker", "Manual", "Webhook" |
| `TenantSnelStartConnection` | `ConnectionType` | "CustomKey", "OAuth" |
| `BankAccountSnelStartLink` | `ExportFormat` | "MT940", "CAMT053" |
| `BankAccountSetting` | `ExportFormat` | "MT940", "CAMT053" |
| `MyPosExportBatch` | `Status` | "Concept" |
| `MyPosExportBatch` | `ExportTarget` | "SnelStartBankboek" |

**Oplossing:** Maak enums in de Domain-laag:
```csharp
public enum TenantStatus { Draft, Trial, Active, Suspended, Cancelled }
public enum ImportRunStatus { Started, Skipped, Succeeded, Failed }
public enum TriggerSource { BackgroundWorker, Manual, Webhook }
public enum SnelStartConnectionType { CustomKey, OAuth }
```
De bestaande `SnelStartExportFormat`-enum in Application is al correct — gebruik deze ook in de entiteiten.

---

## FOUT 6 — Legacy/dode velden in BankAccountSetting

**Bestand:** `GoDutchSnelStartWebApp.Domain/Entities/BankAccountSetting.cs`, regels 10–12

```csharp
public string? GoDutchApiBaseUrl { get; set; }
public string? GoDutchUsername { get; set; }
public string? GoDutchPasswordEncrypted { get; set; }
```

De `BankAccountSettingsService` stelt deze velden bewust op `null`:
```csharp
// GoDutch credentials are tenant-scoped via TenantGoDutchConnections.
GoDutchApiBaseUrl = null;
GoDutchUsername = null;
GoDutchPasswordEncrypted = null;
```

De velden zijn gedeprecieerd maar staan nog in de entiteit en (waarschijnlijk) in de database.

**Oplossing:** Verwijder de velden uit de entiteit én de bijbehorende stored procedures/kolommen nadat de migratie-cleanup is afgerond.

---

## FOUT 7 — Herhaalde SnelStart-referentievelden (denormalisatie in Domain)

SnelStart dagboek/grootboek-gegevens worden op meerdere plaatsen vlak in entiteiten opgeslagen:

- `BankAccount`: `SnelStartGrootboekId`, `SnelStartGrootboekNummer`, `SnelStartGrootboekNaam`, `SnelStartDagboekId`, etc.
- `TenantMyPosConnection`: `SnelStartBankDagboekId`, `SnelStartBankDagboekNummer`, `SnelStartBankDagboekNaam`, `SnelStartBankIban`
- `MyPosExportBatch`: dezelfde velden nogmaals

**Oplossing:** Maak Value Objects:
```csharp
public record SnelStartGrootboekRef(Guid Id, string Nummer, string Naam);
public record SnelStartDagboekRef(Guid Id, string Code, string Naam);
```
Dit vermindert duplicatie en maakt de intentie explicieter.

---

## VERBETERING 8 — Portal-project heeft geen project-referenties

**Bestand:** `GoDutchSnelStartWebApp.Portal/GoDutchSnelStartWebApp.Portal.csproj`

Het Portal-project (Blazor) heeft geen `<ProjectReference>` naar Application of Domain. Het kan daardoor geen gebruik maken van domeinlogica. Als dit een toekomstige UI is, moet de koppeling worden toegevoegd.

---

## VERBETERING 9 — Infrastructure verwijst direct naar Domain (overbodige afhankelijkheid)

**Bestand:** `GoDutchSnelStartWebApp.Infrastructure/GoDutchSnelStartWebApp.Infrastructure.csproj`

```xml
<ProjectReference Include="..\GoDutchSnelStartWebApp.Application\..." />
<ProjectReference Include="..\GoDutchSnelStartWebApp.Domain\..." />
```

Infrastructure verwijst al transitief naar Domain via Application. De directe referentie naar Domain is overbodig. In de praktijk is dit acceptabel, maar in strikte Clean Architecture is het beter om alleen via Application te verwijzen.

---

## VERBETERING 10 — Inconsistente taal in code (NL/EN gemengd)

Log-berichten in `GoDutchAutoSyncService`, `GoDutchSnelStartImportService` etc. zijn Nederlands. Variabele namen, interface-namen en code-comments zijn Engels. Kies één taal en pas dit consequent toe. Aanbeveling: **Engels voor code**, Nederlands alleen toegestaan in functionele log-berichten naar de business.

---

## VERBETERING 11 — Geen IBAN-validatie in Domain

`BankAccount.Iban` is een `string` zonder enige validatieregel. Een IBAN heeft een vastgesteld formaat (ISO 13616). Validatie hoort thuis in het Domain als een Value Object of in de Application-laag als invoervalidatie.

---

## Prioritering

| Prioriteit | Bevinding | Impact |
|---|---|---|
| 1 (hoog) | FOUT 2 — Dubbele DI-registratie | Bug-risico |
| 2 (hoog) | FOUT 1 — Controller gebruikt repository direct | Laagschending |
| 3 (hoog) | FOUT 3 — Application-services in Infrastructure DI | Laagschending, onderhoudbaarheid |
| 4 (midden) | FOUT 5 — Magic strings | Type-onveiligheid, refactoring |
| 5 (midden) | FOUT 6 — Legacy velden BankAccountSetting | Misleidend domeinmodel |
| 6 (midden) | FOUT 4 — Anemic Domain Model | Langetermijn-onderhoudbaarheid |
| 7 (laag) | FOUT 7 — Denormalisatie SnelStart-refs | Herhaling, value objects |
| 8 (laag) | VERBETERING 8–11 | Kwaliteit |
