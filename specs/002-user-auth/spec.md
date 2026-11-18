# Feature Specification: Gestion des utilisateurs (inscription, connexion, déconnexion)

**Feature Branch**: `feat/#3`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "Gestion des utilisateurs. Base de données : une table users qui
stockera les utilisateurs, une table claimValues qui stockera les claims, une table userClaims qui
fera la relation n:n avec users, une table logins qui stockera les tentatives de logins (id, userId,
dateLogin, nbrTries). Backend : les entités correspondantes aux tables, les configurations pour
décrire les relations, les API pour la connexion, la déconnexion, l'enregistrement d'un nouvel
utilisateur. Frontend : écran de login, écran d'enregistrement."

> Note : les choix techniques de la description (noms de tables, entités, configurations des
> relations, API) relèvent du plan (`/speckit-plan`) et y seront repris. La présente spec décrit
> uniquement le comportement attendu.

## Clarifications

### Session 2026-11-18

- Q: Quels droits sont attribués à un nouvel utilisateur ? → A: Un droit de base unique,
  `User`, attribué automatiquement à l'inscription.
- Q: Nombre d'échecs autorisés et durée du blocage ? → A: 5 échecs consécutifs, puis blocage
  de 15 minutes.
- Q (plan, D1) : Comment rendre la déconnexion effective ? → A: Session prolongeable de 7 jours,
  révoquée à la déconnexion ; un accès déjà accordé reste valable 15 minutes au plus (FR-014).
- Q (plan, D2) : Faut-il un historique des tentatives de connexion ? → A: Non ; un seul état par
  utilisateur (dernière tentative + échecs consécutifs) (FR-009).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Créer un compte (Priority: P1)

En tant que visiteur, j'ouvre l'écran d'inscription de l'application, je saisis mon nom, mon
prénom, mon adresse e-mail et un mot de passe, et j'obtiens un compte me permettant ensuite de
me connecter.

**Why this priority**: sans compte, aucune autre fonctionnalité personnelle (collections,
wishlist…) n'est possible ; c'est le point d'entrée de tout utilisateur.

**Independent Test**: depuis l'écran d'inscription, créer un compte avec des informations
valides, puis constater que le compte existe (une nouvelle inscription avec le même e-mail est
refusée).

**Acceptance Scenarios**:

1. **Given** un visiteur sur l'écran d'inscription, **When** il saisit un nom, un prénom, un
   e-mail non encore utilisé et un mot de passe conforme, puis valide, **Then** son compte est
   créé, un message de confirmation s'affiche et il est redirigé vers l'écran de connexion.
2. **Given** un compte existant avec l'e-mail `a@b.fr`, **When** un visiteur tente de s'inscrire
   avec `A@B.fr`, **Then** l'inscription est refusée avec le message « Cette adresse e-mail est
   déjà utilisée ».
3. **Given** un visiteur sur l'écran d'inscription, **When** il laisse un champ obligatoire vide,
   saisit un e-mail mal formé ou un mot de passe non conforme, **Then** l'inscription est refusée
   et chaque champ en erreur affiche un message explicite.
4. **Given** un visiteur sur l'écran d'inscription, **When** la confirmation du mot de passe ne
   correspond pas au mot de passe, **Then** l'inscription est refusée avant tout envoi.
5. **Given** un compte nouvellement créé, **When** on consulte ses droits, **Then** il possède
   le droit de base `User`, et uniquement celui-ci.

---

### User Story 2 - Se connecter (Priority: P1)

En tant qu'utilisateur inscrit, j'ouvre l'écran de connexion, je saisis mon e-mail et mon mot de
passe, et j'accède à l'application en étant identifié.

**Why this priority**: indissociable de l'inscription ; c'est ce qui rend le compte utile.

**Independent Test**: avec un compte existant, se connecter depuis l'écran de connexion et
constater l'accès à l'application ; avec un mauvais mot de passe, constater le refus.

**Acceptance Scenarios**:

1. **Given** un utilisateur inscrit, **When** il saisit son e-mail et son mot de passe corrects,
   **Then** il est connecté et redirigé vers l'écran d'accueil de l'application.
2. **Given** un utilisateur inscrit, **When** il saisit un mot de passe incorrect, **Then** la
   connexion est refusée avec le message générique « E-mail ou mot de passe incorrect ».
3. **Given** aucun compte avec l'e-mail saisi, **When** le visiteur tente de se connecter,
   **Then** il reçoit exactement le même message générique qu'en cas de mauvais mot de passe.
