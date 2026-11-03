# Mapping technique

## Contrat commun

Le projet `Njord.Core.Mapping` fournit `IMapper<TSource, TTarget>`, un contrat technique indépendant du domaine et des fournisseurs. Il expose `TTarget Map(TSource source)`; le contrat n'embarque aucune règle métier et ne dépend d'aucun autre projet.

Lorsqu'une transformation dépend de plusieurs valeurs, son adaptateur déclare un type d'entrée explicite plutôt que d'ajouter des arguments spécifiques au fournisseur au contrat partagé.

## Utilisation dans les fournisseurs de données

Les fournisseurs Massive et Yahoo Finance enregistrent leurs mappers via le contrat générique :

- Massive mappe les pages d'agrégats et la requête résolue vers les barres OHLC.
- Yahoo Finance mappe la réponse chart et la requête résolue vers des barres OHLC et diagnostics.
- Yahoo Finance convertit aussi la granularité demandée en intervalle API et en intervalle rapporté.

Les DTO externes, la validation des réponses, les règles de conversion et les diagnostics restent dans le projet de chaque fournisseur. Le contrat commun standardise l'invocation et l'injection, pas les règles métier ni les schémas externes.

## Ajouter un mapper

1. Réutiliser `IMapper<TSource, TTarget>` lorsqu'il s'agit d'une transformation synchrone.
2. Garder les types source et destination propres à leur adaptateur si la transformation est spécifique à une API.
3. Créer un type source dédié lorsque la transformation a besoin de plusieurs valeurs.
4. Enregistrer le mapper via `IMapper<TSource, TTarget>` dans le conteneur DI du projet propriétaire.
5. Tester les conversions, les entrées invalides et les erreurs propres au contrat de l'adaptateur.

Ne pas utiliser ce contrat pour abstraire des comportements asynchrones, des workflows ou des règles qui ont des responsabilités différentes.

## Contrats de fournisseurs de données marché

`Njord.Data.MarketData` expose le marqueur métier `IMarketData`, les contrats
génériques `IMarketDataRequest<TMarketData>`,
`IMarketDataResult<TMarketData>` et
`IMarketDataProvider<TSupportedMarketData>`, ainsi que l'interface OHLC
historique non générique conservée pour compatibilité. Chaque requête associe
statiquement le type demandé ; les résultats génériques exposent le ticker,
les données et les diagnostics communs. Les résultats OHLC et corporate action
conservent leurs propriétés spécialisées et publient `Data` comme alias de leur
snapshot.

Massive et Yahoo Finance enregistrent `IMarketDataProvider<IMarketData>` et
dispatchent les demandes OHLC et corporate action vers leurs chemins communs
existants, préservant les résultats et diagnostics par ticker. Toute
combinaison requête/type inconnue est explicitement rejetée. Les deux adaptateurs
continuent aussi d'enregistrer `IMarketDataProvider` pour compatibilité.
`MarketDataProviderSelector` sélectionne via cette interface historique ;
l'application reste responsable de l'enregistrement et du choix du provider.

La capacité facultative `ICorporateActionProvider` représente les événements
source sans calculer de facteur d'ajustement. Les deux adaptateurs exposent
leur couverture limitée par des diagnostics. La comparaison OHLC exige une
devise connue et identique, un fuseau valide, une base de prix explicitement
non ajustée et des séances appariées. Les métadonnées actuellement
indisponibles ou non prouvées pour les providers rendent donc la comparaison
réelle non concluante ; il ne faut pas en déduire une validation financière.
