# Démonstrations Njord

Ce répertoire rassemble de petits programmes console exécutables qui montrent comment consommer les modules présents dans la solution. Ils servent de points de départ concrets pour les développeurs et permettent de lancer les exemples localement.

Les démos sont volontairement limitées au périmètre réellement implémenté. Elles ne constituent pas une GUI ou un dashboard ; ces capacités sont toujours dans la vision produit. Les démos Core et Models utilisent des données intégrées et ne nécessitent aucun service externe. La démo Massive effectue une requête réelle au fournisseur et requiert une clé API valide ainsi qu'un accès réseau.

## Projets disponibles

| Projet | Module illustré | Exécution |
|---|---|---|
| `Njord.Demo.Serialization` | Sérialisation JSON, enums texte et désérialisation de dates | `dotnet run --project demo/Core/Njord.Demo.Serialization` |
| `Njord.Demo.Extensions` | Vérifications d'énumérables et `None` | `dotnet run --project demo/Core/Njord.Demo.Extensions` |
| `Njord.Demo.Retry` | Retry configurable linéaire ou exponentiel, annulation et `Retry-After` | `dotnet run --project demo/Core/Njord.Demo.Retry` |
| `Njord.Demo.Models` | Création et affichage de modèles d'instrument et de marché | `dotnet run --project demo/Models/Njord.Demo.Models` |
| `Njord.Demo.Massive` | Récupération réelle de barres OHLC historiques avec Massive | `dotnet run --project demo/Data/Njord.Demo.Massive` |

Chaque projet est autonome et peut être lancé depuis la racine du dépôt. Pour l'explorer dans un IDE, ouvrir son `.csproj` ou la solution `Njord.slnx`.

## Démonstration Massive en conditions réelles

1. Copier `demo/Data/Njord.Demo.Massive/massive.settings.example.json` sous le nom `massive.settings.local.json` dans le même répertoire.
2. Renseigner `DataProviders:Massive:ApiKey` dans ce fichier local. Ne pas committer la clé ; ce nom de fichier est exclu par `.gitignore`.
3. Lancer la commande ci-dessus. Les arguments facultatifs sont le ticker, la date de début et la date de fin, dans cet ordre :

```text
dotnet run --project demo/Data/Njord.Demo.Massive -- AAPL 2025-01-01 2025-01-10
```

Sans arguments, la démo utilise `AAPL` et une plage récente de dix jours calendaires. L'accès aux données et à l'historique dépend du plan et des droits du compte Massive. La réponse est affichée sous forme de barres OHLC ; aucune devise n'est inférée.

## Ajouter une démo

Créer un projet console .NET 10 dans le sous-répertoire thématique approprié, référencer seulement le module concerné, ajouter le projet à `Njord.slnx`, puis documenter son objectif, ses prérequis et sa commande ici. Montrer un parcours utilisable plutôt qu'un duplicata de tests unitaires ; isoler les secrets et dépendances externes dans la configuration locale.
