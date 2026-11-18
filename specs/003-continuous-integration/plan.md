# Implementation Plan: Intégration continue

**Branch**: `feat/#4` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

## Summary

Un workflow GitHub Actions `.github/workflows/ci.yml`, déclenché sur les PR vers `main` et les
push sur `main`, avec deux jobs parallèles : **Back** (.NET) et **Front** (Angular/Ionic).

## Technical Context

**Platform**: GitHub Actions, runner `ubuntu-latest` (Docker disponible pour Testcontainers).

**Back**: `actions/setup-dotnet` en `10.0.x` (le projet cible `net10.0` ; le SDK 11 RC local n'est
pas reproduit en CI pour éviter une préversion) ; `dotnet restore` → `dotnet build -c Release
--no-restore` → `dotnet test -c Release --no-build` (logger TRX) ; `dotnet tool restore` puis
`dotnet ef migrations has-pending-model-changes` (FR-003). Pas de cache NuGet : celui de
`setup-dotnet` exige des `packages.lock.json`, absents du projet (à reconsidérer si la durée
dépasse SC-002).

**Front**: `actions/setup-node` en Node 24 (version locale du projet), cache npm sur
`frontend/package-lock.json` ; `npm ci` → `ng lint` → `ng test --watch=false` →
`ng build`.

**Contraintes**:
- `permissions: contents: read` au niveau du workflow (FR-007) ; aucun secret.
- `concurrency` par workflow + branche avec `cancel-in-progress: true` (FR-006).
- `has-pending-model-changes` démarre l'hôte de l'API (design-time) : `AddInfrastructure` exige
  une chaîne de connexion et une clé JWT. On fournit des valeurs factices par variables
  d'environnement (`ConnectionStrings__CollectionApp`, `Jwt__SigningKey`) — ce ne sont pas des
  secrets, aucune connexion n'est ouverte.
- Résultats TRX publiés en artefact (FR-008), même en cas d'échec (`if: always()`).
- `timeout-minutes: 15` par job (garde-fou, SC-002 visé < 10 min).

**Versions des actions** (dernières versions majeures publiées, vérifiées le 2026-11-18) :
`actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/setup-node@v7`,
`actions/upload-artifact@v7`.

## Constitution Check

| Principe | Vérification | Statut |
|---|---|---|
| I. Spec d'abord | Spec 003 rédigée et validée avant le workflow | ✅ |
| II. Critères testés | La CI exécute tous les tests « en local et en CI » ; SC-003 vérifié en cassant volontairement un test | ✅ |
| V. Migrations immuables | `has-pending-model-changes` bloque un modèle sans migration | ✅ |
| VII. Aucun secret | Aucun secret ; valeurs factices non sensibles pour le design-time | ✅ |
| VIII. Simplicité | Un seul fichier de workflow, pas d'action tierce hors `actions/*` | ✅ |
| Quality gates | Build back et front, tous les tests, lint : automatisés | ✅ |

## Project Structure

```text
.github/workflows/ci.yml        # le workflow
specs/003-continuous-integration/
├── spec.md
├── plan.md
├── quickstart.md               # vérifier la CI, activer la protection de branche
└── tasks.md
CLAUDE.md                       # section CI
```
