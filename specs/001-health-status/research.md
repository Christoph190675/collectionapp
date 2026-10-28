# Research: Statut de santé (walking skeleton)

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-10-28

## R1. Provider EF Core MySQL compatible EF Core 10

- **Constat**: `CollectionApp.Infrastructure` référence `Pomelo.EntityFrameworkCore.MySql` 9.0.0, qui
  cible EF Core 9. Aucune version Pomelo 10.x n'est publiée sur NuGet (dernière : 9.0.0) ; le support
  EF Core 10 est toujours en attente côté Pomelo (issue #2007, PR #2019 non mergée). La page des
  providers EF Core de Microsoft ne liste Pomelo que pour EF Core 8 et 9. Pomelo 9 ne fonctionne pas
  avec EF Core 10.
- **Decision**: remplacer Pomelo par le provider officiel Oracle **`MySql.EntityFrameworkCore` 10.0.x**
  (dernière stable : 10.0.12), aligné sur **EF Core 10.0.x** (`Microsoft.EntityFrameworkCore` 10.0.14).
  Outils : `Microsoft.EntityFrameworkCore.Design` 10.0.x dans le projet de démarrage `CollectionApp.Api`
  (requis par `dotnet ef`), `dotnet-ef` 10.0.x en outil local.
- **Rationale**: provider maintenu par l'éditeur de MySQL, version majeure alignée sur EF Core 10,
  pas de dépendance à un fork tiers.
- **Alternatives considered**:
  - `Microting.EntityFrameworkCore.MySql` 10.0.13 (fork communautaire de Pomelo, API quasi identique) :
    rejeté, car il dépend de la maintenance d'un seul acteur et ses namespaces diffèrent de Pomelo.
    C'est l'option de repli si un manque fonctionnel du provider Oracle apparaît plus tard.
  - Attendre Pomelo 10 : rejeté, aucune date de sortie, et la sortie elle-même est incertaine.
  - Rester en EF Core 9 : rejeté, contraire à la stack de la constitution (.NET 10).
- **Note**: le SDK installé est 11.0.100-rc.3. Il compile `net10.0` sans problème ; les packages
  restent épinglés en 10.0.x (pas de préversion 11).

## R2. Vérification de la base : connexion + migrations en attente

- **Decision**: un service `DatabaseHealthChecker` (Infrastructure) exécute, avec un délai global de
  **2 s** : `Database.CanConnectAsync(ct)` puis `Database.GetPendingMigrationsAsync(ct)`. La base est
  « ok » si la connexion aboutit et qu'il n'y a aucune migration en attente, « ko » dans tous les autres
  cas (exception, délai dépassé, migration en attente). La cause est journalisée via `ILogger` (niveau
  Warning) et n'est jamais renvoyée au client (FR-005b).
- **Délai garanti**: un `CancellationTokenSource(TimeSpan.FromSeconds(2))` est passé aux appels, et
  l'ensemble est enveloppé dans `Task.WaitAsync(timeout)`. Le délai reste ainsi borné même si le driver
  ignore l'annulation pendant l'ouverture du socket. La chaîne de connexion porte aussi
  `Connection Timeout=2`.
- **Rationale**: FR-004 exige l'état réel à chaque appel et la clarification Q3 exige la prise en
  compte des migrations en attente. 2 s côté API reste sous le délai de 5 s du front, de sorte qu'une
  base lente donne « Base de données = KO » et non « API = KO ».
- **Alternatives considered**: `AddHealthChecks().AddDbContextCheck<>()` d'ASP.NET Core. Rejeté : son
  format de réponse est imposé, il ne vérifie pas les migrations en attente et il contournerait la règle
  contrôleur → MediatR de la constitution.

## R3. Placement dans la Clean Architecture

- **Decision**:
  - `Application/Health/` : `IDatabaseHealthChecker` (interface), `GetHealthStatusQuery` + handler
    MediatR, `HealthStatusDto`.
  - `Infrastructure/Persistence/` : `CollectionAppDbContext`, `DatabaseHealthChecker`, `Migrations/`.
    `Infrastructure/DependencyInjection.cs` : `AddInfrastructure(IConfiguration)`.
  - `Api/Controllers/HealthController.cs` : envoie la requête MediatR et traduit le DTO en 200/503.
- **Rationale**: respecte le principe IV. Le contrôleur ne fait que la traduction HTTP, il ne connaît
  ni le DbContext ni `Domain`. Aucune entité métier n'est créée, donc il n'y a ni interface de
  repository ni ajout dans `Domain` (principe VIII).
- **Horloge**: `TimeProvider.System` injecté (type du BCL, pas de nouvelle abstraction), ce qui rend
  `checkedAt` testable.

## R4. CORS : liste d'origines exactes

- **Constat**: `Startup.Configure` compare les origines avec `origin.StartsWith(x)`. Avec
  `http://localhost:8100` autorisée, `http://localhost:8100.evil.com` ou `http://localhost:81000` sont
  aussi acceptées, ce qui viole FR-011.
- **Decision**: comparaison **exacte, insensible à la casse** des origines lues dans
  `Cors:AllowedOrigins`. Valeurs Development :
  `http://localhost:8100` (ionic serve), `https://localhost` (WebView Android Capacitor, schéma par
  défaut), `capacitor://localhost` (WebView iOS Capacitor). La configuration est déplacée dans une
  politique nommée enregistrée dans `ConfigureServices`, appliquée après `UseRouting`.
- **Alternatives considered**: `WithOrigins(...)` natif. C'est équivalent ; on le retient s'il reste
  lisible, sinon `SetIsOriginAllowed` avec égalité stricte.

## R5. Codes HTTP et pipeline existant

- **Decision**: `HealthController` renvoie `Ok(dto)` (200) ou `StatusCode(503, dto)`. Le corps JSON est
  identique dans les deux cas (contrat : [contracts/health.openapi.yaml](./contracts/health.openapi.yaml)).
- **Vérifications pipeline**: `UseStatusCodePages` n'intervient que sur une réponse sans corps, donc le
  503 avec corps n'est pas réécrit. `HttpExceptionFilter` et `ExceptionMiddleware` ne sont pas
  concernés, car aucune exception ne sort du handler : le checker capture tout.
- **Endpoint anonyme**: pas d'`[Authorize]` ; `[AllowAnonymous]` explicite en prévision d'une future
  politique d'authentification globale.

## R6. HTTPS, ports et accès depuis l'émulateur ou un téléphone

- **Constat**: dans `appsettings.Development.json`, la section `Kestrel:EndPoints` (https 44381)
  l'emporte sur `UseUrls(8080/8081)` ; l'API n'écoute donc qu'en HTTPS sur `localhost`. Or le front
  `environment.dev.ts` pointe sur `https://localhost:44380/` (port erroné, fichier non branché dans
  `angular.json`). `UseHttpsRedirection` redirigerait les appels HTTP, et une redirection fait échouer
  un preflight CORS.
- **Decision**:
  - Kestrel Development : `Http` sur `http://0.0.0.0:8080` (joignable depuis l'émulateur via `10.0.2.2`
    et depuis le réseau local) et `Https` sur `https://localhost:44381`.
  - `UseHttpsRedirection` appliqué hors Development uniquement (le certificat de dev n'est pas reconnu
    par un téléphone).
  - Front : `apiUrl` dans `environment.ts` (`http://localhost:8080/` pour le navigateur : Firefox
    n'approuve pas le certificat de dev du magasin Windows), plus une
    configuration de build `android` (`environment.android.ts`, `http://10.0.2.2:8080/`) pour FR-015.
    `environment.dev.ts`, inutilisé, est supprimé.
  - Capacitor : `server.androidScheme: 'https'` (défaut) ; pour appeler une API HTTP depuis l'origine
    `https://localhost`, la WebView Android bloque le contenu mixte, d'où `android.allowMixedContent:
    true` et `server.cleartext: true`, **activés seulement si `CAP_ENV=dev` au moment du
    `cap sync`** (`capacitor.config.ts` est évalué à la synchronisation) ; sans cette variable, la
    configuration ne contient aucune option non sécurisée.
- **Limite**: le poste de développement est sous Windows. iOS ne peut pas y être compilé ni testé ;
  l'origine `capacitor://localhost` est configurée et couverte par le test CORS automatisé, mais la
  validation manuelle sur iOS est reportée. L'ajout de la plateforme Android (`npx cap add android`)
  fait partie de la feature.

## R7. Migrations

- **Decision**: migration `InitialCreate` générée dans `Infrastructure/Persistence/Migrations`. Le
  DbContext n'a aucune entité, donc la migration est vide et sert seulement à créer
  `__EFMigrationsHistory` et à établir la base de référence. Elle est appliquée **uniquement** par
  `dotnet ef database update` (FR-014, clarification Q4). L'API n'appelle jamais `Migrate()` ;
  l'input du plan qui demandait « appliquée au démarrage en Development » a été écarté au profit de la
  spec, avec l'accord de l'utilisateur.
- **Base de données**: conteneur MySQL géré par le développeur, hors du projet (pas de
  `docker-compose` versionné). Chaîne de connexion de l'API dans `dotnet user-secrets`
  (`ConnectionStrings:CollectionApp`, utilisateur root).

## R8. Tests

- **Back** : xUnit (`CollectionApp.Tests`, ajouté à la solution, référence `CollectionApp.Api`) avec
  `Microsoft.AspNetCore.Mvc.Testing` 10.0.x (`WebApplicationFactory<Program>`) et
  **`Testcontainers.MySql`** 4.x pour une vraie base MySQL éphémère. Une fixture de collection démarre
  le conteneur une seule fois par session de tests.
  - Base OK : conteneur migré (`Database.MigrateAsync()` appelé **par le test**, pas par l'API).
  - Migration en attente : base vierge du même conteneur, sans migration.
  - Base KO : chaîne de connexion vers un port fermé.
  - CORS : requêtes avec en-tête `Origin` (autorisées, refusées, préfixe piégeux).
  - API ne migre pas : après démarrage de l'API sur une base vierge, la migration reste en attente.
  - Unitaires : handler (ok/ko, horodatage via `FakeTimeProvider`).
- **Front** : Vitest via `@angular/build:unit-test` (déjà configuré) avec `HttpTestingController`
  pour `HealthService` (200, 503 avec corps, 503 sans corps, 500, erreur réseau, délai dépassé avec
  fake timers, corps invalide), et `TestBed` pour la page Statut (affichage, relance, indicateur de
  chargement, bouton désactivé, sortie de l'onglet).
- **Rationale**: le principe II impose un test par critère d'acceptation. Les scénarios « base
  arrêtée » et « migration en attente » demandent une vraie base, que Testcontainers fournit sans
  configuration manuelle.
- **Alternatives considered**: provider EF InMemory ou SQLite. Rejeté : ils ne testent ni la
  connectivité MySQL ni les migrations MySQL.
