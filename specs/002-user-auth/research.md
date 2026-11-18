# Research: Gestion des utilisateurs

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-11-18

Toutes les inconnues du Technical Context sont résolues ci-dessous.

## R1 — Versions des dépendances ajoutées

- **Decision**:
  - `Microsoft.Extensions.Identity.Core` **10.0.14** (Infrastructure), aligné sur
    `JwtBearer` 10.0.14 et `EntityFrameworkCore.Design` 10.0.14 déjà présents.
  - Émission des JWT avec `Microsoft.IdentityModel.JsonWebTokens` (`JsonWebTokenHandler`),
    version **8.19.2** déjà tirée transitivement par JwtBearer, référencée explicitement dans
    Infrastructure à la même version.
  - Front : `@capacitor/preferences` **8.0.1**, compatible avec `@capacitor/core` 8.5.3.
- **Rationale**: aucune nouvelle famille de dépendances ; versions homogènes avec l'existant.
- **Alternatives considered**: `System.IdentityModel.Tokens.Jwt` (`JwtSecurityTokenHandler`) —
  API historique, plus lente, que Microsoft recommande de remplacer par `JsonWebTokenHandler`.

## R2 — Format et validation de l'access token

- **Decision**:
  - JWT signé **HS256**, clé symétrique d'au moins 32 octets lue dans `Jwt:SigningKey`
    (`dotnet user-secrets` en dev, variable d'environnement en Docker) ; démarrage en erreur si
    la clé est absente ou trop courte.
  - `iss` = `aud` = `collection-app` ; durée **15 min** (`Jwt:AccessTokenMinutes`).
  - Claims, via `ClaimPrincipalTypes` existant : `sub` = id utilisateur, `email`, `firstname`,
    `lastname`, et un claim `claims` par droit (`User`, `LogRead`…), lus par
    `ICurrentUser.HasClaim`.
  - Validation par JwtBearer avec **`ClockSkew` = 30 s** (au lieu de 5 min par défaut) et
    `TimeProvider` injecté.
- **Rationale**: le `ClockSkew` par défaut de 5 min porterait la fenêtre après déconnexion à
  20 min et violerait FR-014 (15 min max). HS256 suffit : l'API est à la fois émettrice et
  consommatrice.
- **Alternatives considered**: RS256 (paire de clés) — utile seulement si un tiers doit valider
  les tokens ; inutile ici (principe VIII).

## R3 — Refresh token et durée de session

- **Decision**:
  - Valeur opaque de 64 octets aléatoires (`RandomNumberGenerator`), encodée base64url, renvoyée
    au client ; seule son **empreinte SHA-256** est stockée en base.
  - **Rotation** à chaque `/refresh` : l'ancien token est révoqué, un nouveau est émis avec la
    **même date d'expiration** (7 jours à partir de la connexion). La session ne dépasse donc
    jamais 7 jours (FR-013).
  - Un token révoqué, expiré ou inconnu → 401, l'app renvoie vers la connexion.
- **Rationale**: le hachage protège les sessions si la base fuit ; la rotation limite la
  réutilisation d'un token volé ; l'expiration absolue respecte FR-013.
- **Alternatives considered**: expiration glissante (prolongée à chaque refresh) — contredit
  FR-013 ; détection de réutilisation avec révocation de toute la famille de tokens — plus sûr
  mais plus complexe, reportée (YAGNI).

## R4 — Déconnexion

- **Decision**: `POST /api/auth/logout` (authentifié) avec le refresh token dans le corps ;
  révoque **uniquement** ce token (session de l'appareil courant, conformément à l'hypothèse
  « plusieurs sessions simultanées »). Réponse 204 même si le token est déjà révoqué
  (idempotent). Côté app : suppression des tokens stockés puis redirection vers `/login`.
- **Rationale**: respecte FR-014 et le scénario US3-1 sans toucher aux autres appareils.
- **Alternatives considered**: « déconnecter partout » — hors périmètre.

## R5 — Normalisation et unicité de l'e-mail

- **Decision**: e-mail normalisé (`Trim()` + `ToLowerInvariant()`) dans le Domain, à
  l'inscription comme à la connexion ; colonne `email` avec **index unique**. Une violation de
  l'index (inscriptions simultanées) est traduite en `AlreadyExistException` → **409**.
  Validation du format par FluentValidation (`EmailAddress()`), longueur max 254.
- **Rationale**: la normalisation explicite ne dépend pas de la collation MySQL ; l'index unique
  est la seule garantie fiable contre la course (edge case « deux inscriptions simultanées »).
- **Alternatives considered**: vérification « existe déjà ? » seule — insuffisante en cas de
  concurrence (gardée uniquement pour un message rapide).

## R6 — Hachage des mots de passe

- **Decision**: `PasswordHasher<User>` (format V3 : PBKDF2-HMAC-SHA512, sel aléatoire,
  100 000 itérations par défaut) derrière l'interface `IPasswordHasher` d'Application. Le
  résultat `SuccessRehashNeeded` déclenche la mise à jour du hash à la connexion.
- **Rationale**: algorithme éprouvé, versionné (évolutif sans migration), sans la lourdeur
  d'ASP.NET Identity (D3).
- **Alternatives considered**: BCrypt.Net (dépendance tierce supplémentaire), Argon2 (pas
  d'implémentation dans le framework).

## R7 — Ne pas révéler l'existence d'un compte

- **Decision**: e-mail inconnu → le handler vérifie quand même le mot de passe saisi contre un
  **hash factice** précalculé, puis renvoie la même 401 avec le même message que pour un mauvais
  mot de passe. Les tentatives sur e-mail inconnu ne sont pas enregistrées (hypothèse de la spec).
- **Rationale**: même contenu **et** même temps de réponse perçu (edge case, SC-005).
- **Alternatives considered**: réponse immédiate — révèle l'existence d'un compte par le temps.

## R8 — Calcul du blocage avec une ligne `logins` par utilisateur (D2)

- **Decision**: à chaque tentative sur un compte existant, avec `now` = `TimeProvider` :
  1. Si `nbrTries >= 5` **et** `now - dateLogin < 15 min` → **423** « compte temporairement
     bloqué », **sans** vérifier le mot de passe **ni** modifier la ligne (le blocage n'est pas
     prolongé, FR-010).
  2. Si `nbrTries >= 5` et le délai est écoulé → `nbrTries = 0` (le compteur repart de zéro).
  3. Mot de passe faux → `nbrTries += 1`, `dateLogin = now`, 401.
  4. Mot de passe correct → `nbrTries = 0`, `dateLogin = now`, 200 + tokens.
  La ligne `logins` est créée à la première tentative. La date du 5e échec est donc le début du
  blocage, puisqu'une tentative bloquée ne met pas à jour `dateLogin`.
- **Rationale**: respecte FR-009 à FR-011 avec exactement les colonnes demandées.
- **Alternatives considered**: verrou optimiste sur la ligne pour des échecs parallèles —
  écarté : au pire un échec simultané n'est pas compté, sans impact sécurité significatif à
  cette échelle.

## R9 — Données de référence des droits

- **Decision**: `HasData` dans `ClaimValueConfiguration` avec des ids fixes :
  `1 = User` (« Droit de base de tout utilisateur »), `2 = LogRead` (« Lecture des journaux »).
  L'inscription attribue le droit `User` en le recherchant **par son nom**.
- **Rationale**: données versionnées dans la migration `AddUserAuth`, reproductibles
  (principe V).
- **Alternatives considered**: seeding au démarrage de l'API — contredit la clarification de la
  feature 001 (l'API ne modifie jamais le schéma ni les données de référence elle-même).

## R10 — Front : intercepteur, guard et stockage

- **Decision**:
  - `TokenStorageService` encapsule `@capacitor/preferences` (clés `accessToken`,
    `refreshToken`).
  - `authInterceptor` (fonctionnel, `HttpInterceptorFn`) : ajoute `Authorization: Bearer` sauf
    sur `/api/auth/login|register|refresh` et `/api/health` ; sur une 401, appelle **une seule
    fois** `/refresh` (requête partagée si plusieurs appels échouent en même temps), puis rejoue
    la requête ; si le refresh échoue → vide le stockage et redirige vers
    `/login?expired=1` (message « Votre session a expiré », US4-3).
  - `authGuard` (`CanActivateFn`) sur les onglets réservés ; `login`, `register` et l'onglet
    Statut restent publics (FR-016).
  - Au démarrage, l'app appelle `GET /api/auth/me` pour savoir si la session est valide (US2-7).
- **Rationale**: un seul point de gestion des tokens (principe VI : aucun `HttpClient` dans les
  composants).
- **Alternatives considered**: renouvellement proactif par minuterie avant expiration — plus de
  code, gain négligeable.

## R11 — Stratégie de tests

- **Decision**:
  - Back : tests d'intégration via `ApiFactory` + Testcontainers MySQL existants ; le
    `TimeProvider` est remplacé par un `FakeTimeProvider` pour tester le blocage (5 échecs →
    423 ; +15 min → succès) et l'expiration de l'access token (+15 min 30 s → 401) sans attente
    réelle ; la clé JWT de test est injectée par configuration de l'`ApiFactory`.
  - Tests unitaires des handlers (règles R8) et des validators.
  - Test OpenAPI des 5 endpoints (sur le modèle de `HealthOpenApiTests`).
  - Front : Vitest pour `AuthService`, `authInterceptor` (refresh unique, redirection),
    `authGuard` et les validations des formulaires.
- **Rationale**: chaque scénario d'acceptation reçoit un test automatisé (principe II), en
  réutilisant l'outillage de la feature 001.

## R12 — Limitation de débit

- **Decision**: ajouter une règle `AspNetCoreRateLimit` (déjà configuré) sur
  `POST:/api/auth/*` : 20 requêtes / minute / IP.
- **Rationale**: complète le blocage par compte (qui ne protège pas contre l'essai d'un mot de
  passe sur de nombreux comptes) pour un coût quasi nul : simple configuration.
- **Alternatives considered**: aucune limite — laisse le balayage de comptes sans frein.
