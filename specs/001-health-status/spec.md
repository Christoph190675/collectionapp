# Feature Specification: Statut de santé (walking skeleton)

**Feature Branch**: `feat/#2`

**Created**: 2026-10-28

**Status**: Validated (2026-10-28)

**Input**: User description: "Walking skeleton : en tant que développeur, je veux qu'un onglet
"Statut" de l'application mobile affiche l'état de santé de l'API et de la base de données, afin de
vérifier que toute la chaîne front -> API -> base fonctionne."

## Clarifications

### Session 2026-10-28

- Q: Quand la base de données est arrêtée, quel code HTTP le point de vérification de santé
  doit-il renvoyer ? → A: 200 si tout est OK, 503 si la base est KO ; le contenu détaille
  l'état de la base dans les deux cas et le front le lit même sur un 503.
- Q: Quand la base est KO, la réponse de santé doit-elle inclure le détail technique de
  l'erreur ? → A: Non ; la réponse se limite aux états OK/KO, le détail est uniquement
  journalisé côté API.
- Q: La base doit-elle être « OK » dès qu'elle répond, ou seulement si toutes les migrations
  sont appliquées ? → A: OK seulement si la connexion aboutit et qu'aucune migration n'est en
  attente ; sinon KO.
- Q: Comment les migrations doivent-elles être appliquées : au démarrage de l'API ou par une
  commande explicite ? → A: Commande explicite lancée par le développeur ; l'API ne modifie
  jamais le schéma elle-même.
- Q: Le walking skeleton doit-il fonctionner seulement dans le navigateur ou aussi dans
  l'application installée (émulateur/téléphone) ? → A: Les deux ; liste d'origines autorisées
  configurable par environnement.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Vérifier que toute la chaîne fonctionne (Priority: P1)

En tant que développeur, j'ouvre l'onglet « Statut » de l'application mobile et je vois
immédiatement que l'API et la base de données sont opérationnelles.

**Why this priority**: c'est l'objectif du walking skeleton : prouver de bout en bout que
l'application, l'API et la base communiquent. Sans ce parcours, rien d'autre n'a de valeur.

**Independent Test**: démarrer la base, l'API et l'application, ouvrir l'onglet « Statut » et
constater l'affichage API = OK et Base de données = OK.

**Acceptance Scenarios**:

1. **Given** l'API et la base de données sont démarrées et la base est initialisée,
   **When** le développeur ouvre l'onglet « Statut »,
   **Then** l'écran affiche « API = OK » et « Base de données = OK ».
2. **Given** l'application est lancée,
   **When** le développeur consulte la barre de navigation,
   **Then** un onglet « Statut » est présent et accessible.

---

### User Story 2 - Diagnostiquer une panne (Priority: P2)

En tant que développeur, quand un maillon de la chaîne est en panne, l'onglet « Statut » m'indique
lequel, sans que l'application plante.

**Why this priority**: le statut n'est utile que s'il distingue clairement les cas de panne.

