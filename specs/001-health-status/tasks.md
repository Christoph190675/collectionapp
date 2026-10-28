---

description: "Task list for feature 001-health-status (walking skeleton)"
---

# Tasks: Statut de santé (walking skeleton)

**Input**: Design documents from `/specs/001-health-status/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/health.openapi.yaml, quickstart.md

**Tests**: OBLIGATOIRES. Le principe II de la constitution et SC-006 exigent au moins un test
automatisé par critère d'acceptation. Les tests sont écrits avant l'implémentation de chaque story
et doivent d'abord échouer. Chaque nom de test back reprend l'identifiant du critère couvert
(ex. `US2_AC1_DatabaseStopped_Returns503WithDatabaseKo`) ; chaque `it(...)` front le cite dans son
libellé (ex. `'US2-AC2 : API injoignable → API KO, base inconnue'`).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 … US5)
- Chemins relatifs à la racine du dépôt (`backend/`, `frontend/`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: dépendances, outillage, base de données locale

- [X] T001 Dans `backend/src/CollectionApp.Infrastructure/CollectionApp.Infrastructure.csproj`, supprimer `Pomelo.EntityFrameworkCore.MySql` 9.0.0 (incompatible EF Core 10, cf. research R1) et ajouter `MySql.EntityFrameworkCore` **10.0.12** ; ajouter `Microsoft.Extensions.Configuration.Abstractions` 10.0.x si nécessaire à la compilation de `AddInfrastructure(IConfiguration)`. Ne pas ajouter de package en préversion 11.
- [X] T002 [P] Dans `backend/src/CollectionApp.Api/CollectionApp.Api.csproj`, ajouter `Microsoft.EntityFrameworkCore.Design` **10.0.14** avec `<PrivateAssets>all</PrivateAssets>` (requis par `dotnet ef`, projet de démarrage).
- [X] T003 [P] Créer le manifeste d'outils local `backend/.config/dotnet-tools.json` (`dotnet new tool-manifest` dans `backend/`) puis `dotnet tool install dotnet-ef --version 10.0.14`.
- [X] T004 [P] Dans `backend/tests/CollectionApp.Tests/CollectionApp.Tests.csproj` : ajouter `Microsoft.AspNetCore.Mvc.Testing` 10.0.14, `Testcontainers.MySql` 4.16.0, `Microsoft.Extensions.TimeProvider.Testing` 10.x, `Microsoft.Extensions.Diagnostics.Testing` 10.x, `Xunit.SkippableFact` 1.x (skip dynamique, T028), et une `ProjectReference` vers `..\..\src\CollectionApp.Api\CollectionApp.Api.csproj` ; supprimer `backend/tests/CollectionApp.Tests/UnitTest1.cs` ; ajouter le projet de tests à `backend/CollectionApp.slnx` dans un dossier `/tests/`.
- [X] T005 [P] ~~Créer `docker-compose.yml` et `.env.example`~~ : retiré à la demande de l'utilisateur, le conteneur MySQL est géré hors du projet.
- [X] T006 [P] Front : dans `frontend/src/main.ts`, ajouter `provideHttpClient()` (`@angular/common/http`) aux providers ; dans `frontend/src/environments/environment.ts`, ajouter `apiUrl: 'https://localhost:44381/'` ; dans `frontend/src/environments/environment.prod.ts`, ajouter `apiUrl: ''` (non défini pour ce squelette) ; supprimer `frontend/src/environments/environment.dev.ts` (inutilisé, port erroné).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: persistance, migration initiale, câblage de l'API et socle de tests, nécessaires à toutes les stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Créer `backend/src/CollectionApp.Infrastructure/Persistence/CollectionAppDbContext.cs` : `public class CollectionAppDbContext(DbContextOptions<CollectionAppDbContext> options) : DbContext(options)`, **aucun `DbSet`** (data-model : aucune table métier).
- [X] T008 Créer `backend/src/CollectionApp.Infrastructure/DependencyInjection.cs` : `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`. Il lit `configuration.GetConnectionString("CollectionApp")`, lève une `InvalidOperationException` explicite si la chaîne est absente, et appelle `services.AddDbContext<CollectionAppDbContext>(o => o.UseMySQL(connectionString))`. Il ne contient **aucun appel à `Migrate()`** (FR-014).
- [X] T009 Dans `backend/src/CollectionApp.Api/Startup.cs` : dans `InitializeServices`, appeler `services.AddInfrastructure(configuration)` et `services.AddSingleton(TimeProvider.System)` ; dans `Configure`, entourer `app.UseHttpsRedirection()` de `if (!env.IsDevelopment())` **à sa position actuelle** (ne pas changer l'ordre du pipeline, cf. research R6).
- [X] T010 [P] Dans `backend/src/CollectionApp.Api/appsettings.Development.json`, remplacer `Kestrel:EndPoints` par `Http` → `http://0.0.0.0:8080` et `Https` → `https://localhost:44381`.
- [X] T011 Configurer le secret local (dev, non versionné) : `dotnet user-secrets set "ConnectionStrings:CollectionApp" "Server=localhost;Port=3306;Database=collectionapp;User=root;Password=<MYSQL_ROOT_PASSWORD>;Connection Timeout=2" --project backend/src/CollectionApp.Api`. Puis générer la migration depuis `backend/` : `dotnet ef migrations add InitialCreate -p src/CollectionApp.Infrastructure -s src/CollectionApp.Api -o Persistence/Migrations`. Vérifier que `backend/src/CollectionApp.Infrastructure/Persistence/Migrations/` contient `*_InitialCreate.cs`, son `.Designer.cs` et `CollectionAppDbContextModelSnapshot.cs`, et que `Up`/`Down` sont vides (pas d'entité).
- [X] T012 [P] Créer `backend/tests/CollectionApp.Tests/Infrastructure/MySqlContainerFixture.cs` : `IAsyncLifetime` qui démarre **un seul** `MySqlContainer` (`mysql:8.4`, mot de passe généré aléatoirement, utilisateur root) pour toute la collection xUnit `"MySql"` (`[CollectionDefinition("MySql")]` + `ICollectionFixture<MySqlContainerFixture>`). Méthode `Task<string> CreateDatabaseAsync(bool migrate)` : crée une base au nom unique (`test_{Guid:N}`), renvoie sa chaîne de connexion (avec `Connection Timeout=2`) et, si `migrate` vaut true, applique les migrations via un `CollectionAppDbContext` construit **dans le test** (`Database.MigrateAsync()`). Constante `UnreachableConnectionString` : `Server=127.0.0.1;Port=1;Database=x;User=x;Password=x;Connection Timeout=2`.
- [X] T013 [P] Créer `backend/tests/CollectionApp.Tests/Infrastructure/ApiFactory.cs` : `WebApplicationFactory<Program>` paramétrée par une chaîne de connexion et une liste d'origines CORS optionnelle ; `UseEnvironment("Testing")`, pour ne charger ni `appsettings.Development.json` ni les user-secrets ; `ConfigureAppConfiguration` ajoute une `AddInMemoryCollection` avec `ConnectionStrings:CollectionApp` et `Cors:AllowedOrigins:{i}`. Par défaut ce sont les 3 origines de dev (`http://localhost:8100`, `https://localhost`, `capacitor://localhost`), remplaçables par paramètre. Il n'y a pas de fusion par index avec un fichier : ne pas créer d'`appsettings.Testing.json`. En `Testing`, `UseHttpsRedirection` est actif, mais TestServer n'expose aucun port HTTPS, donc aucune redirection n'a lieu ; expose un `FakeLogCollector` (Microsoft.Extensions.Diagnostics.Testing) via `ConfigureLogging(l => l.AddFakeLogging())`.

**Checkpoint**: `dotnet build backend/CollectionApp.slnx` passe, la migration existe, l'API démarre.

---

## Phase 3: User Story 1 - Vérifier que toute la chaîne fonctionne (Priority: P1) 🎯 MVP

**Goal**: l'onglet « Statut » affiche API = OK et Base de données = OK quand tout fonctionne.

**Independent Test**: base Docker démarrée et migrée, API et front lancés, ouvrir `/tabs/status` → API = OK, Base de données = OK (quickstart scénario 2).

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T014 [P] [US1] Test unitaire `backend/tests/CollectionApp.Tests/Health/GetHealthStatusHandlerTests.cs` : avec un faux `IDatabaseHealthChecker` qui renvoie `true` et un `FakeTimeProvider` fixé à `2026-10-28T09:15:42Z`, le handler renvoie `Api == "ok"`, `Database == "ok"`, `CheckedAt == 2026-10-28T09:15:42Z` avec `DateTimeKind.Utc`. Ajouter le cas `false` → `Database == "ko"`, `Api == "ok"`.
- [X] T015 [P] [US1] Test d'intégration `backend/tests/CollectionApp.Tests/Health/HealthEndpointTests.cs` (collection `"MySql"`) `US1_AC1_AllHealthy_Returns200WithOk` : base créée avec `migrate: true`, `GET /api/health` → **200**, `Content-Type` JSON, corps avec **exactement** les propriétés `api`, `database`, `checkedAt` (contrat : `additionalProperties: false`), `api == "ok"`, `database == "ok"`, `checkedAt` en ISO 8601 se terminant par `Z`.
- [X] T016 [P] [US1] Test `frontend/src/app/services/health.service.spec.ts` (`HttpTestingController`) `'US1-AC1 : 200 valide → API ok, base ok'` : `check()` appelle `GET ${environment.apiUrl}api/health` ; une réponse 200 `{api:'ok',database:'ok',checkedAt:'2026-10-28T09:15:42.123Z'}` donne `{ api:'ok', database:'ok', checkedAt: new Date('2026-10-28T09:15:42.123Z'), error: undefined }`.
- [X] T017 [P] [US1] Test `frontend/src/app/status/status.page.spec.ts` `'US1-AC1 : affiche API = OK et Base de données = OK'` : avec un `HealthService` simulé renvoyant le résultat OK, `ionViewWillEnter()` déclenche `check()` une fois ; le DOM contient les libellés « API » et « Base de données », chacun avec « OK », ainsi que l'heure de dernière vérification (FR-010).
- [X] T018 [P] [US1] Test `frontend/src/app/tabs/tabs.page.spec.ts` `'US1-AC2 : onglet Statut présent'` : le DOM contient un `ion-tab-button` avec `tab="status"`, `href="/tabs/status"` et le libellé « Statut ».

### Implementation for User Story 1

- [X] T019 [P] [US1] Créer `backend/src/CollectionApp.Application/Health/IDatabaseHealthChecker.cs` : `Task<bool> IsHealthyAsync(CancellationToken cancellationToken)`. Il renvoie `true` si et seulement si la connexion aboutit ET qu'aucune migration n'est en attente.
- [X] T020 [P] [US1] Créer `backend/src/CollectionApp.Application/Health/HealthStatusDto.cs` : `public sealed record HealthStatusDto(string Api, string Database, DateTime CheckedAt)` avec constantes `public const string Ok = "ok"; public const string Ko = "ko";`. `CheckedAt` est un `DateTime` en `DateTimeKind.Utc`, pour que System.Text.Json sérialise avec le suffixe `Z` exigé par le contrat (un `DateTimeOffset` produirait `+00:00`). Sérialisé en camelCase : `api`, `database`, `checkedAt`.
- [X] T021 [US1] Créer `backend/src/CollectionApp.Application/Health/GetHealthStatusQuery.cs` : `public sealed record GetHealthStatusQuery : IRequest<HealthStatusDto>` et `GetHealthStatusQueryHandler(IDatabaseHealthChecker checker, TimeProvider timeProvider)`, qui renvoie `new HealthStatusDto(HealthStatusDto.Ok, healthy ? Ok : Ko, timeProvider.GetUtcNow().UtcDateTime)`. Rend T014 vert.
- [X] T022 [US1] Créer `backend/src/CollectionApp.Infrastructure/Persistence/DatabaseHealthChecker.cs` (implémente `IDatabaseHealthChecker`, dépend de `CollectionAppDbContext` et `ILogger<DatabaseHealthChecker>`) selon research R2 : `CancellationTokenSource` lié au token appelant avec `CancelAfter(2 s)`, puis `CanConnectAsync(ct)` ; si false, Warning et `false`. Sinon `GetPendingMigrationsAsync(ct)` ; si non vide, Warning listant les migrations en attente, puis `false`. L'ensemble est enveloppé dans `.WaitAsync(TimeSpan.FromSeconds(2))`. Toute exception (y compris `TimeoutException`, et `OperationCanceledException` due au délai interne) donne un Warning avec l'exception, puis `false`. Une `OperationCanceledException` due à l'annulation de l'appelant (`cancellationToken.IsCancellationRequested`) est relancée telle quelle. Ne jamais propager le message d'erreur. L'enregistrer en `Scoped` dans `AddInfrastructure` (`backend/src/CollectionApp.Infrastructure/DependencyInjection.cs`).
- [X] T023 [US1] Créer `backend/src/CollectionApp.Api/Controllers/HealthController.cs` : `[ApiController] [Route("api/health")] [AllowAnonymous]`, constructeur `(IMediator mediator)`, `[HttpGet]` `[ProducesResponseType<HealthStatusDto>(StatusCodes.Status200OK)]` `[ProducesResponseType<HealthStatusDto>(StatusCodes.Status503ServiceUnavailable)]` `public async Task<IActionResult> Get(CancellationToken ct)` → `var dto = await mediator.Send(new GetHealthStatusQuery(), ct); return dto.Database == HealthStatusDto.Ok ? Ok(dto) : StatusCode(StatusCodes.Status503ServiceUnavailable, dto);`. Aucune autre logique. Rend T015 vert.
- [X] T024 [US1] Créer `frontend/src/app/services/health.service.ts` : `@Injectable({ providedIn: 'root' })`, types exportés `HealthCheckResult { api: 'ok' | 'ko'; database: 'ok' | 'ko' | 'unknown'; checkedAt: Date; error?: 'unreachable' | 'timeout' | 'invalid-response' }` (cf. data-model) ; `check(): Observable<HealthCheckResult>` qui fait `GET ${environment.apiUrl}api/health` et transforme un 200 dont le corps passe un type guard `isHealthBody` (`api === 'ok'`, `database` ∈ {`ok`,`ko`}, `checkedAt` string de date valide) en résultat. Rend T016 vert.
- [X] T025 [US1] Créer `frontend/src/app/status/status.page.ts|html|scss` (standalone, imports Ionic depuis `@ionic/angular` comme `tab1.page.ts`) : signal `result = signal<HealthCheckResult | null>(null)` ; `ionViewWillEnter()` appelle `HealthService.check()` et met à jour `result` ; le template (`ion-header` « Statut », `ion-list` avec deux `ion-item`) affiche « API » / « OK|KO » et « Base de données » / « OK|KO|Inconnu » avec `ion-badge` (`success`/`danger`/`medium`), ainsi que « Dernière vérification : » suivi de `checkedAt | date:'HH:mm:ss'`. Aucune logique métier : affichage seulement. Rend T017 vert.
- [X] T026 [US1] Dans `frontend/src/app/tabs/tabs.routes.ts`, ajouter l'enfant `{ path: 'status', loadComponent: () => import('../status/status.page').then(m => m.StatusPage) }` ; dans `frontend/src/app/tabs/tabs.page.html`, ajouter `<ion-tab-button tab="status" href="/tabs/status">` avec `<ion-icon aria-hidden="true" name="pulse">` et `<ion-label>Statut</ion-label>` ; enregistrer l'icône `pulse` dans `frontend/src/app/tabs/tabs.page.ts` (`addIcons`, comme les icônes existantes). Rend T018 vert.

**Checkpoint**: US1 fonctionnelle de bout en bout (quickstart scénario 2).

---

## Phase 4: User Story 2 - Diagnostiquer une panne (Priority: P2)

**Goal**: base arrêtée → API = OK / Base = KO ; API injoignable, lente ou invalide → API = KO explicite, sans plantage.

**Independent Test**: quickstart scénarios 4 et 6.

### Tests for User Story 2 ⚠️

- [X] T027 [P] [US2] Dans `backend/tests/CollectionApp.Tests/Health/HealthEndpointTests.cs`, ajouter `US2_AC1_DatabaseStopped_Returns503WithDatabaseKo` : `ApiFactory` avec `MySqlContainerFixture.UnreachableConnectionString`, `GET /api/health` → **503**, corps avec exactement `api == "ok"`, `database == "ko"`, `checkedAt` ; la réponse ne contient ni « Exception », ni « 127.0.0.1 », ni « Port » (FR-005b) ; le `FakeLogCollector` contient au moins un log de niveau Warning émis par `DatabaseHealthChecker` (FR-005b).
- [X] T028 [P] [US2] Dans le même fichier, ajouter `Edge_SlowDatabase_RespondsKoWithin3Seconds` : chaîne de connexion vers une adresse non routable `Server=10.255.255.1;Port=3306;...;Connection Timeout=30` (la connexion reste suspendue), `GET /api/health` → 503 `database == "ko"` en **moins de 3 s** (chronomètre), ce qui prouve le délai de 2 s imposé par `WaitAsync`. Vérifier en plus que la durée est **d'au moins 1,5 s** ; si la connexion échoue instantanément sur ce réseau, marquer le test `[SkippableFact]` et appeler `Skip.If(elapsed < 1.5 s, …)` avec le message « adresse non routable rejetée immédiatement : le délai n'a pas pu être exercé ».
- [X] T029 [P] [US2] Dans `frontend/src/app/services/health.service.spec.ts`, ajouter : `'US2-AC1 : 503 avec corps valide → API ok, base ko'` ; `'US2-AC2 : erreur réseau (status 0) → API ko, base unknown, error unreachable'` ; `'Edge : pas de réponse en 5 s → error timeout'` (fake timers Vitest, avancer de 5 000 ms) ; `'Edge : 500 → error invalid-response'` ; `'Edge : 200 avec corps invalide → error invalid-response'` ; `'Edge : 503 sans corps conforme → error invalid-response'`. Pour tous les cas d'erreur, `checkedAt` est l'heure locale de la tentative et l'observable **émet** un résultat au lieu d'échouer.
- [X] T030 [P] [US2] Dans `frontend/src/app/status/status.page.spec.ts`, ajouter : `'US2-AC1 : base KO affichée'` (API OK, Base de données KO) ; `'US2-AC2 : API injoignable affichée sans plantage'` (API KO, Base de données « Inconnu », message « API injoignable », aucune exception levée) ; `'Edge : messages timeout et réponse invalide'` (« Délai dépassé (5 s) » et « Réponse invalide de l'API »).

### Implementation for User Story 2

- [X] T031 [US2] Dans `frontend/src/app/services/health.service.ts`, compléter `check()` : `.pipe(timeout(5000), map(...), catchError(...))` selon le tableau de data-model. Un `HttpErrorResponse` de statut 503 dont `error` passe `isHealthBody` donne un résultat API ok avec la base lue dans le corps. Le statut 0 donne `unreachable`, `TimeoutError` (rxjs) donne `timeout`, tout le reste (y compris un 200 au corps invalide) donne `invalid-response`. Dans ces trois cas, le résultat est `api 'ko'`, `database 'unknown'`, `checkedAt new Date()`. Rend T029 vert.
- [X] T032 [US2] Dans `frontend/src/app/status/status.page.html|ts`, afficher le message d'erreur sous la ligne API quand `result()?.error` est défini : `unreachable` → « API injoignable », `timeout` → « Délai dépassé (5 s) », `invalid-response` → « Réponse invalide de l'API » ; base `unknown` → badge « Inconnu ». Rend T030 vert.

**Checkpoint**: US1 et US2 fonctionnent indépendamment ; T027/T028 passent déjà grâce à T022/T023.

---

## Phase 5: User Story 3 - Relancer la vérification (Priority: P2)

**Goal**: relance manuelle, indicateur de chargement, pas de vérification concurrente, sortie d'onglet sans erreur.

**Independent Test**: quickstart scénarios 5 et 7.

### Tests for User Story 3 ⚠️

- [X] T033 [P] [US3] Dans `frontend/src/app/status/status.page.spec.ts`, ajouter : `'US3-AC1 : la relance met à jour l'état KO → OK'` (premier `check()` base ko, clic sur le bouton « Relancer » → second `check()` base ok, DOM mis à jour) ; `'US3-AC2 : indicateur et bouton désactivé pendant la vérification'` (avec un `Subject` non résolu : `ion-spinner` présent, bouton `disabled`, un second clic n'appelle pas `check()` une seconde fois) ; `'Edge : sortie d'onglet pendant la vérification'` (`ionViewWillLeave()` puis émission tardive : aucune erreur, `result` inchangé, abonnement fermé).

### Implementation for User Story 3

- [X] T034 [US3] Dans `frontend/src/app/status/status.page.ts|html` : signal `loading` ; méthode `refresh()` qui ne fait rien si `loading()` est vrai, sinon passe `loading` à true, s'abonne à `check()` (subscription conservée) et repasse `loading` à false à l'émission (`finalize`) ; `ionViewWillEnter()` appelle `refresh()` ; `ionViewWillLeave()` désabonne et remet `loading` à false ; template : `ion-button` « Relancer » `[disabled]="loading()"` `(click)="refresh()"`, `ion-spinner` affiché si `loading()`. Rend T033 vert.

**Checkpoint**: US1 à US3 complètes côté parcours développeur.

---

## Phase 6: User Story 4 - Restreindre l'accès à l'API aux origines du front (Priority: P3)

**Goal**: seules les origines configurées (navigateur, Android, iOS) sont autorisées ; comparaison exacte.

**Independent Test**: quickstart scénarios 8 et 9.

### Tests for User Story 4 ⚠️

- [X] T035 [P] [US4] Créer `backend/tests/CollectionApp.Tests/Cors/CorsTests.cs` (collection `"MySql"`, base migrée) avec un `ApiFactory` configurant les origines `http://localhost:8100`, `https://localhost`, `capacitor://localhost` : `US4_AC1_ConfiguredOrigin_IsAllowed` (theory sur les 3 origines, `GET /api/health` avec en-tête `Origin` → `Access-Control-Allow-Origin` égal à l'origine ; preflight `OPTIONS` avec `Access-Control-Request-Method: GET` → même en-tête) ; `US4_AC2_UnknownOrigin_IsRejected` (theory : `http://evil.example`, `http://localhost:8100.evil.com`, `http://localhost:81000`, `https://localhost:4443` → aucun en-tête `Access-Control-Allow-Origin`) ; `FR012_OriginsComeFromConfiguration` (factory configurée avec la seule origine `http://custom.test` → autorisée ; `http://localhost:8100`, `https://localhost` et `capacitor://localhost` → toutes refusées).

### Implementation for User Story 4

- [X] T036 [US4] Dans `backend/src/CollectionApp.Api/Startup.cs` : remplacer `services.AddCors()` par une politique nommée `"Frontend"` construite dans `ConfigureServices`. Elle lit `Cors:AllowedOrigins` et utilise `SetIsOriginAllowed(origin => allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))` (égalité **exacte**, plus de `StartsWith`), avec `AllowAnyHeader().AllowAnyMethod().AllowCredentials()` et les `WithExposedHeaders` existants. Dans `Configure`, supprimer l'ancien bloc `UseCors(builder => ...)` et appeler `app.UseCors("Frontend")` **après** `app.UseRouting()` et avant `UseAuthentication`. Rend T035 vert.
- [X] T037 [P] [US4] Dans `backend/src/CollectionApp.Api/appsettings.Development.json`, `Cors:AllowedOrigins` = `["http://localhost:8100", "https://localhost", "capacitor://localhost"]` ; dans `backend/src/CollectionApp.Api/appsettings.json`, ajouter `"Cors": { "AllowedOrigins": [] }` (aucune origine par défaut).

**Checkpoint**: CORS conforme à FR-011/FR-012.

---

## Phase 7: User Story 5 - Initialiser la base par une première migration (Priority: P3)

**Goal**: `InitialCreate` initialise une base vierge par commande explicite, de façon idempotente ; l'API ne migre jamais.

**Independent Test**: quickstart scénarios 1 à 3.

### Tests for User Story 5 ⚠️

- [X] T038 [P] [US5] Dans `backend/tests/CollectionApp.Tests/Health/HealthEndpointTests.cs`, ajouter `US5_AC1_BlankDatabase_Returns503WithDatabaseKo` (base créée avec `migrate: false`, `GET /api/health` → 503, `database == "ko"`, et un Warning mentionnant `InitialCreate` dans le `FakeLogCollector`) et `FR014_ApiNeverAppliesMigrations` (base vierge, démarrer l'API et appeler `/api/health` deux fois, puis via un `CollectionAppDbContext` du test : `GetPendingMigrationsAsync()` contient toujours la migration `InitialCreate` et `GetAppliedMigrationsAsync()` est vide).
- [X] T039 [P] [US5] Créer `backend/tests/CollectionApp.Tests/Health/MigrationTests.cs` (collection `"MySql"`) : `US5_AC2_MigrateBlankDatabase_AppliesInitialCreate_AndHealthIsOk` (base vierge, `Database.MigrateAsync()`, applied contient un identifiant se terminant par `_InitialCreate`, puis `GET /api/health` → 200 `database == "ok"`) ; `US5_AC3_MigrateTwice_IsIdempotent` (second `MigrateAsync()` sans exception, migrations appliquées inchangées, pending vide).

### Implementation for User Story 5

- [X] T040 [US5] Aucun code nouveau : la migration (T011) et la vérification des migrations en attente (T022) existent déjà. Vérifier que T038 et T039 passent, puis contrôler par `grep -r "Migrate(" backend/src` qu'aucun appel à `Migrate()` ou `MigrateAsync()` n'existe dans `backend/src` (FR-014).

**Checkpoint**: toutes les stories sont fonctionnelles et testées.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: plateforme Android (FR-015), documentation, validation finale

- [X] T041 [P] Créer `frontend/src/environments/environment.android.ts` (`production: false`, `apiUrl: 'http://10.0.2.2:8080/'`). Dans `frontend/angular.json`, ajouter sous `build.configurations` une configuration `android` qui reprend les options de `development` avec `fileReplacements` `environment.ts` → `environment.android.ts`.
- [X] T042 Dans `frontend/capacitor.config.ts`, calculer `const isDev = process.env['CAP_ENV'] === 'dev';`. `server.androidScheme: 'https'` reste inconditionnel ; `server.cleartext: true` et `android.allowMixedContent: true` ne sont ajoutés que si `isDev` (appel de l'API HTTP locale depuis l'origine `https://localhost`, cf. research R6). Sans `CAP_ENV=dev`, la config ne contient aucune option non sécurisée. Puis, dans `frontend/`, lancer `npm install @capacitor/android@8`, `npx ng build --configuration android`, `npx cap add android` et `$env:CAP_ENV='dev'; npx cap sync android`, et versionner `frontend/android/` (le `.gitignore` généré exclut les builds).
- [X] T043 [P] Vérifier dans Swagger (`https://localhost:44381/swagger`) que `GET /api/health` expose les réponses 200 et 503 avec le schéma `HealthStatusDto` et qu'il est cohérent avec `specs/001-health-status/contracts/health.openapi.yaml`. Corriger les attributs dans `backend/src/CollectionApp.Api/Controllers/HealthController.cs` si besoin.
- [X] T044 [P] Lancer `npm run lint` dans `frontend/` et `dotnet build backend/CollectionApp.slnx -warnaserror` (warnings du code ajouté seulement) ; corriger les erreurs dans les fichiers créés par cette feature.
- [X] T045 Exécuter `dotnet test backend/CollectionApp.slnx` (Docker requis) et `npm test -- --watch=false` dans `frontend/` : tout est vert. Contrôler la traçabilité : chaque critère US1-AC1/AC2, US2-AC1/AC2, US3-AC1/AC2, US4-AC1/AC2, US5-AC1/AC2/AC3 est cité par au moins un test. US1-AC1 et US3-AC1 sont couverts ensemble par les tests back (T015, T027) et front (T017, T033) ; aucun test de bout en bout navigateur n'est prévu pour ce squelette.
- [ ] T046 Dérouler les scénarios manuels de `specs/001-health-status/quickstart.md` (1 à 10 ; le scénario 10 sur émulateur Android) et noter les écarts. Vérifier avec `git status` qu'aucun `.env` ni secret n'est suivi.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: aucune dépendance ; T001 avant T007/T008 (packages EF).
- **Foundational (Phase 2)**: dépend de Phase 1. T007 → T008 → T009 → T011 (la migration exige DbContext + câblage + Design package T002 + outil T003). T012/T013 dépendent de T004 et T007.
- **US1 (Phase 3)**: dépend de Phase 2. C'est le MVP.
- **US2 (Phase 4)**: dépend de US1. Les tests back passent avec T022/T023 ; le front complète `health.service.ts` et `status.page` créés en US1.
- **US3 (Phase 5)**: dépend de US1 (page Statut) ; indépendante de US2.
- **US4 (Phase 6)**: dépend uniquement de Phase 2 + T023 (endpoint à appeler) ; parallélisable avec US2/US3.
- **US5 (Phase 7)**: dépend de Phase 2 + T022/T023 ; parallélisable avec US2/US3/US4.
- **Polish (Phase 8)**: après toutes les stories.

### Within Each User Story

- Tests écrits d'abord et en échec, puis implémentation.
- Back : interface/DTO → handler → checker → contrôleur.
- Front : service → page → routes/onglet.

### Parallel Opportunities

- Phase 1 : T002, T003, T004, T005, T006 en parallèle après T001 (fichiers distincts).
- Phase 2 : T010, T012, T013 en parallèle avec la chaîne T007 → T011.
- US1 : tests T014 à T018 en parallèle ; T019 et T020 en parallèle ; back (T021 à T023) et front (T024 à T026) en parallèle.
- Après US1 : US3 (front), US4 (back), US5 (back) en parallèle.

---

## Parallel Example: User Story 1

```bash
# Tests US1 (tous en échec au départ) :
Task: "T014 Handler unit tests in backend/tests/CollectionApp.Tests/Health/GetHealthStatusHandlerTests.cs"
Task: "T015 Integration test US1_AC1 in backend/tests/CollectionApp.Tests/Health/HealthEndpointTests.cs"
Task: "T016 HealthService spec in frontend/src/app/services/health.service.spec.ts"
Task: "T017 Status page spec in frontend/src/app/status/status.page.spec.ts"
Task: "T018 Tabs spec in frontend/src/app/tabs/tabs.page.spec.ts"

# Puis back et front en parallèle :
Task: "T019-T023 backend Application/Infrastructure/Api"
Task: "T024-T026 frontend service/page/tabs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (Setup) + Phase 2 (Foundational)
2. Phase 3 (US1)
3. **STOP and VALIDATE** : quickstart scénario 2, tests US1 verts

### Incremental Delivery

1. Setup + Foundational → API démarrable, migration prête
2. US1 → chaîne OK visible (MVP)
3. US2 → pannes diagnostiquées
4. US3 → relance
5. US4 → CORS durci
6. US5 → migration validée
7. Polish → Android, validation complète

---

## Notes

- [P] = fichiers différents, pas de dépendance sur une tâche non terminée
- Ne jamais modifier `*_InitialCreate.cs` une fois mergée (principe V)
- Aucun secret dans les fichiers versionnés (principe VII) ; Testcontainers génère ses propres identifiants
- Committer après chaque tâche ou groupe logique
- SC-001 (moins de 2 s) et FR-015 (Android) sont validés manuellement (T046). Pour SC-002, la borne de 6 s repose sur le timeout front de 5 s (testé en T029) et sur la limite de 2 s côté API (testée en T028).

## État d'implémentation (2026-10-28)

- Back : 18 tests verts (`dotnet test backend/CollectionApp.slnx`), dont le contrôle OpenAPI (T043, automatisé dans `HealthOpenApiTests`).
- Front : 21 tests verts (`npm test -- --watch=false`, Node 24.22.1) ; build `android` et `cap sync` OK.
- Traçabilité (T045) : les 11 critères d'acceptation sont cités par au moins un test.
- T046 (2026-10-28) : scénarios 1 à 9 validés en conditions réelles (Firefox, base MySQL du développeur sur le port 3307) ; scénario 10 (émulateur Android) **reporté** : aucun SDK Android sur le poste ; build `android` et `cap sync` validés, CORS des origines Android couvert par `CorsTests`.
- Corrections issues de T046 : `apiUrl` du navigateur en `http://localhost:8080/` (Firefox n'approuve pas le certificat de dev Windows) ; port `ng serve` fixé à 8100 dans `angular.json` (origine CORS autorisée).
