# Feature Specification: Intégration continue

**Feature Branch**: `feat/#4`

**Created**: 2026-11-18

**Status**: Validated (2026-11-18)

**Input**: User description: "Implémenter l'intégration continue."

## Contexte

La constitution exige, avant tout merge, que le back et le front compilent sans erreur et que
tous les tests passent (« Quality gates »), « en local et en CI » (principe II). Aujourd'hui le
dépôt n'a aucune CI : la PR #2 a été fusionnée sur la seule foi des tests lancés en local.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Chaque pull request est vérifiée automatiquement (Priority: P1)

En tant que développeur, quand j'ouvre ou mets à jour une pull request vers `main`, je vois sur
la PR si le back et le front compilent et si tous leurs tests passent, sans rien lancer moi-même.

**Why this priority**: c'est l'objet même de la CI ; elle rend les quality gates de la
constitution vérifiables par tous.

**Independent Test**: ouvrir une PR vers `main` et constater que les vérifications démarrent
seules et affichent un résultat.

**Acceptance Scenarios**:

1. **Given** une PR ouverte vers `main`, **When** un commit y est poussé, **Then** les
   vérifications back et front démarrent automatiquement et leur résultat apparaît sur la PR.
2. **Given** un commit qui casse un test (back ou front), **When** la CI s'exécute, **Then** la
   vérification concernée échoue et désigne l'étape en échec.
3. **Given** un commit qui ne compile pas ou ne respecte pas le lint front, **When** la CI
   s'exécute, **Then** la vérification échoue.
4. **Given** une modification du modèle EF Core sans migration correspondante, **When** la CI
   s'exécute, **Then** la vérification back échoue (principe V).

---

### User Story 2 - La branche principale reste vérifiée (Priority: P2)

En tant que développeur, après chaque fusion dans `main`, la même vérification tourne sur `main`
pour détecter immédiatement un problème issu de la combinaison de plusieurs branches.

**Why this priority**: utile, mais secondaire tant que les PR sont vérifiées.

**Independent Test**: fusionner une PR et constater qu'une exécution démarre sur `main`.

**Acceptance Scenarios**:

1. **Given** une fusion dans `main`, **When** le commit arrive sur `main`, **Then** la CI
   s'exécute sur `main`.

---

### Edge Cases

- Deux commits poussés coup sur coup sur la même PR : seule l'exécution la plus récente compte,
  la précédente est annulée.
- Une PR qui ne touche que la documentation (`specs/`, `*.md`) : la CI s'exécute quand même
  (résultat toujours disponible pour décider du merge).
- Aucun secret n'est nécessaire à la CI : les tests fournissent leur propre configuration et
  leur propre base (conteneur éphémère).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La CI MUST s'exécuter automatiquement à chaque ouverture ou mise à jour d'une PR
  vers `main`, et à chaque commit arrivant sur `main`.
- **FR-002**: La CI MUST compiler le back et exécuter tous ses tests, y compris les tests
  d'intégration sur une base MySQL éphémère.
- **FR-003**: La CI MUST vérifier qu'aucun changement du modèle EF Core n'est dépourvu de
  migration.
- **FR-004**: La CI MUST installer les dépendances front à l'identique du fichier de
  verrouillage, exécuter le lint, tous les tests unitaires et le build de production.
- **FR-005**: Le back et le front MUST être vérifiés par deux vérifications distinctes, chacune
  avec son propre statut sur la PR.
- **FR-006**: Une exécution en cours MUST être annulée quand un nouveau commit arrive sur la même
  branche.
- **FR-007**: La CI MUST NOT utiliser de secret ni écrire dans le dépôt (permissions en lecture
  seule).
- **FR-008**: En cas d'échec des tests back, les résultats détaillés MUST être consultables
  depuis l'exécution.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % des PR vers `main` affichent un statut back et un statut front.
- **SC-002**: Une exécution complète dure moins de 10 minutes.
- **SC-003**: Un test volontairement cassé fait échouer la vérification correspondante.

## Assumptions

- Plateforme : GitHub Actions (le dépôt est hébergé sur GitHub), runners Linux hébergés par
  GitHub, sur lesquels Docker est disponible pour Testcontainers.
- Les vérifications sont obligatoires avant merge sur `main` (protection de branche, réglage du
  dépôt GitHub activé le 2026-11-18 à la demande du propriétaire, voir quickstart).
- Hors périmètre : déploiement, build Android/iOS, analyse de couverture, publication d'artefacts
  applicatifs.
