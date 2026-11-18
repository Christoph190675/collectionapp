# Quickstart: Gestion des utilisateurs

**Feature**: [spec.md](./spec.md) | Contrat : [contracts/auth.openapi.yaml](./contracts/auth.openapi.yaml) |
Modèle : [data-model.md](./data-model.md)

Guide de validation de bout en bout. Les commandes sont lancées depuis la racine du dépôt.

## Prérequis

- Prérequis de la feature 001 ([quickstart 001](../001-health-status/quickstart.md)) : Docker,
  conteneur MySQL sur `localhost:3306`, SDK .NET 10, `dotnet tool restore`, `npm ci` dans
  `frontend/`.
- Chaîne de connexion déjà configurée en user-secrets (feature 001).

## 1. Configuration (une seule fois)

```powershell
# Clé de signature JWT : au moins 32 octets aléatoires, jamais versionnée (principe VII)
$key = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
dotnet user-secrets set "Jwt:SigningKey" $key --project backend/src/CollectionApp.Api

# Schéma : tables users, claimValues, userClaims, logins, refreshTokens + droits User / LogRead
dotnet ef database update -p backend/src/CollectionApp.Infrastructure -s backend/src/CollectionApp.Api
```

**Attendu** : la migration `AddUserAuth` est appliquée ; `claimValues` contient `1 User` et
`2 LogRead`.

## 2. Tests automatisés

```powershell
dotnet test backend/CollectionApp.slnx        # nécessite Docker (Testcontainers)
cd frontend; npm test -- --watch=false; cd ..
```

**Attendu** : tous les tests passent, y compris le blocage et l'expiration (temps simulé par
`FakeTimeProvider`, R11). Chaque scénario d'acceptation a son test (voir `tasks.md`).

## 3. Scénarios manuels

Lancer l'API (`dotnet run --project backend/src/CollectionApp.Api`) et le front
(`cd frontend; npm start`), puis ouvrir `http://localhost:8100`.

| # | Étapes | Résultat attendu | Spec |
|---|--------|------------------|------|
| 1 | Non connecté, ouvrir un onglet réservé | Redirection vers l'écran de connexion | US4-1 |
| 2 | Non connecté, ouvrir l'onglet Statut | Accessible, API = OK / Base = OK | US4-2 |
| 3 | « Créer un compte » : nom, prénom, ` Test@Example.fr `, `Collec2026` ×2 | Message de confirmation, redirection vers la connexion | US1-1 |
| 4 | Recommencer l'inscription avec `test@example.fr` | « Cette adresse e-mail est déjà utilisée » | US1-2 |
| 5 | Inscription avec e-mail `abc`, mot de passe `abcdefgh` | Erreurs affichées sous les champs e-mail et mot de passe | US1-3, FR-017 |
| 6 | Inscription avec une confirmation différente | Refus avant envoi (aucune requête dans l'onglet Réseau) | US1-4 |
| 7 | Dans MySQL : `SELECT c.name FROM userClaims uc JOIN claimValues c ON c.id = uc.claimId JOIN users u ON u.id = uc.userId WHERE u.email = 'test@example.fr';` | Une seule ligne : `User` | US1-5, FR-006 |
| 8 | `SELECT passwordHash FROM users WHERE email = 'test@example.fr';` | Valeur hachée (préfixe base64 `AQAAAA…`), jamais `Collec2026` | FR-005, SC-004 |
| 9 | Connexion avec `TEST@example.fr` / `Collec2026` | Connecté, écran d'accueil | US2-1 |
| 10 | Se déconnecter, puis connexion avec un mauvais mot de passe | « E-mail ou mot de passe incorrect » | US2-2 |
| 11 | Connexion avec `inconnu@example.fr` | Exactement le même message | US2-3, SC-005 |
| 12 | 5 mauvais mots de passe de suite, puis le bon | « Compte temporairement bloqué », même avec le bon mot de passe | US2-4, FR-010 |
| 13 | `UPDATE logins SET dateLogin = DATE_SUB(UTC_TIMESTAMP(6), INTERVAL 16 MINUTE) WHERE userId = <id>;` puis connexion avec le bon mot de passe | Connexion réussie ; `nbrTries` = 0 | US2-5, US2-6 |
| 14 | Connecté, fermer l'onglet puis rouvrir l'app | Toujours connecté | US2-7 |
| 15 | Se déconnecter | Retour à l'écran de connexion ; `revokedAt` renseigné dans `refreshTokens` | US3-1 |
| 16 | Rejouer `POST /api/auth/refresh` avec le refresh token de la session déconnectée (Swagger ou curl) | 401 | US3-2 |
| 17 | Rejouer `GET /api/auth/me` avec l'ancien access token 16 min après la déconnexion | 401 | US3-3, FR-014 |
| 18 | Supprimer l'entrée `refreshToken` dans le stockage de l'app (DevTools > Application), attendre l'expiration de l'access token puis agir | Redirection vers la connexion avec « Votre session a expiré » | US4-3 |
| 19 | 21 `POST /api/auth/login` en moins d'une minute depuis la même IP | La 21ᵉ renvoie 429 | R12 |

## 4. Contrôles qualité

- Les 5 endpoints `/api/auth/*` apparaissent dans Swagger (`http://localhost:8080/swagger`) avec
  le schéma de sécurité Bearer sur `logout` et `me`.
- Une erreur de validation renvoie **400** avec `error.fields` (et non 500) ; aucune réponse
  d'erreur ne contient `stackTrace` pour un utilisateur sans `LogRead`.
- Aucun mot de passe ni token en clair dans les journaux de l'API.
- `git status` ne montre aucun secret ; la migration `InitialCreate` n'a pas été modifiée.
