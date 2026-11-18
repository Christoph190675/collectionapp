# Quickstart: Intégration continue

**Feature**: [spec.md](./spec.md) | Workflow : [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)

## Ce que fait la CI

À chaque PR vers `main` (et à chaque commit arrivant sur `main`), deux vérifications tournent en
parallèle sur GitHub :

| Vérification | Étapes |
|---|---|
| **Back (.NET)** | restore → build Release → tests (dont intégration MySQL via Testcontainers) → contrôle « modèle EF sans migration » → artefact `backend-test-results` (fichiers `.trx`) |
| **Front (Angular)** | `npm ci` → lint → tests Vitest → build de production |

Aucun secret n'est utilisé.

## Lire un échec

1. Sur la PR, onglet **Checks** (ou lien « Details » du statut en échec).
2. L'étape en rouge indique la cause (build, test, lint, migration manquante).
3. Back : télécharger l'artefact `backend-test-results` en bas de la page de l'exécution pour le
   détail des tests (ouvrable dans Visual Studio / VS Code).

## Reproduire en local (depuis la racine)

```powershell
cd backend
dotnet build CollectionApp.slnx -c Release
dotnet test CollectionApp.slnx -c Release --no-build
$env:ConnectionStrings__CollectionApp = "Server=localhost;Database=design_time;User=ci;Password=ci"
$env:Jwt__SigningKey = "design-time-key-not-a-secret-0123456789abcdef"
dotnet ef migrations has-pending-model-changes -p src/CollectionApp.Infrastructure -s src/CollectionApp.Api --configuration Release --no-build
cd ../frontend
npm ci; npx ng lint; npx ng test --watch=false; npx ng build
```

## Rendre la CI obligatoire avant merge (propriétaire du dépôt)

GitHub → **Settings** → **Branches** (ou **Rules** → **Rulesets**) → règle sur `main` :
- « Require a pull request before merging » ;
- « Require status checks to pass » → sélectionner **Back (.NET)** et **Front (Angular)**
  (ils n'apparaissent dans la liste qu'après une première exécution).
