# Implementation Plan: Gestion des utilisateurs (inscription, connexion, déconnexion)

**Branch**: `feat/#3` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-user-auth/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Permettre à un visiteur de créer un compte (nom, prénom, e-mail, mot de passe), de se connecter
avec son e-mail et son mot de passe, puis de se déconnecter ; protéger les écrans réservés.

Approche technique :

- **Persistance** : 5 tables MySQL via EF Core — `users`, `claimValues`, `userClaims` (n:n),
  `logins` (une ligne par utilisateur : dernière tentative + échecs consécutifs) et
  `refreshTokens` (sessions révocables).
- **Authentification** : access token JWT court (15 min, sans état) + refresh token opaque
  (7 jours) stocké haché en base ; la déconnexion révoque le refresh token.
- **Mots de passe** : `PasswordHasher<T>` d'ASP.NET Core Identity utilisé seul (sans le
  framework Identity).
- **Front** : écrans Ionic de connexion et d'inscription, `AuthService`, intercepteur HTTP
  (Bearer + renouvellement automatique), guard de routes ; tokens stockés via
  `@capacitor/preferences`.

## Technical Context

**Language/Version**: C# / .NET 10 (back) ; TypeScript strict / Angular 22 + Ionic 9 (front)

**Primary Dependencies**:
- Existants : ASP.NET Core, MediatR 14, FluentValidation 12, `MySql.EntityFrameworkCore` 10.0.12,
  `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.14 (référencé, pas encore configuré).
- Ajouts : `Microsoft.Extensions.Identity.Core` (`PasswordHasher<T>`, Infrastructure) ;
  émission des JWT avec la bibliothèque `Microsoft.IdentityModel.JsonWebTokens` déjà tirée par
  JwtBearer (version exacte en Phase 0) ; `@capacitor/preferences` (front).

**Storage**: MySQL (Docker), schéma par migration EF Core `AddUserAuth` ; claims `User` et
`LogRead` insérés par les données de référence de la migration.

**Testing**: xUnit + `WebApplicationFactory` + Testcontainers MySQL (fixtures existantes
`ApiFactory`, `MySqlContainerFixture`), `FakeTimeProvider` pour le blocage et l'expiration ;
Vitest côté front.

**Target Platform**: API ASP.NET Core (Docker / local) ; app Ionic navigateur + Android/iOS
(Capacitor).

**Project Type**: mobile-app + API REST

**Performance Goals**: connexion perçue < 1 s (SC-002 : connexion complète < 30 s) ;
hachage PBKDF2 aux paramètres par défaut d'Identity.

**Constraints**:
- Aucun secret versionné : clé de signature JWT via `dotnet user-secrets` (`Jwt:SigningKey`).
- Message d'échec de connexion identique e-mail inconnu / mauvais mot de passe (FR-008, SC-005).
- Blocage 15 min après 5 échecs consécutifs (FR-010), calculé avec `TimeProvider`.
- Révocation effective au plus tard à l'expiration de l'access token (15 min) — cf. amendement
  FR-014.

**Scale/Scope**: usage personnel / petit groupe (< 1 000 utilisateurs) ; 4 entités + 1 table de
sessions, 5 endpoints, 2 écrans.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Vérification | Statut |
|---|---|---|
| I. Spec d'abord | Spec 002 validée (clarifications du 2026-11-18) ; FR-009 et FR-014 amendés pour refléter D1/D2 avant tout code | ✅ |
| II. Critères testés | Chaque scénario d'acceptation → test d'intégration API ou test Vitest (mapping dans `tasks.md`) | ✅ |
| III. Contrat API explicite | 5 endpoints documentés OpenAPI ; DTO dans `Application`, entités jamais exposées ; `AuthController` délègue à MediatR | ✅ |
| IV. Clean Architecture | Entités + interfaces de repositories dans `Domain` ; commandes/handlers/DTO/interfaces `IPasswordHasher` et `ITokenService` dans `Application` ; EF, hachage et émission JWT dans `Infrastructure` ; `Api` = contrôleur + configuration JwtBearer | ✅ |
| V. Migrations immuables | Nouvelle migration `AddUserAuth`, aucune migration existante modifiée | ✅ |
| VI. Front sans logique métier | Validations de formulaire uniquement ergonomiques (les règles font foi côté API) ; appels HTTP centralisés dans `AuthService` | ✅ |
| VII. Aucun secret versionné | Clé JWT dans user-secrets ; seule une valeur d'exemple vide dans `appsettings.json` | ✅ |
| VIII. Simplicité | Refresh token = complexité ajoutée → justifiée ci-dessous | ⚠️ justifié |

### Re-check post-design (Phase 1) — 2026-11-18

| Principe | Vérification sur les artefacts | Statut |
|---|---|---|
| I | spec, research, data-model, contrat et quickstart cohérents (FR-009/FR-014 amendés) | ✅ |
| II | Chaque scénario US1-1 → US4-3 a un test prévu (R11) et une ligne dans le quickstart | ✅ |
| III | `contracts/auth.openapi.yaml` : 5 endpoints, DTO `User`/`AuthSession` sans hash ni entité | ✅ |
| IV | Entités + règles `Login`/`RefreshToken` + repositories (interfaces) dans Domain ; hachage/JWT/EF dans Infrastructure | ✅ |
| V | Une seule nouvelle migration `AddUserAuth` (schéma + `HasData`) | ✅ |
| VI | Front : validation ergonomique seulement, `AuthService` + intercepteur + guard | ✅ |
| VII | `Jwt:SigningKey` en user-secrets ; tokens de session stockés hachés | ✅ |
| VIII | Pas de framework Identity complet, RS256 ni détection de réutilisation (écartés, R2/R3) | ✅ (refresh justifié) |

## Project Structure

### Documentation (this feature)

```text
specs/002-user-auth/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/src/
├── CollectionApp.Domain/
│   └── Users/                       # User, ClaimValue, UserClaim, Login, RefreshToken,
│                                    # IUserRepository, IRefreshTokenRepository
├── CollectionApp.Application/
│   └── Auth/                        # RegisterUserCommand, LoginCommand, RefreshTokenCommand,
│                                    # LogoutCommand, GetCurrentUserQuery (+ handlers, validators),
│                                    # DTO, IPasswordHasher, ITokenService, AuthOptions
├── CollectionApp.Infrastructure/
│   ├── Persistence/
│   │   ├── CollectionAppDbContext.cs        # DbSets
│   │   ├── Configurations/                  # IEntityTypeConfiguration<> par entité (relations, index, seed)
│   │   ├── Repositories/                    # UserRepository, RefreshTokenRepository
│   │   └── Migrations/                      # AddUserAuth
│   └── Security/                            # IdentityPasswordHasher, JwtTokenService
└── CollectionApp.Api/
    ├── Controllers/AuthController.cs
    └── Startup.cs                           # AddAuthentication().AddJwtBearer(...)

