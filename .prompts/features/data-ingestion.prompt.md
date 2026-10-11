---
description: "Implémenter ou faire évoluer l'ingestion et la normalisation des données de marché"
name: "Ingestion des données de marché"
argument-hint: "Décris le fournisseur, le format ou le comportement d'ingestion à traiter"
agent: "agent"
---

# Tâche : ingestion des données de marché

Lis d'abord [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine le dépôt avant de modifier le code. Implémente uniquement le périmètre demandé par l'utilisateur et ses prérequis immédiats.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : garde le changement aussi local que possible, suis ses appelants et consommateurs, et demande confirmation avant de modifier l'architecture d'ingestion ou de toucher un comportement global/transversal non explicitement demandé. Adapte ensuite les usages affectés dans le périmètre accepté.

La solution cible .NET 10. Pour les packages NuGet utilisés ou ajoutés dans ce périmètre, utilise la dernière version stable compatible ; mets à jour les dépendances concernées pour résoudre une incompatibilité et, si la dernière version reste impraticable, conserve la version stable la plus récente compatible en documentant pourquoi.

Dans l'ordre de développement indicatif, l'ingestion vient après les contrats/fondations minimales et les stockages/archives dont elle dépend, et avant les fonctionnalités qui consomment les données. Garde les projets séparés par responsabilité (connecteurs/workflow vs persistance et cache) dans les répertoires thématiques pertinents ; toute brique technique partagée doit avoir plusieurs consommateurs réels et rester indépendante du métier.

## Résultat métier

Rendre possible l'acquisition, l'import et la récupération de données de marché de façon cohérente, validée et extensible pour les classes d'actifs prises en charge par le projet. Le système doit pouvoir accueillir des fournisseurs comme Massive/Polygon et Yahoo Finance, des imports CSV et Parquet, et recharger dans le workflow les données historiques depuis les fichiers JSON persistés, sans répandre un format externe dans le cœur métier.

## Consignes d'implémentation

- Réutilise les modèles de marché, conventions et contrats existants. Si aucun contrat n'existe et que la tâche en nécessite un, définis une frontière de connecteur claire et minimale.
- Garde le mapping JSON/vendor vers les modèles internes dans l'adaptateur du fournisseur.
- Réutilise l'unique service partagé de sérialisation/désérialisation et ses options/erreurs génériques lorsqu'il existe. Ne recrée pas ces mécanismes dans un connecteur ; garde les DTO de réponse, l'interprétation du schéma vendor et le mapping métier dans l'adaptateur, sans les exposer hors de celui-ci.
- Pour les réponses JSON structurées d'un fournisseur, définis des DTO typés internes à son adaptateur, désérialise avec le service partagé, puis valide les champs et mappe les données vers les modèles métier Njord. N'analyse pas la réponse en parcourant directement des `JsonElement` pour contourner les DTO ; réserve les API bas niveau aux usages réellement dynamiques qui ne se prêtent pas à un schéma DTO.
- Rends configurables les paramètres nécessaires du client HTTP par la configuration existante ; ne place jamais de clé API dans le code ou les logs.
- Pour les appels distants, gère explicitement timeout, annulation, erreurs HTTP, limites de débit et réponses invalides. Applique retry exponentiel et circuit breaker selon les conventions et packages déjà utilisés.
- Ne retry pas aveuglément les erreurs permanentes ou les opérations non idempotentes.
- Pour CSV/Parquet, valide le schéma et les valeurs, signale les erreurs de ligne avec suffisamment de contexte et évite d'abandonner silencieusement un fichier partiellement invalide.
- Traite les archives JSON persistées comme une source explicite du workflow d'ingestion, distincte de la lecture d'une réponse JSON de fournisseur. L'utilisateur ou le processus appelant doit pouvoir sélectionner/fournir l'archive selon le contrat existant ; le lecteur doit charger les données archivées et les rendre disponibles aux étapes d'ingestion demandées (validation, normalisation, enrichissement, backfill ou consommation aval).
- Réutilise le format/versionnement et les règles de lecture/écriture définis par la persistance ; ne crée pas un second format JSON concurrent ni un accès direct aux détails de stockage dans le cœur métier. Si le schéma d'archive est incompatible ou invalide, rapporte un diagnostic explicite et ne traite pas les données comme une ingestion réussie.
- Respecte l'ordre, les timestamps, les unités, les devises et la provenance des données rechargées. Évite les doublons lors d'une relecture si le contrat de persistance l'exige et rends le comportement de rejeu déterministe.
- Centralise la normalisation des symboles selon le référentiel du projet. Ne suppose pas qu'un ticker seul est globalement unique si une place, un fournisseur ou une classe d'actif est nécessaire pour lever l'ambiguïté.
- Sépare validation, enrichissement, backfill, mapping et persistance afin que chaque étape puisse être testée.
- Préserve explicitement horodatages, fuseaux, devise, unités, source et fraîcheur quand ces informations existent dans le schéma.
- Place les options propres à l'ingestion dans son fichier JSON spécialisé (fournisseurs, endpoints non secrets, timeouts, limites, politiques de backfill) via des options typées validées. Ne les rends pas globales au pricing ou au backtest ; si une dépendance partagée existe, documente-la conformément au prompt [configuration](./feature-configuration.prompt.md).
- Instrumente les étapes significatives de récupération/import pour qu'elles soient corrélables et visibles dans le graphe d'exécution, sans journaliser le contenu complet des réponses ni chaque ligne de marché.
- La persistance SQL/JSON et le cache Redis relèvent principalement de [persistence-and-cache.prompt.md](./persistence-and-cache.prompt.md) ; n'y duplique pas leurs adaptateurs.

## Tests et critères de validation

Commence par vérifier le résultat métier du flux d'acquisition/import demandé : les données attendues sont-elles obtenues, normalisées et rendues disponibles sans altérer les cas qui ne sont pas concernés ? Ajoute ensuite les tests techniques adaptés :

- le mapping de réponses valides et invalides de chaque fournisseur concerné ;
- les erreurs réseau et annulations, ainsi que la politique retry si elle est modifiée ;
- la normalisation des symboles, y compris les cas ambigus et les données inconnues ;
- les fichiers malformés, les champs manquants et les valeurs hors domaine ;
- la répétition/backfill sans doublons si l'opération est censée être idempotente.
- le chargement d'une archive JSON persistée valide dans le workflow d'ingestion et la disponibilité des données aux étapes suivantes ;
- les archives manquantes, malformées ou de version non prise en charge, avec une erreur explicite et sans résultat de succès partiel trompeur ;
- la relecture répétée, l'ordre et la préservation des attributs métier des données, selon les garanties établies.

Pour un flux traversant fournisseur ou archive JSON, mapping, validation et persistance, ajoute un test d'intégration qui vérifie le résultat fonctionnel de bout en bout. Exécute d'abord les tests métier, puis les tests techniques ciblés et un build pertinent. Ne prétends pas supporter « toutes les classes d'actifs » si les API, contrats ou tests n'en couvrent qu'un sous-ensemble ; indique précisément la couverture livrée.

## Livrable

Fournis une implémentation compilable dans les projets existants, les tests utiles, les éventuelles mises à jour de documentation/configuration d'exemple sans secret, puis résume les fournisseurs/formats réellement pris en charge (y compris les capacités de rechargement d'archives JSON) et les commandes de validation exécutées.
