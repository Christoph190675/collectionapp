# Implementation Plan: Statut de santé (walking skeleton)

**Branch**: `feat/#2` | **Date**: 2026-10-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-health-status/spec.md`

## Summary

Un onglet « Statut » de l'application Ionic appelle `GET /api/health`. L'API renvoie
`{ api, database, checkedAt }`, avec le code 200 si la base est OK et 503 sinon. La base est OK si
elle répond en moins de 2 s et qu'aucune migration n'est en attente. Le front interprète la réponse
(503 avec corps = API OK), gère le délai de 5 s et les erreurs réseau, et permet la relance. Côté
back, le flux est Controller → MediatR → `IDatabaseHealthChecker` (Infrastructure, EF Core). On y
ajoute un DbContext vide, la migration `InitialCreate` appliquée par commande explicite et un CORS
à origines exactes. Le provider Pomelo 9, incompatible avec EF Core 10, est remplacé par le
provider officiel `MySql.EntityFrameworkCore` 10.0.x ([research R1](./research.md)).

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) ; TypeScript 6 strict / Angular 22 / Ionic 9 / Capacitor 8

**Primary Dependencies**: ASP.NET Core 10, EF Core 10.0.x, `MySql.EntityFrameworkCore` 10.0.x,
MediatR 14, FluentValidation 12, Swashbuckle 10 ; front : `@angular/common/http`, RxJS 7.8

**Storage**: MySQL 8.4 dans un conteneur Docker géré par le développeur, hors du projet ; aucune table métier

**Testing**: xUnit 2.9 + `Microsoft.AspNetCore.Mvc.Testing` 10.0.x + `Testcontainers.MySql` 4.x +
`Microsoft.Extensions.TimeProvider.Testing` + `Microsoft.Extensions.Diagnostics.Testing` (FakeLogger) ; front : Vitest via `@angular/build:unit-test`

**Target Platform**: API Kestrel (Windows/Linux) ; front navigateur + Android (émulateur/téléphone) ;
iOS configuré mais non validable sur le poste Windows ([research R6](./research.md))

**Project Type**: mobile app + API REST

**Performance Goals**: affichage du statut < 2 s quand tout est OK (SC-001)

**Constraints**: vérification base ≤ 2 s côté API ; délai d'appel ≤ 5 s côté front ; état affiché
en moins de 6 s dans tous les cas de panne (SC-002)

**Scale/Scope**: 1 endpoint, 1 page, 1 service front ; usage développeur uniquement

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principe | Statut | Justification |
|---|----------|--------|---------------|
| I | Spec d'abord | ✅ | Spec validée et clarifiée (5 Q/R) |
| II | Critères testés | ✅ | Chaque scénario d'acceptation correspond à un test automatisé ([research R8](./research.md)) ; correspondance dans `tasks.md` |
| III | Contrat API explicite | ✅ | Contrat OpenAPI ([contracts/](./contracts/health.openapi.yaml)) ; `HealthStatusDto` dans Application ; contrôleur limité à MediatR + traduction 200/503 |
| IV | Clean Architecture | ✅ | Interface dans Application, implémentation EF dans Infrastructure, contrôleur dans `Api/Controllers` ; Api ne référence pas Domain ; rien ajouté à Domain |
| V | Migrations immuables | ✅ | `InitialCreate` versionnée ; jamais modifiée après merge |
| VI | Front sans logique métier | ✅ | Appel API centralisé dans `HealthService` ; la page ne fait qu'afficher. La traduction 200/503/erreur → état est une interprétation du transport, pas une règle métier |
| VII | Aucun secret versionné | ✅ | Chaîne de connexion dans user-secrets ; mot de passe Testcontainers généré |
| VIII | Simplicité | ✅ | Pas de repository (aucune entité), pas d'abstraction d'horloge maison (`TimeProvider` du BCL), pas de lib de health checks |

**Écart avec l'input utilisateur, résolu** : « migration appliquée au démarrage en Development » était
en conflit avec FR-014. L'utilisateur a confirmé la spec : migration par commande explicite.

**Re-check post-design** : ✅ aucun écart introduit par data-model, contrat ou quickstart.

## Project Structure

### Documentation (this feature)

```text
specs/001-health-status/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── health.openapi.yaml
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
backend/
├── CollectionApp.slnx                     # MOD  + tests/CollectionApp.Tests
├── .config/dotnet-tools.json              # NEW  dotnet-ef 10.0.x
├── src/
│   ├── CollectionApp.Api/
│   │   ├── Controllers/
│   │   │   └── HealthController.cs        # NEW  GET /api/health → 200 | 503
│   │   ├── Startup.cs                     # MOD  AddInfrastructure, CORS exact, HTTPS redirect hors Dev, TimeProvider
│   │   ├── appsettings.Development.json   # MOD  Kestrel http 0.0.0.0:8080 + https 44381 ; 3 origines CORS
│   │   └── CollectionApp.Api.csproj       # MOD  + Microsoft.EntityFrameworkCore.Design
│   ├── CollectionApp.Application/
│   │   └── Health/
│   │       ├── IDatabaseHealthChecker.cs  # NEW
│   │       ├── HealthStatusDto.cs         # NEW
│   │       └── GetHealthStatusQuery.cs    # NEW  query + handler
│   └── CollectionApp.Infrastructure/
│       ├── DependencyInjection.cs         # NEW  AddInfrastructure(IConfiguration)
│       ├── Persistence/
│       │   ├── CollectionAppDbContext.cs  # NEW  aucun DbSet
│       │   ├── DatabaseHealthChecker.cs   # NEW  CanConnect + pending migrations, timeout 2 s
│       │   └── Migrations/                # NEW  InitialCreate (généré)
│       └── CollectionApp.Infrastructure.csproj  # MOD  Pomelo → MySql.EntityFrameworkCore 10.0.x
└── tests/CollectionApp.Tests/
    ├── CollectionApp.Tests.csproj         # MOD  + Mvc.Testing, Testcontainers.MySql, TimeProvider.Testing, ref Api
    ├── UnitTest1.cs                       # DEL  test vide du template
    ├── Infrastructure/
    │   ├── MySqlContainerFixture.cs       # NEW
    │   └── ApiFactory.cs                  # NEW  WebApplicationFactory<Program> + chaîne de connexion injectée
    ├── Health/
    │   ├── GetHealthStatusHandlerTests.cs # NEW  unitaires
    │   ├── HealthEndpointTests.cs         # NEW  intégration : ok / base KO / migration en attente / API ne migre pas
    │   └── MigrationTests.cs              # NEW  InitialCreate appliquée + idempotence
    └── Cors/
        └── CorsTests.cs                   # NEW  3 origines autorisées, origine inconnue, préfixe piégeux