4. **Given** un utilisateur ayant échoué 5 fois de suite, **When** il tente de se connecter
   dans les 15 minutes qui suivent le 5e échec, même avec le bon mot de passe, **Then** la
   connexion est refusée avec un message indiquant que le compte est temporairement bloqué.
5. **Given** un utilisateur bloqué depuis plus de 15 minutes, **When** il se connecte avec le bon
   mot de passe, **Then** la connexion réussit.
6. **Given** un utilisateur ayant subi moins de 5 échecs consécutifs, **When** il
   se connecte avec succès, **Then** son compteur d'échecs consécutifs est remis à zéro.
7. **Given** un utilisateur connecté qui ferme puis rouvre l'application, **When** sa session est
   toujours valide, **Then** il reste connecté sans ressaisir ses identifiants.

---

### User Story 3 - Se déconnecter (Priority: P2)

En tant qu'utilisateur connecté, je me déconnecte pour que personne d'autre ne puisse utiliser
mon compte depuis cet appareil.

**Why this priority**: indispensable à la sécurité, mais n'apporte de valeur qu'une fois la
connexion en place.

**Independent Test**: se connecter, se déconnecter, puis constater que l'accès aux écrans
réservés redirige vers l'écran de connexion.

**Acceptance Scenarios**:

1. **Given** un utilisateur connecté, **When** il choisit « Se déconnecter », **Then** sa session
   est terminée et il est redirigé vers l'écran de connexion.
2. **Given** un utilisateur qui vient de se déconnecter, **When** une demande de prolongation de
   son ancienne session est faite, **Then** elle est refusée comme non authentifiée.
3. **Given** un utilisateur déconnecté depuis plus de 15 minutes, **When** une requête est faite
   avec son ancienne session, **Then** elle est refusée comme non authentifiée.

---

### User Story 4 - Accès protégé (Priority: P2)

En tant que visiteur non connecté, je n'ai accès qu'aux écrans de connexion, d'inscription et
de statut ; tout autre écran me redirige vers la connexion.

**Why this priority**: garantit que les futures données personnelles (collections) ne sont
accessibles qu'à leur propriétaire.

**Independent Test**: sans être connecté, ouvrir un écran réservé et constater la redirection
vers l'écran de connexion.

**Acceptance Scenarios**:

1. **Given** un visiteur non connecté, **When** il tente d'ouvrir un écran réservé, **Then** il
   est redirigé vers l'écran de connexion.
2. **Given** un visiteur non connecté, **When** il ouvre l'écran de statut, **Then** celui-ci
   reste accessible.
3. **Given** un utilisateur dont la session a expiré, **When** il effectue une action, **Then** il
   est redirigé vers l'écran de connexion avec le message « Votre session a expiré ».

---

### Edge Cases

- E-mail saisi avec des espaces autour ou des majuscules : il est normalisé (espaces retirés,
  comparaison insensible à la casse) à l'inscription comme à la connexion.
- Deux inscriptions simultanées avec le même e-mail : une seule réussit.
- Tentatives de connexion sur un e-mail inexistant : même message et même temps de réponse
  perçu qu'un mauvais mot de passe (ne pas révéler l'existence d'un compte).
- API injoignable pendant l'inscription ou la connexion : l'écran affiche un message clair et
  conserve les champs saisis (sauf le mot de passe).
- Double appui sur le bouton de validation : une seule demande est traitée.
- Expiration du blocage : passé la durée de blocage, l'utilisateur peut à nouveau se connecter.

## Requirements *(mandatory)*

### Functional Requirements

**Inscription**

- **FR-001**: Le système MUST permettre à un visiteur de créer un compte en fournissant nom,
  prénom, adresse e-mail et mot de passe (avec confirmation).
- **FR-002**: Le système MUST refuser une inscription dont l'e-mail est déjà associé à un compte,
  la comparaison étant insensible à la casse et aux espaces de début/fin.
- **FR-003**: Le système MUST valider le format de l'e-mail et exiger que nom et prénom soient
  renseignés (1 à 100 caractères chacun).
- **FR-004**: Le système MUST exiger un mot de passe d'au moins 8 caractères contenant au moins
  une lettre et un chiffre.
- **FR-005**: Le système MUST NOT stocker les mots de passe en clair ni sous une forme
  réversible ; un mot de passe MUST NOT jamais être renvoyé par le système.
- **FR-006**: Le système MUST attribuer automatiquement à chaque nouveau compte le droit de
  base `User`, et aucun autre.

**Connexion**

- **FR-007**: Le système MUST permettre à un utilisateur inscrit de se connecter avec son e-mail
  et son mot de passe.
