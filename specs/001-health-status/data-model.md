# Data Model: Statut de santé (walking skeleton)

**Feature**: [spec.md](./spec.md) | **Date**: 2026-10-28

Cette feature ne persiste aucune donnée métier. Le DbContext `CollectionAppDbContext` ne contient
aucun `DbSet` ; la migration `InitialCreate` ne crée que la table technique `__EFMigrationsHistory`.

## HealthStatus (non persisté)

Résultat d'une vérification, produit par l'API et affiché par le front.

| Champ | Type | Valeurs | Règle |
|-------|------|---------|-------|
| `api` | string | `"ok"` | Toujours `"ok"` quand l'API répond (FR-005) |
| `database` | string | `"ok"` \| `"ko"` | `"ok"` si et seulement si la connexion aboutit en moins de 2 s ET qu'aucune migration n'est en attente (FR-004) |
| `checkedAt` | string (date-time ISO 8601, UTC, suffixe `Z`) | — | Instant de la vérification côté API |

Code HTTP associé : `200` si `database = "ok"`, `503` si `database = "ko"` (FR-005).
Aucun autre champ n'est exposé (FR-005b).

## HealthCheckResult (front uniquement, non persisté)

Modèle de vue construit par `HealthService` à partir de la réponse ou de l'échec d'appel.

| Champ | Type | Valeurs | Règle |
|-------|------|---------|-------|
| `api` | `'ok' \| 'ko'` | — | `'ok'` si réponse 200 ou 503 avec corps conforme ; `'ko'` sinon (FR-005a, FR-007) |
| `database` | `'ok' \| 'ko' \| 'unknown'` | — | Lu dans le corps ; `'unknown'` quand `api = 'ko'` |
| `checkedAt` | `Date` | — | `checkedAt` de l'API si disponible, sinon heure locale de la tentative (FR-010) |
| `error` | `'unreachable' \| 'timeout' \| 'invalid-response' \| undefined` | — | Renseigné seulement quand `api = 'ko'` ; détermine le message affiché |

## Transitions d'état de l'écran Statut

```text
[idle] --ouverture onglet / relance--> [loading] --réponse--> [result]
[result] --relance--> [loading]          (relance ignorée pendant [loading], FR-009)
[loading] --sortie de l'onglet--> requête annulée, aucune erreur
```

Correspondance réponse → affichage :

| Cas | `api` | `database` | `error` |
|-----|-------|-----------|---------|
| 200, corps valide | ok | ok | — |
| 503, corps valide | ok | ko | — |
| Erreur réseau (statut 0) | ko | unknown | unreachable |
| Aucune réponse en 5 s | ko | unknown | timeout |
| Autre statut, ou corps non conforme | ko | unknown | invalid-response |
