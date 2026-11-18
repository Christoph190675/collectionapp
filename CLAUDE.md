# CollectionApp

Application mobile de gestion de collections (TCG, vinyles, jeux vidéo…), développée en
Spec-Driven Development avec Spec Kit.

## Règles

- Lire `.specify/memory/constitution.md` et la spec active (`.specify/feature.json` → `specs/<feature>/`)
  avant toute implémentation. Aucun code sans spec validée ; un changement de comportement amende
  d'abord la spec.
- Chaque critère d'acceptation a au moins un test nommé d'après lui : `US2_AC4_...` côté back,
  `it('US2-AC4 : ...')` côté front.
- Ne jamais modifier une migration existante : toute évolution du schéma passe par une nouvelle migration.
- Aucun secret dans un fichier versionné (`appsettings.json` garde `Jwt:SigningKey` vide).
- Messages de commit : `ft(#<n° de feature>) : <résumé>`.

## Stack

- Back : .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core + `MySql.EntityFrameworkCore`.
  Clean Architecture : `backend/src/CollectionApp.{Api,Application,Domain,Infrastructure}`.
- Front : Ionic + Angular (TypeScript strict), Capacitor, Vitest. Appels HTTP uniquement dans
  `frontend/src/app/services/`.
- Base : MySQL dans un conteneur Docker géré hors du dépôt.

## Secrets requis (dev, `dotnet user-secrets` sur `backend/src/CollectionApp.Api`)

- `ConnectionStrings:CollectionApp`
- `Jwt:SigningKey` (au moins 32 octets aléatoires, voir `specs/002-user-auth/quickstart.md`)

## Commandes (depuis la racine)

```powershell
dotnet build backend/CollectionApp.slnx
dotnet test backend/CollectionApp.slnx            # Docker requis (Testcontainers MySQL)
dotnet run --project backend/src/CollectionApp.Api # http://localhost:8080, Swagger sur /swagger
dotnet tool restore --tool-manifest backend/dotnet-tools.json
dotnet ef migrations add <Nom> -p backend/src/CollectionApp.Infrastructure -s backend/src/CollectionApp.Api -o Persistence/Migrations
dotnet ef database update -p backend/src/CollectionApp.Infrastructure -s backend/src/CollectionApp.Api
cd frontend; npm test -- --watch=false             # tests Vitest
cd frontend; npm start                             # http://localhost:8100
```

## Pièges connus

- L'API n'applique jamais les migrations elle-même : `dotnet ef database update` est explicite.
- Les tests d'intégration utilisent `ApiFactory` (environnement `Testing`, clients HTTPS,
  `FakeTimeProvider` exposé en `Clock`, limitation de débit désactivée sauf `rateLimiting: true`).
- Corps d'erreur de l'API : `{ id, code, message, status, fields? }` (`HttpExceptionBodyError`).
