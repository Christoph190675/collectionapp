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

## CI obligatoire avant merge (activée le 2026-11-18)

Protection de branche sur `main` (GitHub → **Settings** → **Branches**) :
- pull request obligatoire (aucune approbation exigée : projet à un seul développeur) ;
- statuts requis **Back (.NET)** et **Front (Angular)**, acceptés uniquement s'ils viennent de
  GitHub Actions ; la branche n'a pas à être à jour avec `main` pour fusionner ;
- push forcé et suppression de `main` interdits ;
- les administrateurs peuvent contourner la règle (case « bypass » explicite au moment du merge,
  ou `gh pr merge --admin`) : à réserver aux urgences.

Vérifier la règle en ligne de commande :

```powershell
gh api repos/Christoph190675/collectionapp/branches/main/protection --jq "{checks: [.required_status_checks.checks[].context], enforce_admins: .enforce_admins.enabled}"
```