frontend/
├── angular.json                           # MOD  configuration "android" (fileReplacements)
├── capacitor.config.ts                    # MOD  cleartext / mixed content (dev)
├── android/                               # NEW  npx cap add android
└── src/
    ├── main.ts                            # MOD  provideHttpClient()
    ├── environments/
    │   ├── environment.ts                 # MOD  apiUrl http://localhost:8080/
    │   ├── environment.android.ts         # NEW  apiUrl http://10.0.2.2:8080/
    │   ├── environment.prod.ts            # MOD  apiUrl (placeholder)
    │   └── environment.dev.ts             # DEL  inutilisé, port erroné
    └── app/
        ├── services/
        │   ├── health.service.ts          # NEW  HttpClient, timeout 5 s, mapping → HealthCheckResult
        │   └── health.service.spec.ts     # NEW
        ├── status/
        │   ├── status.page.ts|html|scss   # NEW  ionViewWillEnter → check ; bouton Relancer ; spinner
        │   └── status.page.spec.ts        # NEW
        └── tabs/
            ├── tabs.routes.ts             # MOD  + /tabs/status
            └── tabs.page.html             # MOD  + bouton d'onglet « Statut »
```

**Structure Decision**: structure existante conservée (`backend/` Clean Architecture en 4 projets +
`frontend/` Ionic). Les nouveaux dossiers suivent l'input : `Api/Controllers`,
`Infrastructure/Persistence`. Les onglets Tab1 à Tab3 du template restent en place, hors périmètre.

## Design Notes

- **Endpoint** : `[ApiController] [Route("api/health")] [AllowAnonymous]`, `GET` → `mediator.Send(new
  GetHealthStatusQuery())` → `dto.Database == "ok" ? Ok(dto) : StatusCode(503, dto)`.
  Documenté avec `[ProducesResponseType<HealthStatusDto>(200)]` et `(503)`.
- **Handler** : `api = "ok"`, `database = await checker.IsHealthyAsync(ct) ? "ok" : "ko"`,
  `checkedAt = timeProvider.GetUtcNow()`.
- **Checker** : voir [research R2](./research.md) ; journalise la cause en Warning, n'écrit jamais
  le message d'exception dans la réponse.
- **Front `HealthService.check()`** : `http.get<unknown>(apiUrl + 'api/health').pipe(timeout(5000))`.
  `catchError` sur `HttpErrorResponse` : un 503 avec corps valide donne `database ko` ; un statut 0
  donne `unreachable` ; `TimeoutError` donne `timeout` ; tout le reste donne `invalid-response`.
  Validation du corps par un type guard. Mapping complet dans [data-model](./data-model.md).
- **Page Statut** : vérification dans `ionViewWillEnter` (Ionic garde la page en cache, `ngOnInit` ne
  suffit pas pour FR-002), abonnement annulé dans `ionViewWillLeave`, bouton « Relancer »
  `[disabled]="loading()"`, signaux Angular pour l'état. Libellés français.
- **CORS** : voir [research R4](./research.md) ; corrige un défaut existant (préfixe accepté).

## Complexity Tracking

Aucune violation de la constitution à justifier.
