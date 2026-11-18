---

description: "Task list for feature 002-user-auth (gestion des utilisateurs)"
---

# Tasks: Gestion des utilisateurs (inscription, connexion, déconnexion)

**Input**: Design documents from `/specs/002-user-auth/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/auth.openapi.yaml, quickstart.md

**Tests**: OBLIGATOIRES (principe II de la constitution : au moins un test automatisé par
critère d'acceptation). Les tests de chaque story sont écrits **avant** son implémentation et
doivent d'abord échouer. Chaque nom de test back reprend l'identifiant du critère couvert
(ex. `US2_AC4_FiveFailures_ReturnsLocked`) ; chaque `it(...)` front le cite dans son libellé
(ex. `'US1-AC4 : confirmation différente → aucune requête'`).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 … US4)
- Chemins relatifs à la racine du dépôt (`backend/`, `frontend/`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: dépendances et configuration (research R1, R2)

- [X] T001 Dans `backend/src/CollectionApp.Infrastructure/CollectionApp.Infrastructure.csproj`, ajouter `Microsoft.Extensions.Identity.Core` **10.0.14** et `Microsoft.IdentityModel.JsonWebTokens` **8.19.2** (même version que celle tirée par JwtBearer, R1). Aucun package en préversion.
- [X] T002 [P] Front : `cd frontend; npm install @capacitor/preferences@8.0.1` (compatible `@capacitor/core` 8.5.3), puis `npx cap sync`.
- [X] T003 [P] Dans `backend/src/CollectionApp.Api/appsettings.json`, ajouter la section `"Jwt": { "Issuer": "collection-app", "Audience": "collection-app", "AccessTokenMinutes": 15, "RefreshTokenDays": 7, "SigningKey": "" }` — `SigningKey` reste **vide** dans le fichier versionné (principe VII).
- [X] T004 Configurer la clé locale (non versionnée) : `dotnet user-secrets set "Jwt:SigningKey" "<48 octets aléatoires en base64>" --project backend/src/CollectionApp.Api` (commande PowerShell de génération dans `quickstart.md` §1).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: modèle de données complet + migration unique, socle d'authentification, corrections de la gestion d'erreurs, socle de tests et socle front. Nécessaire à toutes les stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Gestion des erreurs (écarts de l'existant, plan.md)

- [X] T005 Test d'abord : créer `backend/tests/CollectionApp.Tests/Errors/ErrorFormatTests.cs` — (a) `ExceptionStatusMapper.GetStatusCode(new FluentValidation.ValidationException(...))` renvoie `BadRequest` ; (b) le corps d'une `ValidationException` contient `error.fields` = liste `{ field, message }` avec `field` en camelCase ; (c) sans claim `LogRead`, `error.stackTrace` et `error.details` sont absents ; (d) avec `LogRead`, ils sont présents ; (e) une `LockedException` avec `RetryAfter = 14 min 30 s` produit 423 et l'en-tête `Retry-After: 870`.
- [X] T006 Dans `backend/src/CollectionApp.Domain/Exceptions/LockedException.cs`, ajouter une propriété optionnelle `TimeSpan? RetryAfter` (constructeur `LockedException(string message, TimeSpan? retryAfter = null)`). Dans `backend/src/CollectionApp.Api/Http/Exceptions/ExceptionStatusMapper.cs`, mapper `FluentValidation.ValidationException` → `HttpStatusCode.BadRequest`. Dans la couche `Http/Exceptions/` (seule partie de `Api` autorisée à référencer `Domain.Exceptions`, constitution v1.1.0 principe IV), ajouter l'en-tête `Retry-After` (secondes, arrondi au supérieur) quand l'exception est une `LockedException` dont `RetryAfter` est renseigné ; test correspondant dans `ErrorFormatTests.cs` (T005).
- [X] T007 Dans `backend/src/CollectionApp.Api/Http/Exceptions/HttpException.cs` : (a) ajouter à `HttpExceptionBodyError` une propriété `[JsonProperty(PropertyName = "fields", NullValueHandling = NullValueHandling.Ignore)] List<HttpExceptionField>? Fields` (classe `HttpExceptionField { field, message }`), remplie depuis `ValidationException.Errors` (`PropertyName` en camelCase, `ErrorMessage`) ; message global `Les données saisies sont invalides` ; (b) dans `ToJson()`, remplacer `StackTrace = FullStackTrace()` par une valeur renseignée **uniquement** si l'utilisateur a le claim `ClaimPrincipalValues.LogRead` (comme `GetDetails()`). T005 doit passer.

### Domain (data-model.md)

- [X] T008 [P] Créer `backend/src/CollectionApp.Domain/Users/User.cs` : `Id` (int), `LastName`, `FirstName`, `Email`, `PasswordHash` (string), navigations `ICollection<UserClaim> UserClaims`, `Login? Login`, `ICollection<RefreshToken> RefreshTokens` ; `static string NormalizeEmail(string email)` = `email.Trim().ToLowerInvariant()` ; `static User Create(lastName, firstName, email, passwordHash)` qui applique `Trim()` sur nom/prénom et `NormalizeEmail` sur l'e-mail. Aucun mot de passe en clair dans le Domain (FR-005).
- [X] T009 [P] Créer `backend/src/CollectionApp.Domain/Users/ClaimValue.cs` : `Id`, `Name`, `Description`, `ICollection<UserClaim> UserClaims` ; constantes `public const int UserId = 1; public const string UserName = "User"; public const int LogReadId = 2; public const string LogReadName = "LogRead";`.
- [X] T010 [P] Créer `backend/src/CollectionApp.Domain/Users/UserClaim.cs` : `UserId`, `ClaimId`, navigations `User User`, `ClaimValue Claim`.
- [X] T011 [P] Créer `backend/src/CollectionApp.Domain/Users/Login.cs` : `Id`, `UserId`, `DateLogin` (DateTime UTC), `NbrTries` (int), `User User` ; constantes `MaxFailures = 5`, `LockDuration = TimeSpan.FromMinutes(15)` ; méthodes `bool IsLocked(DateTime now)` (= `NbrTries >= 5 && now - DateLogin < 15 min`), `DateTime? LockedUntil` (= `DateLogin + 15 min` si `NbrTries >= 5`), `void ResetIfLockExpired(DateTime now)`, `void RegisterFailure(DateTime now)` (`NbrTries += 1`, `DateLogin = now`), `void RegisterSuccess(DateTime now)` (`NbrTries = 0`, `DateLogin = now`) — règles R8.
- [X] T012 [P] Créer `backend/src/CollectionApp.Domain/Users/RefreshToken.cs` : `Id`, `UserId`, `TokenHash`, `CreatedAt`, `ExpiresAt`, `RevokedAt` (DateTime?), `User User` ; `static RefreshToken Issue(int userId, string tokenHash, DateTime now, DateTime expiresAt)`, `bool IsActive(DateTime now)` (= `RevokedAt == null && now < ExpiresAt`), `void Revoke(DateTime now)` (`RevokedAt ??= now`, idempotent).
- [X] T013 [P] Créer `backend/src/CollectionApp.Domain/Users/IUserRepository.cs` (`ExistsByEmailAsync`, `GetByEmailWithLoginAndClaimsAsync`, `GetByIdWithClaimsAsync`, `GetClaimByNameAsync`, `AddAsync`, `SaveChangesAsync`) et `backend/src/CollectionApp.Domain/Users/IRefreshTokenRepository.cs` (`GetByHashAsync`, `AddAsync`, `SaveChangesAsync`), avec `CancellationToken` partout.
- [X] T014 [P] Tests unitaires Domain dans `backend/tests/CollectionApp.Tests/Users/LoginTests.cs` et `backend/tests/CollectionApp.Tests/Users/RefreshTokenTests.cs` : transitions de `Login` (4 échecs → non bloqué ; 5e échec → bloqué ; à 14 min 59 s bloqué ; à 15 min non bloqué ; `ResetIfLockExpired` remet à 0 ; succès remet à 0) et de `RefreshToken` (`IsActive` avant/après `ExpiresAt`, `Revoke` idempotent) ; `User.NormalizeEmail(" A@B.fr ") == "a@b.fr"`.

### Persistance (data-model.md)

- [X] T015 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Configurations/UserConfiguration.cs` : table `users` ; `id` PK auto-incrément ; `lastName` et `firstName` `varchar(100)` NOT NULL ; `email` `varchar(254)` NOT NULL avec **index unique** `IX_users_email` ; `passwordHash` `varchar(255)` NOT NULL ; noms de colonnes en camelCase.
- [X] T016 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Configurations/ClaimValueConfiguration.cs` : table `claimValues` ; `id` PK **sans auto-incrément** (`ValueGeneratedNever`) ; `name` `varchar(50)` NOT NULL, index unique `IX_claimValues_name` ; `description` `varchar(255)` NOT NULL ; `HasData` : `(1, "User", "Droit de base de tout utilisateur")`, `(2, "LogRead", "Lecture des journaux")` (R9).
- [X] T017 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Configurations/UserClaimConfiguration.cs` : table `userClaims` ; `HasKey(x => new { x.UserId, x.ClaimId })` ; `HasOne(x => x.User).WithMany(u => u.UserClaims)` en `ON DELETE CASCADE` ; `HasOne(x => x.Claim).WithMany(c => c.UserClaims).OnDelete(DeleteBehavior.Restrict)` ; index `IX_userClaims_claimId`.
- [X] T018 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Configurations/LoginConfiguration.cs` : table `logins` ; `id` PK auto-incrément ; `userId` NOT NULL, FK `users.id` `ON DELETE CASCADE`, **index unique** `IX_logins_userId` ; relation `HasOne(l => l.User).WithOne(u => u.Login).HasForeignKey<Login>(l => l.UserId)` ; `dateLogin` `datetime(6)` NOT NULL ; `nbrTries` `int` NOT NULL, défaut 0.
- [X] T019 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` : table `refreshTokens` ; `id` PK auto-incrément ; `userId` NOT NULL, FK `users.id` `ON DELETE CASCADE`, index `IX_refreshTokens_userId` ; `tokenHash` `char(64)` NOT NULL, **index unique** `IX_refreshTokens_tokenHash` ; `createdAt`, `expiresAt` `datetime(6)` NOT NULL ; `revokedAt` `datetime(6)` NULL.
- [X] T020 Dans `backend/src/CollectionApp.Infrastructure/Persistence/CollectionAppDbContext.cs` : ajouter les `DbSet` `Users`, `ClaimValues`, `UserClaims`, `Logins`, `RefreshTokens` et `OnModelCreating` → `modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollectionAppDbContext).Assembly)` ; mettre à jour le commentaire XML (« No entity yet » n'est plus vrai).
- [X] T021 Générer la migration depuis `backend/` : `dotnet ef migrations add AddUserAuth -p src/CollectionApp.Infrastructure -s src/CollectionApp.Api -o Persistence/Migrations`. Vérifier : 5 tables, index et FK conformes à T015–T019, 2 lignes `InsertData` pour `claimValues` ; **`*_InitialCreate.cs` non modifié** (principe V).
- [X] T022 [P] Créer `backend/src/CollectionApp.Infrastructure/Persistence/Repositories/UserRepository.cs` (`Include` de `Login` et `UserClaims.Claim` selon la méthode ; `SaveChangesAsync` traduit une `DbUpdateException` sur `IX_users_email` en `AlreadyExistException("Cette adresse e-mail est déjà utilisée")`, R5) et `backend/src/CollectionApp.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`.

### Sécurité (research R2, R3, R6)

- [X] T023 Déplacer `ClaimPrincipalTypes.cs` et `ClaimPrincipalValues.cs` de `backend/src/CollectionApp.Api/Http/Security/` vers `backend/src/CollectionApp.Application/Security/` (namespace `CollectionApp.Application.Security`), **source unique** des noms de claims pour Api et Infrastructure (analyse H2) ; mettre à jour les `using` de `ClaimPrincipalExtension.cs`, `CurrentUser.cs` et `HttpException.cs`. Créer dans `backend/src/CollectionApp.Application/Auth/` : `AuthOptions.cs` (`Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`, `RefreshTokenDays`, section `Jwt`), `IPasswordHasher.cs` (`string Hash(string password)`, `PasswordCheck Verify(string hash, string password)` avec `enum PasswordCheck { Failed, Success, SuccessRehashNeeded }`), `ITokenService.cs` (`AccessToken CreateAccessToken(User user, IEnumerable<string> claims, DateTime now)` → valeur + `ExpiresAt` ; `string CreateRefreshTokenValue()` ; `string HashRefreshToken(string value)`), et les DTO `UserDto.cs` (`Id`, `LastName`, `FirstName`, `Email`, `Claims`) et `AuthSessionDto.cs` (`AccessToken`, `AccessTokenExpiresAt`, `RefreshToken`, `RefreshTokenExpiresAt`, `User`) conformes au contrat.
- [X] T024 [P] Créer `backend/src/CollectionApp.Infrastructure/Security/IdentityPasswordHasher.cs` : implémente `IPasswordHasher` avec `PasswordHasher<User>` (format V3) ; expose aussi un **hash factice** précalculé (`DummyHash`) pour R7.
- [X] T025 [P] Créer `backend/src/CollectionApp.Infrastructure/Security/JwtTokenService.cs` : `JsonWebTokenHandler`, HS256, claims (constantes de `CollectionApp.Application.Security.ClaimPrincipalTypes`, T023) : `sub` **et** `userId` = id utilisateur (`userId` est le claim lu par `ClaimPrincipalExtension.GetUserId()` ; sans lui `ICurrentUser.UserId` vaudrait `null` avec `MapInboundClaims = false` — analyse H1), `email`, `firstname`, `lastname` et un claim `claims` par droit ; `exp` = `now + AccessTokenMinutes` ; refresh token = 64 octets `RandomNumberGenerator` en base64url ; `HashRefreshToken` = SHA-256 en hexadécimal minuscule (64 caractères).
- [X] T026 Dans `backend/src/CollectionApp.Infrastructure/DependencyInjection.cs` : enregistrer `IUserRepository`, `IRefreshTokenRepository`, `IPasswordHasher`, `ITokenService` ; lier `AuthOptions` à la section `Jwt` et **lever une `InvalidOperationException` explicite au démarrage si `Jwt:SigningKey` est vide ou fait moins de 32 octets**.
- [X] T027 Dans `backend/src/CollectionApp.Api/Startup.cs`, `ConfigureServices` : `services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()` puis `services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<AuthOptions>, TimeProvider>((o, auth, clock) => ...)` (pour lire `AuthOptions` et le `TimeProvider` du conteneur, y compris le `FakeTimeProvider` des tests) avec `ValidIssuer`, `ValidAudience`, `IssuerSigningKey`, **`ClockSkew = TimeSpan.FromSeconds(30)`** (R2, FR-014), `MapInboundClaims = false`, `o.TimeProvider = clock` ; `services.AddAuthorization()`. `UseAuthentication`/`UseAuthorization` existent déjà dans `ConfigureAuthorization`.
- [X] T028 [P] Dans `backend/src/CollectionApp.Api/Swagger/DependencyInjection.cs`, remplacer le schéma OAuth2 externe (`EndPoints:AuthenticationAPI` + `/identity/oauth/token`) par un schéma `http` / `bearer` / `JWT` nommé `bearerAuth`, appliqué aux seuls endpoints `[Authorize]` (contrat : `security: []` sur register/login/refresh) ; retirer `EndPoints:AuthenticationAPI` de `backend/tests/CollectionApp.Tests/Infrastructure/ApiFactory.cs` s'il n'est plus lu.

### Socle de tests

- [X] T029 Dans `backend/tests/CollectionApp.Tests/Infrastructure/ApiFactory.cs` : injecter `Jwt:SigningKey` (clé de test fixe ≥ 32 octets) dans `AddInMemoryCollection` ; remplacer le `TimeProvider` enregistré par un **`FakeTimeProvider` exposé en propriété `Clock`** (`ConfigureTestServices`) pour R11 ; ajouter un helper `Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)` (register + login, header Bearer) ; **désactiver la limitation de débit par défaut** (`IpRateLimiting:EnableEndpointRateLimiting = false` dans `AddInMemoryCollection`), avec un paramètre de constructeur `bool rateLimiting = false` pour la réactiver dans `RateLimitTests` uniquement — sinon les classes de tests qui enchaînent plus de 20 POST par minute (`LockoutTests`, `RegisterTests`…) recevraient des 429 (analyse H3).
- [X] T030 [P] Créer `backend/tests/CollectionApp.Tests/Auth/AuthTestData.cs` : constantes `ValidPassword = "Collec2026"`, générateur d'e-mails uniques, et méthodes `RegisterAsync(HttpClient, ...)` / `LoginAsync(HttpClient, ...)` désérialisant `UserDto` / `AuthSessionDto`.

### Socle front

- [X] T031 [P] Créer `frontend/src/app/services/token-storage.service.ts` (+ `.spec.ts`) : encapsule `@capacitor/preferences` (clés `accessToken`, `refreshToken`) : `get()`, `save(session)`, `clear()`. Aucun autre fichier n'accède à `Preferences` (R10).
- [X] T032 [P] Créer `frontend/src/app/services/auth.models.ts` : interfaces `UserDto`, `AuthSession`, `RegisterRequest`, `LoginRequest`, `ApiError` (dont `error.fields?: { field: string; message: string }[]`), conformes à `contracts/auth.openapi.yaml`.
- [X] T033 Créer `frontend/src/app/services/auth.service.ts` (+ `.spec.ts` initial) : signal `currentUser`, méthodes `register`, `login`, `logout`, `refresh`, `loadCurrentUser` ; **seul** point d'appel HTTP de `/api/auth/*` (principe VI). Les méthodes sont complétées story par story.

**Checkpoint**: `dotnet build backend/CollectionApp.slnx` passe, la migration `AddUserAuth` s'applique sur une base vierge, T005 et T014 passent, le front compile.

---

## Phase 3: User Story 1 - Créer un compte (Priority: P1) 🎯 MVP

**Goal**: un visiteur crée un compte (nom, prénom, e-mail, mot de passe) et reçoit le droit `User`.

**Independent Test**: depuis l'écran d'inscription, créer un compte valide, puis constater qu'une nouvelle inscription avec le même e-mail est refusée.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T034 [P] [US1] Tests unitaires du validator dans `backend/tests/CollectionApp.Tests/Auth/RegisterUserCommandValidatorTests.cs` : `lastName`/`firstName` « obligatoires, 1 à 100 caractères après `Trim` » ; `email` « obligatoire, format e-mail, ≤ 254 caractères » ; `password` « ≥ 8 caractères, au moins une lettre et un chiffre, ≤ 128 caractères » (`abcdefgh` et `12345678` refusés, `Collec2026` accepté) ; `passwordConfirmation` « égal à `password` » (US1-AC3, US1-AC4).
- [X] T035 [P] [US1] Tests d'intégration dans `backend/tests/CollectionApp.Tests/Auth/RegisterTests.cs` (collection `"MySql"`, base migrée) : `US1_AC1_ValidData_Returns201WithNormalizedEmail` (201, e-mail ` Test@Example.fr ` renvoyé `test@example.fr`, aucun champ mot de passe/hash dans la réponse) ; `US1_AC2_SameEmailDifferentCase_Returns409` (message « Cette adresse e-mail est déjà utilisée ») ; `US1_AC3_InvalidFields_Returns400WithFieldErrors` (`error.fields` contient `email` et `password`) ; `US1_AC4_ConfirmationMismatch_Returns400` ; `US1_AC5_NewUser_HasOnlyUserClaim` (`claims == ["User"]` et une seule ligne `userClaims` en base) ; `FR005_PasswordStoredHashed` (`passwordHash` ≠ mot de passe et commence par le préfixe V3) ; `EdgeCase_ConcurrentRegistrations_OnlyOneSucceeds` (2 requêtes en parallèle : un 201 et un 409).
- [X] T036 [P] [US1] Tests Vitest dans `frontend/src/app/auth/register/register.page.spec.ts` : `'US1-AC1 : inscription réussie → message + redirection /login'` ; `'US1-AC2 : 409 → message e-mail déjà utilisé'` ; `'US1-AC3 : 400 → erreurs affichées sous chaque champ'` ; `'US1-AC4 : confirmation différente → aucune requête'` ; `'EdgeCase : API injoignable → message, champs conservés sauf mot de passe'` ; `'EdgeCase : double appui → une seule requête'`.

### Implementation for User Story 1

- [X] T037 [US1] Créer `backend/src/CollectionApp.Application/Auth/RegisterUserCommand.cs` : record `RegisterUserCommand(LastName, FirstName, Email, Password, PasswordConfirmation) : IRequest<UserDto>`, son `RegisterUserCommandValidator` (règles de T034, messages en français) et son handler : `ExistsByEmailAsync(NormalizeEmail(email))` → `AlreadyExistException` ; `User.Create(..., hasher.Hash(password))` ; ajout d'**un seul** `UserClaim` vers le droit `ClaimValue.UserName` obtenu par `GetClaimByNameAsync` (FR-006) ; `SaveChangesAsync` ; renvoie `UserDto`.
- [X] T038 [US1] Créer `backend/src/CollectionApp.Api/Controllers/AuthController.cs` : `[ApiController] [Route("api/auth")]`, action `[AllowAnonymous] [HttpPost("register")]` → `_mediator.Send(...)` → `201 Created` avec `UserDto` ; `[ProducesResponseType]` 201/400/409 conformes au contrat. Aucune logique métier (principe III). T034 et T035 doivent passer.
- [X] T039 [US1] Implémenter `register()` dans `frontend/src/app/services/auth.service.ts` (`POST {apiUrl}api/auth/register`).
- [X] T040 [US1] Créer la page `frontend/src/app/auth/register/register.page.ts|html|scss` (standalone Ionic, mobile-first) : champs nom, prénom, e-mail, mot de passe, confirmation ; validations ergonomiques uniquement (obligatoire, format, confirmation identique — les règles font foi côté API) ; erreurs `error.fields` affichées sous chaque champ, erreurs globales en bandeau (FR-017) ; bouton désactivé pendant l'envoi ; champ mot de passe vidé après une erreur réseau ; succès → toast « Compte créé » puis `/login` ; lien « J'ai déjà un compte » (FR-015).
- [X] T041 [US1] Dans `frontend/src/app/app.routes.ts`, ajouter les routes publiques `register` (et `login`, page créée en US2) en `loadComponent`. T036 doit passer.

**Checkpoint**: User Story 1 est fonctionnelle et testable seule (quickstart scénarios 3 à 8).

---

## Phase 4: User Story 2 - Se connecter (Priority: P1)

**Goal**: un utilisateur inscrit se connecte avec e-mail + mot de passe ; blocage 15 min après 5 échecs ; session conservée à la réouverture.

**Independent Test**: avec un compte existant, se connecter et accéder à l'app ; avec un mauvais mot de passe, constater le refus.

### Tests for User Story 2 ⚠️

- [X] T042 [P] [US2] Tests unitaires du handler dans `backend/tests/CollectionApp.Tests/Auth/LoginCommandHandlerTests.cs` (fakes de repositories / hasher) : e-mail inconnu → `Verify` appelé avec `DummyHash` puis `UnauthorizedException` (R7, SC-005) ; compte bloqué → `LockedException` **sans** appel à `Verify` ni modification de `Login` (FR-010) ; `SuccessRehashNeeded` → hash mis à jour.
- [X] T043 [P] [US2] Tests d'intégration dans `backend/tests/CollectionApp.Tests/Auth/LoginTests.cs` : `US2_AC1_ValidCredentials_Returns200WithSession` (200, tokens non vides, `accessTokenExpiresAt = now + 15 min`, `refreshTokenExpiresAt = now + 7 j`, e-mail saisi en majuscules accepté) ; `US2_AC2_WrongPassword_Returns401GenericMessage` (« E-mail ou mot de passe incorrect ») ; `US2_AC3_UnknownEmail_ReturnsSameResponseAsWrongPassword` (même code, même `error.code`, même `message`) ; `US2_AC6_SuccessAfterFailures_ResetsCounter` (`nbrTries` = 0 en base) ; `FR009_Failure_UpdatesDateLoginAndCounter` (une seule ligne `logins` par utilisateur).
- [X] T044 [P] [US2] Tests d'intégration du blocage dans `backend/tests/CollectionApp.Tests/Auth/LockoutTests.cs` (avec `ApiFactory.Clock`) : `US2_AC4_FiveFailures_ReturnsLockedEvenWithGoodPassword` (423 + en-tête `Retry-After`) ; `US2_AC4_AttemptsDuringLock_DoNotExtendLock` (tentatives à +5 et +10 min, puis succès à +15 min) ; `US2_AC5_After15Minutes_LoginSucceeds` ; `Assumption_AfterLockExpiry_CounterRestartsAtZero` (après la fin du blocage, 4 échecs ne bloquent pas).
- [X] T045 [P] [US2] Tests d'intégration dans `backend/tests/CollectionApp.Tests/Auth/MeTests.cs` : `US2_AC7_ValidAccessToken_ReturnsCurrentUser` (200, `id` = id de l'utilisateur inscrit, `claims` = `["User"]`) ; `Me_WithoutToken_Returns401` ; `H1_IssuedToken_PopulatesCurrentUserId` (un access token émis par `JwtTokenService` donne `ICurrentUser.UserId` = id de l'utilisateur et `HasClaim("User")` = true).
- [X] T046 [P] [US2] Tests Vitest dans `frontend/src/app/auth/login/login.page.spec.ts` et `frontend/src/app/services/auth.service.spec.ts` : `'US2-AC1 : connexion réussie → tokens stockés + redirection accueil'` ; `'US2-AC2/AC3 : 401 → message générique'` ; `'US2-AC4 : 423 → message compte temporairement bloqué'` ; `'US2-AC7 : au démarrage, tokens présents + /me 200 → utilisateur connecté'` ; `'EdgeCase : API injoignable → message clair'`.

### Implementation for User Story 2

- [X] T047 [US2] Créer `backend/src/CollectionApp.Application/Auth/LoginCommand.cs` : record `LoginCommand(Email, Password) : IRequest<AuthSessionDto>`, validator (e-mail et mot de passe obligatoires, ≤ 254 / ≤ 128), handler appliquant **exactement** R8 avec `TimeProvider` : utilisateur inconnu → `Verify(DummyHash, password)` puis `UnauthorizedException("E-mail ou mot de passe incorrect")`, rien n'est enregistré ; `Login` créé à la première tentative ; `IsLocked(now)` → `LockedException("Compte temporairement bloqué. Réessayez dans N minutes.", retryAfter: LockedUntil - now)` sans vérifier ni modifier ; sinon `ResetIfLockExpired`, `Verify`, `RegisterFailure` + 401 ou `RegisterSuccess` ; en cas de succès, crée un `RefreshToken.Issue(..., now, now + RefreshTokenDays)` (seul le hash est stocké) et renvoie `AuthSessionDto`.
- [X] T048 [US2] Créer `backend/src/CollectionApp.Application/Auth/GetCurrentUserQuery.cs` : `IRequest<UserDto>` lisant `ICurrentUser.UserId` puis `GetByIdWithClaimsAsync` ; utilisateur absent → `UnauthorizedException`.
- [X] T049 [US2] Dans `backend/src/CollectionApp.Api/Controllers/AuthController.cs`, ajouter `[AllowAnonymous] [HttpPost("login")]` (200 / 400 / 401 / 423 ; l'en-tête `Retry-After` est posé par la couche d'erreurs à partir de `LockedException.RetryAfter` (T006) — le contrôleur ne référence **aucun** type de `Domain`) et `[Authorize] [HttpGet("me")]` (200 / 401). T042 à T045 doivent passer.
- [X] T050 [US2] Dans `frontend/src/app/services/auth.service.ts`, implémenter `login()` (enregistre la session via `TokenStorageService`, met à jour `currentUser`) et `loadCurrentUser()` (tokens présents → `GET api/auth/me`).
- [X] T051 [US2] Créer la page `frontend/src/app/auth/login/login.page.ts|html|scss` : champs e-mail et mot de passe, messages globaux (401 générique, 423 bloqué, API injoignable), bouton désactivé pendant l'envoi, lien « Créer un compte » (FR-015), affichage du message « Votre session a expiré » si l'URL contient `expired=1` (utilisé en US4) ; succès → `/tabs/tab1`.
- [X] T052 [US2] Dans `frontend/src/app/app.component.ts`, appeler `authService.loadCurrentUser()` au démarrage (US2-AC7). T046 doit passer.

**Checkpoint**: US1 et US2 fonctionnent chacune seule (quickstart scénarios 9 à 13). ⚠️ US2-AC7 (quickstart scénario 14) ne fonctionne à ce stade que tant que l'access token est valide (15 min) : la réouverture après expiration nécessite le renouvellement automatique livré en US4 (T064, test T062).

---

## Phase 5: User Story 3 - Se déconnecter (Priority: P2)

**Goal**: un utilisateur connecté se déconnecte ; sa session ne peut plus être prolongée et tout accès est refusé au plus tard 15 min après.

**Independent Test**: se connecter, se déconnecter, puis constater que l'accès aux écrans réservés redirige vers la connexion.

### Tests for User Story 3 ⚠️

- [X] T053 [P] [US3] Tests d'intégration dans `backend/tests/CollectionApp.Tests/Auth/RefreshTests.cs` : `Refresh_ValidToken_ReturnsNewPairAndRevokesOld` (l'ancien refresh token → 401 ensuite) ; `FR013_Refresh_KeepsAbsoluteExpiry` (`refreshTokenExpiresAt` inchangé après rotation) ; `FR013_After7Days_RefreshReturns401` (`Clock` + 7 j) ; `Refresh_UnknownToken_Returns401`.
- [X] T054 [P] [US3] Tests d'intégration dans `backend/tests/CollectionApp.Tests/Auth/LogoutTests.cs` : `US3_AC1_Logout_Returns204AndRevokesToken` (`revokedAt` renseigné) ; `US3_AC2_RefreshAfterLogout_Returns401` ; `US3_AC3_AccessTokenRejected15MinutesAfterLogout` (`Clock` + 15 min 31 s → `/me` 401) ; `Logout_Twice_Returns204` (idempotent) ; `Logout_OtherUsersToken_Returns204WithoutRevoking` ; `Assumption_LogoutOnlyRevokesCurrentDevice` (2 connexions, déconnexion de l'une, refresh de l'autre → 200).
- [X] T055 [P] [US3] Tests Vitest dans `frontend/src/app/services/auth.service.spec.ts` et `frontend/src/app/tab3/tab3.page.spec.ts` : `'US3-AC1 : se déconnecter → POST logout, stockage vidé, redirection /login'` ; `'US3-AC1 : logout en échec réseau → stockage vidé quand même'`.

