<!--
Sync Impact Report
- Version change: (template non rempli) → 1.0.0
- Principes définis (création initiale) :
  I. Spec d'abord (NON-NÉGOCIABLE)
  II. Critères d'acceptation testés (NON-NÉGOCIABLE)
  III. Contrat API explicite
  IV. Clean Architecture back-end
  V. Migrations EF Core immuables
  VI. Front mobile-first sans logique métier
  VII. Aucun secret versionné
  VIII. Simplicité d'abord
- Sections ajoutées : Stack & contraintes techniques, Workflow de développement, Governance
- Sections supprimées : aucune
- Templates : non modifiés (lus à l'exécution par les commandes Spec Kit)
- TODO différés : aucun
-->

# Collection App Constitution

Application mobile de gestion de collections (TCG, vinyles, jeux vidéo, etc.).

## Core Principles

### I. Spec d'abord (NON-NÉGOCIABLE)

- Toute feature MUST commencer par une spécification (`spec.md`) validée avant toute ligne de code.
- Aucun code de production n'est écrit sans spec validée ; un changement de comportement
  non couvert par la spec MUST d'abord amender la spec.
- Le plan (`plan.md`) et les tâches (`tasks.md`) dérivent de la spec et MUST rester cohérents
  avec elle.

**Rationale** : la spec est la source de vérité ; elle évite le travail non demandé et rend
chaque décision traçable.

### II. Critères d'acceptation testés (NON-NÉGOCIABLE)

- Chaque critère d'acceptation de la spec MUST être couvert par au moins un test automatisé.
- La correspondance critère → test(s) MUST être identifiable (nom du test, référence dans
  `tasks.md` ou commentaire).
- Une feature n'est pas terminée tant que tous ses tests d'acceptation ne passent pas en local
  et en CI.

**Rationale** : un critère non testé est un critère non garanti.

### III. Contrat API explicite

- L'API back-end est REST et MUST être documentée en OpenAPI, générée depuis le code et à jour.
- Les DTO MUST être distincts des entités du domaine ; aucune entité n'est exposée directement
  par un endpoint.
- Les contrôleurs MUST NOT contenir de logique métier : ils délèguent (via MediatR) et
  traduisent les résultats en réponses HTTP.

**Rationale** : le contrat public reste stable et indépendant du modèle interne.

### IV. Clean Architecture back-end

Dépendances autorisées entre projets (toute autre référence est interdite) :

- `Api` → `Application`, `Api` → `Infrastructure` (composition / injection de dépendances).
  `Api` MUST NOT référencer directement `Domain` : les contrôleurs ne manipulent que les DTO
  et messages définis dans `Application`.
- `Infrastructure` → `Application`, `Infrastructure` → `Domain`.
- `Application` → `Domain`.
- `Domain` MUST NOT dépendre d'aucun autre projet ni d'aucun framework d'infrastructure.

Responsabilités :

- **Api** : uniquement les contrôleurs (et la configuration de démarrage associée).
- **Domain** : objets métier — entités, enums, exceptions métier, interfaces des repositories.
- **Application** : DTO, interfaces des services, messaging (commandes/requêtes/handlers
  MediatR, validateurs FluentValidation).
- **Infrastructure** : persistance (DbContext EF Core), implémentations des repositories,
  implémentations des services d'accès aux ressources externes (API tierces, etc.).

**Rationale** : le métier reste isolé, testable et indépendant des choix techniques.

### V. Migrations EF Core immuables

- Toute évolution du schéma MUST passer par une migration EF Core versionnée dans Git.
- Une migration MUST NOT être modifiée ni supprimée après merge ; toute correction se fait
  par une nouvelle migration.

**Rationale** : garantir des bases reproductibles et des déploiements sans divergence.

### VI. Front mobile-first sans logique métier

- Le front (Ionic + Angular, TypeScript `strict`) MUST être conçu mobile-first.
- Le front MUST NOT contenir de logique métier ; les règles métier vivent dans le back-end.
- Tous les appels API MUST être centralisés dans des services Angular dédiés ; les composants
  n'appellent jamais `HttpClient` directement.

**Rationale** : une seule source de vérité métier et une UI simple à faire évoluer.

### VII. Aucun secret versionné

- Un secret (mot de passe, chaîne de connexion, clé d'API, token) MUST NOT apparaître dans le
  code ni dans l'historique Git.
- Les secrets sont fournis via `dotnet user-secrets` en développement et via fichiers `.env`
  (ignorés par Git) ou variables d'environnement pour Docker.
- Un fichier d'exemple sans valeur réelle (ex. `.env.example`) MAY être versionné.

**Rationale** : un secret commité est un secret compromis.

### VIII. Simplicité d'abord

- Une abstraction, couche, pattern ou dépendance MUST NOT être introduit sans besoin avéré
  et présent (YAGNI).
- Toute complexité supplémentaire MUST être justifiée dans le plan de la feature
  (section « Complexity Tracking »).

**Rationale** : le code le plus simple est le plus facile à lire, tester et modifier.

## Stack & contraintes techniques

- **Front** : Ionic + Angular, TypeScript en mode `strict`, Capacitor pour le mobile.
- **Back** : ASP.NET Core (.NET 10), EF Core, MediatR, FluentValidation.
- **Base de données** : MySQL exécuté dans Docker.
- **Documentation API** : OpenAPI.
- **Structure** : `backend/src/CollectionApp.{Api,Application,Domain,Infrastructure}`,
  tests dans `backend/tests/`, front dans `frontend/`.

Tout changement de stack (ajout/remplacement d'un framework ou d'une base) MUST faire l'objet
d'un amendement de cette constitution.

## Workflow de développement

1. `/speckit-specify` → spec validée (et `/speckit-clarify` si nécessaire).
2. `/speckit-plan` → plan conforme à la constitution (Constitution Check passé).
3. `/speckit-tasks` → tâches incluant les tests de chaque critère d'acceptation.
4. `/speckit-implement` → implémentation, tests verts.

Quality gates avant merge :

- Build back et front sans erreur ; tous les tests passent.
- Chaque critère d'acceptation a au moins un test associé.
- Les règles de dépendance de la Clean Architecture sont respectées.
- Aucune migration existante modifiée ; aucun secret ajouté.
- La documentation OpenAPI reflète les endpoints ajoutés ou modifiés.

## Governance

- Cette constitution prévaut sur toute autre pratique du projet. En cas de conflit, elle
  s'applique.
- Tout amendement MUST être documenté dans ce fichier (via `/speckit-constitution`), avec
  mise à jour de la version et de la date de dernier amendement.
- Versioning sémantique :
  - **MAJOR** : suppression ou redéfinition incompatible d'un principe ou d'une règle de gouvernance.
  - **MINOR** : ajout d'un principe ou d'une section, ou extension substantielle d'une règle.
  - **PATCH** : clarification, reformulation, correction sans impact sémantique.
- Chaque plan et chaque revue de code MUST vérifier la conformité à cette constitution ;
  tout écart MUST être justifié explicitement ou corrigé.

**Version**: 1.0.0 | **Ratified**: 2026-10-28 | **Last Amended**: 2026-10-28
