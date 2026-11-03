# Démonstrations Njord

Ce répertoire rassemble de petits programmes console exécutables qui montrent comment consommer les modules présents dans la solution. Ils servent de points de départ concrets pour les développeurs et permettent de lancer les exemples localement.

Les démos sont volontairement limitées au périmètre réellement implémenté. Elles ne constituent pas une GUI ou un dashboard ; ces capacités sont toujours dans la vision produit. Les démos Core et Models utilisent des données intégrées et ne nécessitent aucun service externe. La démo Massive requiert une clé API valide et un accès réseau ; Yahoo Finance requiert un accès réseau et des symboles/historiques disponibles, mais aucun credential.

## Projets disponibles

| Projet | Module illustré | Exécution |
|---|---|---|
| `Njord.Demo.Serialization` | Sérialisation JSON, enums texte et désérialisation de dates | `dotnet run --project demo/Core/Njord.Demo.Serialization` |
| `Njord.Demo.Extensions` | Vérifications d'énumérables et `None` | `dotnet run --project demo/Core/Njord.Demo.Extensions` |
| `Njord.Demo.Retry` | Retry configurable linéaire ou exponentiel, annulation et `Retry-After` | `dotnet run --project demo/Core/Njord.Demo.Retry` |
| `Njord.Demo.Models` | Création et affichage de modèles d'instrument et de marché | `dotnet run --project demo/Models/Njord.Demo.Models` |
| `Njord.Demo.Massive` | Récupération vectorielle de barres OHLC historiques via Massive et le référentiel Njord | `dotnet run --project demo/Data/Njord.Demo.Massive` |
| `Njord.Demo.YahooFinance` | Récupération vectorielle de barres OHLC historiques pour six classes d'actifs via Yahoo Finance et le référentiel Njord | `dotnet run --project demo/Data/Njord.Demo.YahooFinance` |

Chaque projet est autonome et peut être lancé depuis la racine du dépôt. Pour l'explorer dans un IDE, ouvrir son `.csproj` ou la solution `Njord.slnx`.

## Démonstration Massive en conditions réelles

1. La démo utilise directement [massive.settings.json](../src/Data/Njord.DataProviders.Massive/massive.settings.json), qui est copié dans son répertoire de sortie. Renseigner `DataProviders:Massive:ApiKey` dans ce fichier si une clé gratuite est disponible ; la démo ne le surcharge pas avec une configuration locale.
2. Copier `referential.settings.example.json` dans `demo/Data/Njord.Demo.Massive/referential.settings.local.json`. Ce fichier configure uniquement le chemin de la base JSON du référentiel et n'est pas fusionné avec les settings Massive.
3. La base JSON est la source de vérité locale. Elle doit contenir une correspondance pour chaque ticker demandé, le fournisseur `Massive` et toute la plage de dates ; les mappings peuvent être ajoutés via `IInstrumentReferential` ou directement dans le tableau JSON. Le fichier est créé au premier ajout et n'est pas une archive versionnée.
4. Lancer la commande ci-dessus. Les arguments facultatifs sont une liste de tickers Njord séparés par des virgules, la date de début et la date de fin :

```text
dotnet run --project demo/Data/Njord.Demo.Massive -- AAPL,SPX 2025-01-01 2025-01-10
```

Sans arguments, la démo utilise `AAPL` et une plage récente de dix jours calendaires. L'accès aux données et à l'historique dépend du plan et des droits du compte Massive. La réponse est affichée sous forme de barres OHLC portant les tickers canoniques Njord ; aucune devise n'est inférée. Voir [le guide du référentiel](../src/Data/Njord.Data.Referential/README.md) pour le modèle, les périodes inclusives et le format de la base JSON.

## Démonstration Yahoo Finance en conditions réelles

La démo envoie un seul appel vectoriel à `IYahooFinanceHistoricalMarketDataProvider` pour six tickers canoniques — `AAPL` (Stock), `SPX` (Index), `AAPL=2025-01-17C250000` (Option), `ES-CONTINUOUS` (Future), `BTC-USD` (Crypto) et `EURUSD` (Forex). Elle affiche les correspondances valides chargées du référentiel (ticker Njord et symbole Yahoo séparés), les barres OHLC reçues et les diagnostics par ticker. Le fournisseur répartit techniquement le lot par ticker ; un seul appel vectoriel ne signifie pas une requête HTTP unique ni un traitement parallèle.

1. Depuis la racine du dépôt, préparer la configuration locale du référentiel et sa base primaire. Avec PowerShell :

```powershell
Copy-Item demo/Data/Njord.Demo.YahooFinance/referential.settings.example.json demo/Data/Njord.Demo.YahooFinance/referential.settings.local.json
New-Item -ItemType Directory -Force data/referential | Out-Null
Copy-Item demo/Data/Njord.Demo.YahooFinance/yahoo-finance-instruments.example.json data/referential/yahoo-finance-instruments.json
```

Le fichier local configure uniquement le chemin de la base. La démo résout tout chemin relatif depuis la racine de la solution, quel que soit le répertoire courant au lancement. Le tableau JSON fournit une correspondance `YahooFinance` explicitement classée pour chaque ticker de la démo ; les mappings sont à vérifier et peuvent être adaptés. Le dossier `/data/referential/` et les fichiers `referential.settings.local.json` sont ignorés par Git.
2. Lancer depuis la racine du dépôt ou le répertoire du projet :

```text
dotnet run --project demo/Data/Njord.Demo.YahooFinance
```

Sans arguments, la période inclusive est du 2 au 10 janvier 2025, en barres journalières. Deux arguments facultatifs permettent de choisir `date-debut date-fin` au format `yyyy-MM-dd`; chaque mapping doit couvrir toute la période. Par exemple :

```text
dotnet run --project demo/Data/Njord.Demo.YahooFinance -- 2025-01-02 2025-01-10
```

Le projet copie directement `yahoo-finance.settings.json` depuis le projet source Yahoo Finance et ne charge aucun override local des settings du fournisseur. Yahoo Finance ne nécessite pas de clé API dans cet adaptateur, mais il faut un accès réseau ; les symboles, plages d'historique, restrictions du service et limites propres aux options peuvent empêcher le retour de données. Le contrat documente en particulier que l'historique des options n'est pas fiable : l'absence de barres et les diagnostics sont des résultats possibles, pas un échec masqué du lot. `ES=F` est un symbole Yahoo de série continue, pas l'identifiant universel d'un contrat future. La démo n'effectue ni enrichissement ni pricing et ne valide pas les chiffres ou la qualité financière des données.

Voir [le guide du référentiel](../src/Data/Njord.Data.Referential/README.md) et [la documentation Yahoo Finance](../src/Data/Njord.DataProviders.YahooFinance/README.md) pour le schéma des mappings, les dates inclusives et les limites du connecteur.

## Ajouter une démo

Créer un projet console .NET 10 dans le sous-répertoire thématique approprié, référencer seulement le module concerné, ajouter le projet à `Njord.slnx`, puis documenter son objectif, ses prérequis et sa commande ici. Montrer un parcours utilisable plutôt qu'un duplicata de tests unitaires ; isoler les secrets et dépendances externes dans la configuration locale.