**Independent Test**: arrêter la base (puis l'API) et vérifier l'affichage de l'onglet « Statut ».

**Acceptance Scenarios**:

1. **Given** l'API est démarrée mais la base de données est arrêtée,
   **When** le développeur ouvre l'onglet « Statut »,
   **Then** l'API répond et l'écran affiche « API = OK » et « Base de données = KO ».
2. **Given** l'API est injoignable,
   **When** le développeur ouvre l'onglet « Statut »,
   **Then** l'écran indique clairement que l'API est injoignable (« API = KO »), l'état de la base
   est affiché comme inconnu, et l'application reste utilisable (aucun plantage, aucun écran blanc).

---

### User Story 3 - Relancer la vérification (Priority: P2)

En tant que développeur, après avoir redémarré un service, je relance la vérification depuis
l'onglet « Statut » sans redémarrer l'application.

**Why this priority**: permet de valider une correction rapidement ; dépend de la story 1.

**Independent Test**: depuis un état KO, redémarrer le service concerné puis déclencher la
relance et observer le passage à OK.

**Acceptance Scenarios**:

1. **Given** l'onglet « Statut » affiche « Base de données = KO »,
   **When** la base est redémarrée et le développeur relance la vérification,
   **Then** l'écran affiche « Base de données = OK ».
2. **Given** une vérification est en cours,
   **When** le développeur regarde l'écran,
   **Then** un indicateur de chargement est visible et une nouvelle relance ne peut pas être
   déclenchée tant que la vérification en cours n'est pas terminée.

---

### User Story 4 - Restreindre l'accès à l'API aux origines du front (Priority: P3)

En tant que développeur, je veux que seules les origines configurées de l'application front
(version navigateur et application installée sur émulateur ou téléphone) puissent appeler l'API.

**Why this priority**: pose la règle de sécurité dès le squelette, sans impact sur le parcours
principal.

**Independent Test**: appeler l'API depuis chacune des origines configurées (autorisées) puis
depuis une autre origine (refusée).

**Acceptance Scenarios**:

1. **Given** les origines du front sont configurées côté API,
   **When** une requête provient de l'une de ces origines (navigateur ou application installée),
   **Then** la réponse autorise l'origine et la réponse est acceptée.
2. **Given** les origines du front sont configurées côté API,
   **When** une requête provient d'une origine absente de la liste,
   **Then** la réponse n'autorise pas cette origine et le navigateur bloque la réponse.

---

### User Story 5 - Initialiser la base par une première migration (Priority: P3)

En tant que développeur, je veux que le schéma de la base soit créé par une première migration
versionnée, afin que toute base vierge puisse être initialisée de façon reproductible.

**Why this priority**: prérequis technique des futures features, vérifiable indépendamment.

**Independent Test**: sur une base vierge, appliquer les migrations et constater que le schéma
est créé et que la migration initiale est enregistrée comme appliquée.

**Acceptance Scenarios**:

1. **Given** une base de données vierge et joignable,
   **When** la vérification de santé est lancée avant toute migration,
   **Then** l'écran affiche « Base de données = KO ».
2. **Given** une base de données vierge,
   **When** les migrations sont appliquées,
   **Then** la migration initiale est enregistrée comme appliquée et la base est jugée « OK » par
   la vérification de santé.
3. **Given** une base où la migration initiale est déjà appliquée,
   **When** les migrations sont appliquées à nouveau,
   **Then** aucune modification n'est faite et aucune erreur n'est levée.

---

### Edge Cases

- L'API répond très lentement : au-delà d'un délai maximal de 5 secondes, l'écran affiche
  « API = KO » (délai dépassé) au lieu d'attendre indéfiniment.
- La base est joignable mais très lente : la vérification de la base est bornée dans le temps
  et l'API répond « Base de données = KO » sans dépasser le délai côté front.
- L'API renvoie une réponse inattendue (erreur serveur autre qu'un 503 de santé valide, format
  illisible) : l'écran affiche
  « API = KO » avec un message d'erreur lisible, sans planter.
- La base est joignable mais une migration n'est pas appliquée (base vierge ou schéma en
  retard) : l'API répond « Base de données = KO » ; après application des migrations et
  relance, l'état passe à « OK ».
- Le développeur quitte l'onglet pendant une vérification : aucune erreur n'est produite.
- Le développeur relance plusieurs fois rapidement : une seule vérification est en cours à la fois.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: L'application mobile MUST proposer un onglet « Statut » dans sa navigation principale.
- **FR-002**: À l'ouverture de l'onglet, l'application MUST lancer automatiquement une vérification
  de santé.
- **FR-003**: L'API MUST exposer un point de vérification de santé, accessible sans
  authentification, qui renvoie l'état de l'API et l'état de la base de données.
- **FR-004**: L'état de la base de données MUST refléter une connexion effective à la base au
  moment de la vérification (pas une valeur mise en cache). La base est « OK » si et seulement
  si la connexion aboutit ET qu'aucune migration connue de l'API n'est en attente d'application ;
  dans tous les autres cas elle est « KO » (avec code 503, cf. FR-005).
- **FR-005**: Lorsque la base est indisponible, l'API MUST quand même répondre, en indiquant
  « API = OK » et « Base de données = KO ». Le point de vérification renvoie le code 200 quand
  tout est OK et le code 503 (service indisponible) quand la base est KO ; dans les deux cas le
  contenu de la réponse détaille l'état de l'API et de la base.