- **FR-008**: En cas d'échec, le système MUST renvoyer un message générique identique, que l'e-mail
  existe ou non.
- **FR-009**: Pour chaque compte existant, le système MUST tenir à jour la date et l'heure de la
  dernière tentative de connexion et le nombre d'échecs consécutifs (aucun historique détaillé
  des tentatives n'est conservé).
- **FR-010**: Le système MUST bloquer la connexion d'un compte pendant 15 minutes après
  5 échecs consécutifs, puis la débloquer automatiquement ; pendant le blocage, toute tentative
  est refusée sans vérifier le mot de passe ni prolonger le blocage.
- **FR-011**: Une connexion réussie MUST remettre à zéro le compteur d'échecs consécutifs.
- **FR-012**: Une fois connecté, l'utilisateur MUST être identifié avec ses droits sur chaque
  requête, sans ressaisir ses identifiants, jusqu'à la fin de sa session.
- **FR-013**: Une session MUST expirer après 7 jours ; l'utilisateur doit alors se reconnecter.

**Déconnexion**

- **FR-014**: Le système MUST permettre à un utilisateur connecté de se déconnecter ; après
  déconnexion, la session MUST NOT plus pouvoir être prolongée, et tout accès avec cette session
  MUST être refusé au plus tard 15 minutes après la déconnexion.

**Écrans et accès**

- **FR-015**: L'application MUST proposer un écran de connexion et un écran d'inscription,
  accessibles sans être connecté, avec un lien de l'un vers l'autre.
- **FR-016**: L'application MUST rediriger vers l'écran de connexion tout visiteur non connecté
  qui tente d'accéder à un écran réservé ; l'écran de statut reste public.
- **FR-017**: Les écrans MUST afficher les erreurs de validation au niveau de chaque champ et
  les erreurs globales (identifiants incorrects, compte bloqué, API injoignable) de façon
  lisible sur mobile.

### Key Entities *(include if feature involves data)*

- **Utilisateur** : une personne inscrite. Attributs : identifiant, nom, prénom, adresse e-mail
  (unique), mot de passe protégé.
- **Droit (claim)** : une autorisation nommée pouvant être accordée aux utilisateurs.
  Attributs : identifiant, nom (unique), description.
- **Attribution de droit** : association entre un utilisateur et un droit. Un utilisateur peut
  avoir plusieurs droits, un droit peut être attribué à plusieurs utilisateurs (n:n).
- **Tentative de connexion** : état de connexion d'un utilisateur (une seule par utilisateur).
  Attributs : identifiant, utilisateur concerné, date/heure de la dernière tentative, nombre
  d'échecs consécutifs.
- **Session** : une connexion active sur un appareil, valable 7 jours et révocable par la
  déconnexion. Un utilisateur peut avoir plusieurs sessions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un visiteur crée son compte en moins de 2 minutes depuis l'ouverture de l'écran
  d'inscription.
- **SC-002**: Un utilisateur inscrit se connecte en moins de 30 secondes.
- **SC-003**: 100 % des tentatives sur un compte bloqué sont refusées pendant la durée du blocage.
- **SC-004**: Aucun mot de passe n'est lisible en clair, que ce soit dans les données stockées,
  les journaux ou les réponses du système.
- **SC-005**: Les messages d'échec de connexion ne permettent pas de déterminer si une adresse
  e-mail possède un compte.
- **SC-006**: 100 % des écrans réservés sont inaccessibles sans connexion.

## Assumptions

- L'identifiant de connexion est l'adresse e-mail ; il n'y a pas de nom d'utilisateur distinct.
- Aucune confirmation de l'adresse e-mail n'est demandée à l'inscription dans cette version.
- La gestion des droits (création, attribution, retrait) par un administrateur est hors
  périmètre ; le droit `User` (et `LogRead`, déjà référencé par le code existant) est
  initialisé avec les données de référence du système.
- Après la fin d'un blocage, le compteur d'échecs repart de zéro.
- Hors périmètre : mot de passe oublié, modification du profil, suppression de compte,
  connexion via un fournisseur externe (Google, Apple…), authentification à deux facteurs.
- Un utilisateur peut avoir plusieurs sessions simultanées (plusieurs appareils) ; la
  déconnexion ne termine que la session de l'appareil concerné.
- Les tentatives de connexion portant sur un e-mail inexistant ne sont pas enregistrées
  (aucun utilisateur à rattacher).
- L'écran de statut créé par la feature 001 reste accessible sans connexion.