backend/tests/CollectionApp.Tests/
└── Auth/                            # RegisterTests, LoginTests, LockoutTests, RefreshTests,
                                     # LogoutTests, AuthOpenApiTests

frontend/src/app/
├── auth/
│   ├── login/                       # page de connexion
│   └── register/                    # page d'inscription
├── services/
│   ├── auth.service.ts              # appels /api/auth/*
│   └── token-storage.service.ts     # @capacitor/preferences
├── interceptors/auth.interceptor.ts # Bearer + refresh sur 401
├── guards/auth.guard.ts             # redirection vers /login
└── app.routes.ts                    # routes publiques : login, register, status
```

**Structure Decision**: on étend la structure existante (Clean Architecture back, `frontend/`
Ionic) ; un dossier `Users/` dans Domain et `Auth/` dans Application regroupent la feature,
comme `Health/` pour la feature 001.

## Écarts de l'existant à corriger dans cette feature

Constatés en concevant le contrat d'erreur ([contracts/auth.openapi.yaml](contracts/auth.openapi.yaml)) :

1. **`ValidationException` (FluentValidation) non mappée** : `ExceptionStatusMapper` ne la connaît
   pas, donc une erreur de validation renvoie aujourd'hui **500** au lieu de 400, sans détail par
   champ. Correction : la mapper sur 400 et ajouter `error.fields` (`field`, `message`) au corps
   d'erreur (FR-017).
2. **Pile d'appels exposée à tous** : `HttpException.ToJson()` remplit `StackTrace` avec
   `FullStackTrace()` sans condition (la méthode `GetStackTrace()` protégée par `LogRead` n'est
   pas utilisée). Correction : n'inclure `stackTrace` que pour un utilisateur ayant `LogRead`,
   comme `details` — indispensable dès que l'API manipule des comptes.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Refresh token + table `refreshTokens` + endpoint `/refresh` + intercepteur de renouvellement | FR-014 exige qu'une session déconnectée soit refusée, et FR-013 une session de 7 jours | JWT seul de 7 jours sans état : impossible de révoquer à la déconnexion (contredit FR-014) ; session vérifiée en base à chaque requête : écartée par choix utilisateur (D1 = C) |