- **FR-005a**: Le front MUST interpréter une réponse 503 dont le contenu est valide comme
  « API = OK » et lire l'état de la base dans ce contenu ; un 503 n'est jamais traité comme
  une API injoignable.
- **FR-005b**: La réponse de santé MUST se limiter aux états (OK/KO) et à l'horodatage ; elle
  MUST NOT exposer de détail technique (message d'erreur, hôte, nom de base, version). En cas de
  base KO, la cause de l'échec MUST être journalisée côté API.
- **FR-006**: L'écran MUST afficher distinctement l'état de l'API et celui de la base, chacun avec
  la valeur « OK » ou « KO » (ou « inconnu » pour la base quand l'API est injoignable).
- **FR-007**: Lorsque l'API est injoignable, ne répond pas dans un délai de 5 secondes, ou renvoie
  une réponse invalide, l'écran MUST indiquer « API = KO » avec un message explicite, sans plantage.
- **FR-008**: L'utilisateur MUST pouvoir relancer la vérification manuellement depuis l'onglet.
- **FR-009**: Pendant une vérification, l'écran MUST afficher un indicateur de chargement et
  empêcher le déclenchement d'une vérification concurrente.
- **FR-010**: L'écran MUST afficher l'heure de la dernière vérification.
- **FR-011**: L'API MUST n'autoriser les appels cross-origin que depuis les origines du front
  listées dans sa configuration (version navigateur et application installée) ; toute autre
  origine MUST être refusée.
- **FR-012**: La liste des origines autorisées MUST être configurable par environnement, sans
  modification du code.
- **FR-013**: Le schéma de la base MUST être initialisé par une première migration versionnée,
  dont l'application est idempotente.
- **FR-014**: Les migrations MUST être appliquées par une commande explicite lancée par le
  développeur ; l'API MUST NOT appliquer de migration ni modifier le schéma, y compris à son
  démarrage.
- **FR-015**: L'onglet « Statut » MUST fonctionner à l'identique dans le navigateur et dans
  l'application installée sur émulateur ou téléphone ; l'adresse de l'API utilisée par le front
  MUST être configurable par environnement de build.

### Key Entities

- **Statut de santé**: résultat d'une vérification ; contient l'état de l'API (OK/KO), l'état de
  la base de données (OK/KO/inconnu) et l'horodatage de la vérification. Non persisté.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Quand tout fonctionne, le développeur voit « API = OK » et « Base de données = OK »
  en moins de 2 secondes après l'ouverture de l'onglet.
- **SC-002**: Dans 100 % des scénarios de panne testés (base arrêtée, API arrêtée, API lente,
  réponse invalide), l'écran affiche un état explicite en moins de 6 secondes et l'application
  reste utilisable.
- **SC-003**: Après redémarrage d'un service, une seule action de relance suffit pour voir l'état
  repasser à OK.
- **SC-004**: 100 % des appels provenant d'une origine non configurée sont refusés, et 100 % des
  origines configurées (navigateur, Android, iOS) sont acceptées.
- **SC-005**: Une base vierge est initialisée par une seule commande de migration explicite,
  sans intervention manuelle sur le schéma ; démarrer l'API seule ne modifie jamais la base.
- **SC-006**: Chacun des critères d'acceptation ci-dessus est couvert par au moins un test
  automatisé.

## Assumptions

- Utilisateurs : uniquement des développeurs ; l'onglet n'a pas besoin d'être masqué ou protégé
  dans le cadre de ce squelette.
- Pas d'authentification sur l'application ni sur le point de vérification de santé à ce stade.
- La première migration ne contient que le strict nécessaire (aucune table métier) ; les tables
  métier viendront avec les features de gestion de collections.
- L'environnement de développement local (base dans un conteneur, API et front en local) est la
  cible, y compris l'application installée sur un émulateur ou un téléphone du réseau local ;
  le déploiement en production et la publication sur les stores sont hors périmètre.
- Pour l'usage sur téléphone physique, l'API est exposée sur le réseau local du poste de
  développement ; la mise en place de ce réseau n'est pas couverte par des tests automatisés.
- Pas de rafraîchissement automatique périodique : la vérification se fait à l'ouverture de
  l'onglet et sur relance manuelle.
- Les libellés de l'interface sont en français.
