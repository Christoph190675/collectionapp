---

description: "Task list for feature 003-continuous-integration"
---

# Tasks: Intégration continue

**Input**: [spec.md](spec.md), [plan.md](plan.md)

**Tests**: la CI se vérifie par son exécution : chaque critère d'acceptation est validé par une
exécution réelle sur GitHub (T006 à T008), y compris un échec volontaire (SC-003).

## Phase 1: Workflow (US1, US2)

- [X] T001 [US1] Créer `.github/workflows/ci.yml` : déclencheurs `pull_request` et `push` sur `main` (FR-001, US2-AC1) ; `permissions: contents: read` (FR-007) ; `concurrency` par branche avec `cancel-in-progress` (FR-006).
- [X] T002 [US1] Job **Back (.NET)** : `setup-dotnet` 10.0.x, restore, build Release, `dotnet test` avec logger TRX (FR-002), `dotnet ef migrations has-pending-model-changes` avec configuration factice de design-time (FR-003), artefact TRX `if: always()` (FR-008).
- [X] T003 [US1] Job **Front (Angular)** : Node 24 avec cache npm, `npm ci`, `ng lint`, `ng test --watch=false`, `ng build` (FR-004). Deux jobs distincts = deux statuts sur la PR (FR-005).

## Phase 2: Validation

- [X] T004 Rejouer en local les commandes exactes des deux jobs (Release, `--no-build`, variables de design-time, `npm ci`) : toutes doivent réussir.
- [X] T005 [P] Documenter dans `specs/003-continuous-integration/quickstart.md` et `CLAUDE.md` : lecture des résultats, artefact TRX, activation de la protection de branche par le propriétaire du dépôt.
- [ ] T006 [US1] Ouvrir la PR de `feat/#4` : les statuts « Back (.NET) » et « Front (Angular) » apparaissent et passent (US1-AC1, SC-001), durée < 10 min (SC-002).
- [ ] T007 [US1] Sur une branche jetable : casser un test back et un test front → chaque vérification échoue sur la bonne étape (US1-AC2, SC-003) ; modifier une entité sans migration → « Check pending model changes » échoue (US1-AC4). Branche supprimée ensuite.
- [ ] T008 [US2] Après fusion : une exécution démarre sur `main` (US2-AC1).
