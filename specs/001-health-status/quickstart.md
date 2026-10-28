# Quickstart: Statut de santé (walking skeleton)

**Feature**: [spec.md](./spec.md) | Contrat : [contracts/health.openapi.yaml](./contracts/health.openapi.yaml)

Guide de validation de bout en bout. Les commandes sont lancées depuis la racine du dépôt.

## Prérequis

- Docker Desktop démarré (requis par Testcontainers) et ton conteneur MySQL (géré hors du projet), joignable sur `localhost:3306`
- SDK .NET 10 (ou plus récent) et outil local `dotnet-ef` (`dotnet tool restore`)
- Node.js et dépendances front installées (`cd frontend && npm ci`)
- Pour Android : Android Studio + un émulateur

## 1. Configuration (une seule fois)

```powershell
dotnet user-secrets set "ConnectionStrings:CollectionApp" `
  "Server=localhost;Port=3306;Database=collectionapp;User=root;Password=<MYSQL_ROOT_PASSWORD>;Connection Timeout=2" `
  --project backend/src/CollectionApp.Api
```

## 2. Tests automatisés

```powershell
dotnet test backend/CollectionApp.slnx        # nécessite Docker (Testcontainers)
cd frontend; npm test -- --watch=false; cd ..
```

**Attendu** : tous les tests passent. Chaque critère d'acceptation de la spec est couvert (voir
`tasks.md`).

## 3. Scénarios manuels

| # | Étapes | Résultat attendu | Spec |
|---|--------|------------------|------|
| 1 | Base `collectionapp` vierge sur ton conteneur, `dotnet run --project backend/src/CollectionApp.Api`, `cd frontend; npm start`, ouvrir `http://localhost:8100/tabs/status` | API = OK, Base de données = **KO** (migration en attente) ; l'API n'a pas créé de table | US5-1, FR-014 |
| 2 | `dotnet ef database update -p backend/src/CollectionApp.Infrastructure -s backend/src/CollectionApp.Api`, puis bouton « Relancer » | API = OK, Base de données = OK, heure mise à jour | US1-1, US3-1, US5-2 |
| 3 | Relancer la commande `dotnet ef database update` | « No migrations were applied », aucune erreur | US5-3 |
| 4 | Arrêter ton conteneur MySQL, puis « Relancer » | Réponse en moins de 3 s : API = OK, Base de données = KO | US2-1 |
| 5 | Redémarrer ton conteneur MySQL, puis « Relancer » | Base de données = OK | US3-1 |
| 6 | Arrêter l'API, puis « Relancer » | Message « API injoignable », base « inconnu », l'application reste utilisable | US2-2 |
| 7 | Pendant une vérification lente, appuyer de nouveau sur « Relancer » | Indicateur visible, bouton désactivé, une seule requête | US3-2 |
| 8 | `curl -i -H "Origin: http://evil.example" http://localhost:8080/api/health` | Pas d'en-tête `Access-Control-Allow-Origin` | US4-2 |
| 9 | Même commande avec `Origin: http://localhost:8100` | `Access-Control-Allow-Origin: http://localhost:8100` | US4-1 |
| 10 | `cd frontend; npx ng build --configuration android; $env:CAP_ENV='dev'; npx cap sync android; npx cap run android` | Onglet Statut identique sur l'émulateur, API = OK / Base = OK | FR-015 |

## 4. Contrôles qualité

- `GET /api/health` est documenté dans Swagger (`http://localhost:8080/swagger`).
- `git status` ne montre aucun secret.
