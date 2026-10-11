---
description: "Implémenter le stockage SQL Server/EF Core, l'archivage JSON ou le cache Redis"
name: "Persistance et cache"
argument-hint: "Indique le stockage ou le comportement de cache à implémenter"
agent: "agent"
---

# Tâche : persistance et cache

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis inspecte les projets, modèles, migrations et tests existants. Traite uniquement le stockage concerné par la demande.

Pour créer ou modifier les fichiers de configuration fonctionnels, suis également [le prompt de configuration spécialisée](./feature-configuration.prompt.md).

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : garde le changement aussi local que possible, suis les contrats consommateurs et les effets sur les fonctionnalités, et demande confirmation avant de modifier l'architecture de persistance ou un schéma/contrat partagé avec un impact global non explicitement demandé. Après accord, adapte tous les usages directement affectés dans le périmètre accepté.

Garde les projets en .NET 10 (`net10.0` ou le TFM réellement requis). Pour EF Core, Redis et les autres packages NuGet touchés, vise la dernière version stable compatible ; mets à jour les dépendances liées si nécessaire, et si la dernière version reste impraticable, utilise la plus récente compatible en expliquant la contrainte.

Dans l'ordre de développement indicatif, ce socle précède les workflows d'ingestion ou de backtest qui en dépendent. Sépare les projets par responsabilité (par exemple persistance SQL Server, archive JSON, cache Redis) dans le répertoire thématique `Data` ou son équivalent existant ; ne crée pas un projet partagé générique qui mélange ces responsabilités.

## Résultat métier

Fournir une persistance cohérente des instruments, métadonnées et backtests via SQL Server/EF Core, un archivage de données historiques en fichiers JSON et/ou un cache Redis local pour séries temporelles et résultats chauds.

## Consignes d'implémentation

- Conserve les règles métier indépendantes d'EF Core, de SQL Server et de Redis. Implémente les interfaces de persistance/cache dans les adaptateurs techniques existants.
- Réutilise le contexte EF, les conventions d'entités, l'approche de migration et les packages déjà présents. N'ajoute pas une seconde configuration parallèle.
- Si le schéma change, ajoute une migration versionnée et vérifie qu'elle est cohérente avec les entités et les configurations EF.
- Conçois les clés et index d'après les requêtes réelles. Préserve les identifiants, unités, devises, timestamps et provenance nécessaires à la cohérence des données financières.
- Les écritures de runs de backtest et leurs événements doivent avoir des garanties explicites d'intégrité, d'ordre et de reprise ; ne rends pas l'opération partiellement réussie sans le signaler.
- Pour l'archive JSON, définis la forme sérialisée de manière stable et documente les règles de lecture/écriture. N'écrase pas de fichiers existants sans stratégie sûre.
- Fournis une capacité de lecture des archives JSON persistées pour que l'ingestion puisse recharger ces données dans son workflow. Le contrat de persistance définit la sérialisation, les versions de schéma, l'accès au stockage et les erreurs de lecture ; le prompt d'ingestion définit comment les données rechargées sont utilisées dans les étapes métier.
- Pour Redis, rends le TTL configurable selon les patterns existants. Définis clairement le format de clé, la sérialisation, les collisions et les événements d'invalidation.
- N'utilise pas le cache comme source de vérité. Explicite le comportement si Redis est indisponible : ne bascule vers le stockage primaire que si le contrat existant ou la demande le prévoit, sans masquer l'incident.
- Ne journalise pas de chaînes de connexion ou d'identifiants. Utilise les options de configuration et secrets déjà disponibles.
- Si cette couche stocke des logs, événements d'audit ou traces pour les vues d'observabilité, prévois accès par run/période/fonctionnalité, pagination, index utiles, rétention/rotation et limites de volume. Ne conserve pas des payloads complets de données de marché lorsqu'un résumé ou une référence suffit.
- Distingue les exigences de durabilité des événements d'audit obligatoires de la rétention des diagnostics techniques ; documente la politique de purge et le comportement à saturation.
- Mesure la latence en définissant le scénario, le volume de données et le chemin testé avant d'affirmer le seuil de 200 ms.

## Tests et critères de validation

Commence par un test métier ou d'intégration qui vérifie que le scénario utilisateur dépendant du stockage (par exemple enregistrer puis retrouver les instruments, résultats ou runs requis, ou relire une archive JSON et la consommer dans le workflow d'ingestion) retourne les données et états attendus. Complète avec :

- tests techniques ciblés du mapping et des règles de sérialisation/clés/invalidation ;
- tests transversaux d'intégration EF Core/SQL Server et Redis selon les infrastructures de test déjà présentes ;
- vérifications des migrations, lecture/écriture, contraintes d'unicité, transaction/annulation et comportement en cas d'indisponibilité.

- Les tests doivent être déterministes et isoler les dépendances externes selon les conventions du dépôt.
- Exécute d'abord les tests métier, puis les tests techniques pertinents ; indique explicitement si un service local/conteneur requis n'était pas disponible.

## Livrable

Fournis le changement de persistance ou cache demandé, les migrations/tests/documents de configuration nécessaires et un résumé des garanties de cohérence, de TTL et d'indisponibilité effectivement implémentées.
