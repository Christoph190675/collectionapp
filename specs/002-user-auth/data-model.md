# Data Model: Gestion des utilisateurs

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

5 tables MySQL créées par la migration EF Core `AddUserAuth`. Les entités vivent dans
`CollectionApp.Domain/Users/`, leurs configurations (`IEntityTypeConfiguration<T>`) dans
`CollectionApp.Infrastructure/Persistence/Configurations/`. Toutes les dates sont en **UTC**.

## Vue d'ensemble

```text
users 1 ──── n userClaims n ──── 1 claimValues
  │
  ├── 1 ──── 0..1 logins
  │
  └── 1 ──── n refreshTokens
```

## users — `User`

| Colonne | Type MySQL | Contraintes | Propriété C# |
|---|---|---|---|
| `id` | `int` | PK, auto-incrément | `int Id` |
| `lastName` | `varchar(100)` | NOT NULL | `string LastName` |
| `firstName` | `varchar(100)` | NOT NULL | `string FirstName` |
| `email` | `varchar(254)` | NOT NULL, **index unique** `IX_users_email` | `string Email` |
| `passwordHash` | `varchar(255)` | NOT NULL | `string PasswordHash` |

Navigations : `ICollection<UserClaim> UserClaims`, `Login? Login`,
`ICollection<RefreshToken> RefreshTokens`.

**Règles (Domain)** :
- `User.Create(lastName, firstName, email, passwordHash)` : `Trim()` sur nom et prénom,
  e-mail normalisé `Trim().ToLowerInvariant()` (R5) via `User.NormalizeEmail(string)`, réutilisé
  à la connexion.
- Le mot de passe en clair ne traverse jamais le Domain : seul le hash est stocké (FR-005).

**Validation (Application, FluentValidation — `RegisterUserCommandValidator`)** :
- `lastName`, `firstName` : obligatoires, 1 à 100 caractères après `Trim` (FR-003).
- `email` : obligatoire, format e-mail, ≤ 254 caractères (FR-003).
- `password` : ≥ 8 caractères, au moins une lettre et un chiffre, ≤ 128 caractères (FR-004).
- `passwordConfirmation` : égal à `password` (US1-4).

## claimValues — `ClaimValue`

