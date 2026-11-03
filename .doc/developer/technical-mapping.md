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