### Implementation for User Story 3

- [X] T056 [US3] Créer `backend/src/CollectionApp.Application/Auth/RefreshTokenCommand.cs` : `RefreshTokenCommand(RefreshToken) : IRequest<AuthSessionDto>` + validator (obligatoire) ; handler : `GetByHashAsync(HashRefreshToken(value))`, absent ou `!IsActive(now)` → `UnauthorizedException` ; sinon `old.Revoke(now)` et `RefreshToken.Issue(userId, newHash, now, old.ExpiresAt)` (rotation, expiration absolue conservée — R3) ; nouvel access token avec les droits courants de l'utilisateur.
- [X] T057 [US3] Créer `backend/src/CollectionApp.Application/Auth/LogoutCommand.cs` : `LogoutCommand(RefreshToken) : IRequest` + validator ; handler : token trouvé **et** appartenant à `ICurrentUser.UserId` → `Revoke(now)` ; sinon aucun effet ; jamais d'erreur (R4).
- [X] T058 [US3] Dans `backend/src/CollectionApp.Api/Controllers/AuthController.cs`, ajouter `[AllowAnonymous] [HttpPost("refresh")]` (200 / 400 / 401) et `[Authorize] [HttpPost("logout")]` (204 / 401). T053 et T054 doivent passer.
- [X] T059 [US3] Dans `frontend/src/app/services/auth.service.ts`, implémenter `refresh()` (met à jour le stockage) et `logout()` (`POST api/auth/logout` avec le refresh token, puis `TokenStorageService.clear()`, `currentUser` à `null` et navigation `/login`, **même si l'appel échoue**).
- [X] T060 [US3] Ajouter un bouton « Se déconnecter » dans `frontend/src/app/tab3/tab3.page.html|ts` (visible si connecté), qui appelle `authService.logout()`. T055 doit passer.

**Checkpoint**: US1 à US3 fonctionnent (quickstart scénarios 15 à 17).

---

## Phase 6: User Story 4 - Accès protégé (Priority: P2)

**Goal**: sans connexion, seuls les écrans de connexion, d'inscription et de statut sont accessibles ; une session expirée renvoie vers la connexion.

**Independent Test**: sans être connecté, ouvrir un écran réservé et constater la redirection vers la connexion.

### Tests for User Story 4 ⚠️

- [X] T061 [P] [US4] Tests Vitest dans `frontend/src/app/guards/auth.guard.spec.ts` : `'US4-AC1 : non connecté → redirection /login'` ; `'US4-AC2 : route status accessible sans connexion'` ; `'connecté → accès autorisé'`.
- [X] T062 [P] [US4] Tests Vitest dans `frontend/src/app/interceptors/auth.interceptor.spec.ts` : ajoute `Authorization: Bearer` sauf sur `api/auth/login|register|refresh` et `api/health` ; `'401 → un seul refresh puis requête rejouée'` ; `'3 requêtes en 401 simultanées → un seul appel /refresh'` ; `'US4-AC3 : refresh en échec → stockage vidé + /login?expired=1'` ; `'US2-AC7 : réouverture après expiration de l'access token → /me 401 → refresh → /me rejoué → connecté'` (complète US2-AC7, analyse M1).
- [X] T063 [P] [US4] Test d'intégration back dans `backend/tests/CollectionApp.Tests/Auth/ProtectedEndpointTests.cs` : `US4_AC2_HealthStaysAnonymous` (`/api/health` 200 sans token) ; `US4_AC3_ExpiredAccessToken_Returns401` (`Clock` + 15 min 31 s).

### Implementation for User Story 4

- [X] T064 [US4] Créer `frontend/src/app/interceptors/auth.interceptor.ts` (`HttpInterceptorFn`) selon R10 : Bearer, refresh unique partagé (`shareReplay` sur l'appel en cours), rejeu, échec → `TokenStorageService.clear()` + `router.navigate(['/login'], { queryParams: { expired: 1 } })` ; l'enregistrer dans `frontend/src/main.ts` via `provideHttpClient(withInterceptors([authInterceptor]))`.
- [X] T065 [US4] Créer `frontend/src/app/guards/auth.guard.ts` (`CanActivateFn`) : `currentUser` présent, sinon tentative `loadCurrentUser()`, sinon `UrlTree` vers `/login`.
- [X] T066 [US4] Dans `frontend/src/app/tabs/tabs.routes.ts`, appliquer `canActivate: [authGuard]` aux routes `tab1`, `tab2`, `tab3` ; **ne pas** le mettre sur `status` (FR-016). T061 à T063 doivent passer.

**Checkpoint**: toutes les user stories sont fonctionnelles (quickstart scénarios 1, 2, 18).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: limitation de débit, contrat OpenAPI, sécurité des journaux, validation finale

- [X] T067 [P] Limitation de débit (R12) : dans `backend/src/CollectionApp.Api/appsettings.json`, ajouter la section `IpRateLimiting` (`EnableEndpointRateLimiting: true`, `StackBlockedRequests: false`, `HttpStatusCode: 429`, règle `{ "Endpoint": "post:/api/auth/*", "Period": "1m", "Limit": 20 }`) et `IpRateLimitPolicies` vide ; test `backend/tests/CollectionApp.Tests/Auth/RateLimitTests.cs` (seule classe créant `ApiFactory` avec `rateLimiting: true`, T029) : `R12_TwentyFirstLoginInAMinute_Returns429`. Relancer **toute** la suite back ensuite pour vérifier qu'aucun autre test ne reçoit de 429.
- [X] T068 [P] Créer `backend/tests/CollectionApp.Tests/Auth/AuthOpenApiTests.cs` (sur le modèle de `Health/HealthOpenApiTests.cs`) : les 5 chemins de `contracts/auth.openapi.yaml` existent avec leurs codes de réponse ; schéma `bearerAuth` sur `logout` et `me` uniquement ; `UserDto` sans propriété contenant `password`.
- [X] T069 [P] Créer `backend/tests/CollectionApp.Tests/Auth/SensitiveDataTests.cs` : après register / login / refresh / logout, aucun mot de passe, access token ni refresh token en clair dans `ApiFactory.Logs` (SC-004) ; table `refreshTokens` ne contient que des hashes de 64 caractères hexadécimaux.
- [X] T070 Créer `CLAUDE.md` à la racine (absent du dépôt) : stack, commandes (`dotnet test backend/CollectionApp.slnx`, `dotnet ef ... -p backend/src/CollectionApp.Infrastructure -s backend/src/CollectionApp.Api`, `cd frontend; npm test -- --watch=false`), règle « lire la constitution et la spec active avant toute implémentation », secrets requis (`ConnectionStrings:CollectionApp`, `Jwt:SigningKey`).
- [X] T071 Exécuter tous les tests back et front, puis les 19 scénarios et les contrôles qualité de `specs/002-user-auth/quickstart.md` ; cocher les tâches et noter les écarts éventuels.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Foundational (Phase 2)** : dépend de la Phase 1 ; **bloque toutes les stories**. Ordre interne : T005 → T006/T007 ; T008–T013 (Domain) → T015–T019 → T020 → T021 → T022 ; T023 → T024/T025 → T026 → T027 ; T026/T027 → T029.
- **US1 (Phase 3)** : dépend de la Phase 2.
- **US2 (Phase 4)** : dépend de la Phase 2 ; ses tests utilisent l'inscription (T037/T038) pour créer les comptes → à faire après US1.
- **US3 (Phase 5)** : dépend de US2 (il faut une session pour se déconnecter).
- **US4 (Phase 6)** : dépend de US2 (`loadCurrentUser`) et de US3 (`refresh`).
- **Polish (Phase 7)** : dépend de toutes les stories.

### Within Each User Story

- Tests écrits et **en échec** avant l'implémentation.
- Application (commande + validator + handler) → contrôleur → service front → page / routes.

### Parallel Opportunities

- Phase 1 : T002 et T003 en parallèle de T001.
- Phase 2 : les 5 entités (T008–T012), les interfaces (T013), les 5 configurations (T015–T019), T024/T025 et le socle front (T031/T032) sont dans des fichiers distincts.
- Dans chaque story : toutes les tâches de tests marquées [P].
- Phase 7 : T067, T068, T069 en parallèle.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Entités Domain, en parallèle :
Task: "Créer User.cs dans backend/src/CollectionApp.Domain/Users/"
Task: "Créer ClaimValue.cs dans backend/src/CollectionApp.Domain/Users/"
Task: "Créer UserClaim.cs dans backend/src/CollectionApp.Domain/Users/"
Task: "Créer Login.cs dans backend/src/CollectionApp.Domain/Users/"
Task: "Créer RefreshToken.cs dans backend/src/CollectionApp.Domain/Users/"

# Puis les configurations EF, en parallèle :
Task: "UserConfiguration.cs … RefreshTokenConfiguration.cs dans backend/src/CollectionApp.Infrastructure/Persistence/Configurations/"
```

## Parallel Example: User Story 2

```bash
Task: "LoginCommandHandlerTests.cs"
Task: "LoginTests.cs"
Task: "LockoutTests.cs"
Task: "MeTests.cs"
Task: "login.page.spec.ts + auth.service.spec.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 : Setup.
2. Phase 2 : Foundational (CRITICAL — bloque toutes les stories).
3. Phase 3 : US1 (inscription).
4. **STOP and VALIDATE** : tests US1 verts + quickstart scénarios 3 à 8.

### Incremental Delivery

1. Setup + Foundational → socle prêt (migration appliquée, erreurs 400 correctes).
2. US1 → inscription (MVP).
3. US2 → connexion + blocage : l'application devient utilisable.
4. US3 → déconnexion + refresh.
5. US4 → protection des écrans + renouvellement automatique.
6. Polish → limitation de débit, contrat OpenAPI, journaux, `CLAUDE.md`.

Commit conseillé à chaque checkpoint (`ft(#3) : …`).

---

## Notes

- [P] tasks = fichiers différents, sans dépendance sur une tâche incomplète.
- Chaque critère d'acceptation (US1-AC1 … US4-AC3) a au moins un test nommé d'après lui (principe II).
- Ne jamais modifier `*_InitialCreate.cs` ; toute correction de schéma passe par une nouvelle migration.
- Aucun secret dans les fichiers versionnés : `Jwt:SigningKey` vide dans `appsettings.json`.