| Colonne | Type MySQL | Contraintes | Propriété C# |
|---|---|---|---|
| `id` | `int` | PK (valeurs fixes, pas d'auto-incrément) | `int Id` |
| `name` | `varchar(50)` | NOT NULL, **index unique** `IX_claimValues_name` | `string Name` |
| `description` | `varchar(255)` | NOT NULL | `string Description` |

Navigation : `ICollection<UserClaim> UserClaims`.

**Données de référence** (`HasData`, R9) :

| id | name | description |
|---|---|---|
| 1 | `User` | Droit de base de tout utilisateur |
| 2 | `LogRead` | Lecture des journaux |

Les noms sont exposés en constantes dans `ClaimValue` (`ClaimValue.UserName = "User"`) ;
`ClaimPrincipalValues.LogRead` (déplacée dans `Application/Security`, partagée par Api et
Infrastructure) reste la constante utilisée côté autorisation.

## userClaims — `UserClaim`

| Colonne | Type MySQL | Contraintes | Propriété C# |
|---|---|---|---|
| `userId` | `int` | **PK composée** (1/2), FK → `users.id`, `ON DELETE CASCADE` | `int UserId` |
| `claimId` | `int` | **PK composée** (2/2), FK → `claimValues.id`, `ON DELETE RESTRICT`, index `IX_userClaims_claimId` | `int ClaimId` |

Navigations : `User User`, `ClaimValue Claim`.

**Configuration EF** : `HasKey(x => new { x.UserId, x.ClaimId })` ;
`HasOne(x => x.User).WithMany(u => u.UserClaims)` ;
`HasOne(x => x.Claim).WithMany(c => c.UserClaims).OnDelete(DeleteBehavior.Restrict)`.
Entité de jointure explicite (et non `HasMany().WithMany()` implicite) pour garder le nom de
table et de colonnes demandé et pouvoir ajouter des attributs plus tard.

**Règle** : à l'inscription, une seule ligne `(userId, 1 = User)` est créée (FR-006).

## logins — `Login`

| Colonne | Type MySQL | Contraintes | Propriété C# |
|---|---|---|---|
| `id` | `int` | PK, auto-incrément | `int Id` |
| `userId` | `int` | NOT NULL, FK → `users.id`, `ON DELETE CASCADE`, **index unique** `IX_logins_userId` | `int UserId` |
| `dateLogin` | `datetime(6)` | NOT NULL, UTC | `DateTime DateLogin` |
| `nbrTries` | `int` | NOT NULL, défaut 0, ≥ 0 | `int NbrTries` |

Navigation : `User User`. Relation **1 : 0..1** (`HasOne(u => u.Login).WithOne(l => l.User)`),
garantie par l'index unique sur `userId` (D2 : une seule ligne par utilisateur).

**Règles (Domain, R8)** — constantes `MaxFailures = 5`, `LockDuration = 15 min` :

| Méthode | Effet |
|---|---|
| `bool IsLocked(DateTime now)` | `NbrTries >= 5 && now - DateLogin < 15 min` |
| `void ResetIfLockExpired(DateTime now)` | si `NbrTries >= 5` et blocage échu → `NbrTries = 0` |
| `void RegisterFailure(DateTime now)` | `NbrTries += 1`, `DateLogin = now` |
| `void RegisterSuccess(DateTime now)` | `NbrTries = 0`, `DateLogin = now` |

**Transitions d'état** :

```text
             échec (n<4)              5e échec
  [Libre n=0] ──────► [Libre n=1..4] ─────────► [Bloqué n=5]
      ▲    ▲  succès        │                       │  tentative pendant 15 min :
      │    └────────────────┘                       │  423, aucune modification
      │                                             │
      └──────────── +15 min puis tentative ─────────┘  (n remis à 0, puis échec/succès normal)
```

## refreshTokens — `RefreshToken`

| Colonne | Type MySQL | Contraintes | Propriété C# |
|---|---|---|---|
| `id` | `int` | PK, auto-incrément | `int Id` |
| `userId` | `int` | NOT NULL, FK → `users.id`, `ON DELETE CASCADE`, index `IX_refreshTokens_userId` | `int UserId` |
| `tokenHash` | `char(64)` | NOT NULL, **index unique** `IX_refreshTokens_tokenHash` (SHA-256 en hexadécimal) | `string TokenHash` |
| `createdAt` | `datetime(6)` | NOT NULL, UTC | `DateTime CreatedAt` |
| `expiresAt` | `datetime(6)` | NOT NULL, UTC | `DateTime ExpiresAt` |
| `revokedAt` | `datetime(6)` | NULL, UTC | `DateTime? RevokedAt` |

Navigation : `User User`. Relation **1 : n** (plusieurs appareils).

**Règles (Domain, R3/R4)** :

| Méthode | Effet |
|---|---|
| `RefreshToken.Issue(userId, tokenHash, now, expiresAt)` | `CreatedAt = now` ; à la connexion `expiresAt = now + 7 j` |
| `bool IsActive(DateTime now)` | `RevokedAt == null && now < ExpiresAt` |
| `void Revoke(DateTime now)` | `RevokedAt ??= now` (idempotent) |
| Rotation (`/refresh`) | `old.Revoke(now)` puis `Issue(userId, newHash, now, old.ExpiresAt)` — l'expiration absolue est conservée |

La valeur en clair du token n'est jamais stockée : seule `tokenHash` est en base (R3).

## Repositories (interfaces Domain, implémentations Infrastructure)

- `IUserRepository` : `ExistsByEmailAsync(email)`, `GetByEmailWithLoginAndClaimsAsync(email)`,
  `GetByIdWithClaimsAsync(id)`, `AddAsync(user)`, `GetClaimByNameAsync(name)`,
  `SaveChangesAsync()` (traduit la violation de `IX_users_email` en `AlreadyExistException`).
- `IRefreshTokenRepository` : `GetByHashAsync(tokenHash)`, `AddAsync(token)`,
  `SaveChangesAsync()`.

## Correspondance spec → modèle

| Exigence | Élément du modèle |
|---|---|
| FR-001, FR-003 | `users` + `RegisterUserCommandValidator` |
| FR-002, edge case concurrence | `User.NormalizeEmail` + `IX_users_email` |
| FR-005, SC-004 | `users.passwordHash`, aucune colonne en clair |
| FR-006 | `userClaims (userId, 1)` + seed `claimValues` |
| FR-009 à FR-011 | `logins` + méthodes de `Login` |
| FR-012, FR-013 | `refreshTokens.expiresAt` (7 j absolus) |
| FR-014 | `refreshTokens.revokedAt` |
