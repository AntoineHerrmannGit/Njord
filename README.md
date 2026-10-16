# Njord

## Une plateforme pour concevoir, analyser et évaluer des stratégies de marché

**Njord est un projet de plateforme financière desktop destiné à réunir, dans un même environnement, les données de marché, l'analyse quantitative, le pricing et l'évaluation de stratégies de trading.** Sa vision est d'aider les équipes d'investissement et de recherche quantitative à passer plus efficacement de l'idée à l'analyse reproductible, en rendant les données, les hypothèses et les résultats plus faciles à examiner.

Njord est développé en .NET 10 selon une architecture modulaire. Le produit cible d'abord une utilisation locale sur poste de travail, avec des frontières conçues pour permettre l'ajout progressif de fournisseurs et de capacités sans enfermer les modèles métier dans les détails d'une API externe.

## La vision produit

L'ambition est d'offrir un espace de travail intégré où l'utilisateur pourra :

- acquérir, importer et organiser des données de marché provenant de fournisseurs et de fichiers ;
- travailler avec plusieurs classes d'actifs, notamment actions, indices, options, futures, devises et instruments de taux ;
- examiner les prix, séries historiques et indicateurs nécessaires à l'analyse ;
- appliquer des modèles de pricing et comparer leurs résultats avec des hypothèses explicites ;
- écrire ou sélectionner des stratégies, puis les évaluer par rejeu historique et backtesting ;
- consulter les résultats, leur progression et les étapes d'exécution dans une interface dédiée ;
- conserver une configuration locale propre à chaque fonctionnalité et intégrer de nouveaux fournisseurs ou extensions au fil des besoins.

L'objectif n'est pas de masquer la complexité financière derrière des résultats opaques. La conception vise à distinguer les données observées, les modèles et les résultats calculés, afin que les utilisateurs puissent comprendre quelles entrées et quelles conventions contribuent à une analyse.

## Fonctionnalités métier visées

La vision de Njord va au-delà d'une bibliothèque de calculs : il s'agit de fournir une interface de travail financière cohérente, organisée autour des activités de marché.

- **GUI et dashboards** : un environnement desktop pour accéder aux données, outils et résultats, avec des tableaux de bord destinés à présenter notamment les séries de marché, courbes, résultats de pricing, progression de backtests et indicateurs de performance.
- **Création et analyse de produits financiers** : permettre au structureur de décrire un produit à partir de ses caractéristiques financières — sous-jacents, paramètres et règles de paiement — puis d'en examiner les prix et comportements selon les données et modèles disponibles. Les produits, conventions et modèles pris en charge devront être explicitement définis et validés.
- **Scripting de stratégies indicielles et de trading** : offrir un espace pour créer des stratégies en C#, les compiler et les évaluer sur des données historiques par backtesting, avec des résultats et diagnostics consultables.
- **Backtesting** : rejouer des données historiques pour évaluer une stratégie ou étudier le comportement d'un produit dans différents scénarios. L'utilisateur pourra examiner les résultats et comparer des variantes avant d'envisager leur mise en œuvre opérationnelle.

### Valeur ajoutée visée pour un structureur : concevoir, tester, comparer

Njord ambitionne de rapprocher la conception d'un produit de son évaluation historique. Un structureur pourrait partir d'une définition produit et de ses règles de paiement, les confronter aux données de marché historiques, puis analyser et comparer les résultats de backtests dans le même environnement. Cette boucle de travail vise à accélérer l'exploration de variantes de structuration, à mieux comprendre leur comportement sur les périodes étudiées et à faciliter les échanges avec les équipes quantitatives et de trading.

Le backtest constitue une valeur ajoutée centrale : il permettrait d'évaluer systématiquement une stratégie ou un produit sur des données historiques, d'étudier ses résultats dans les conditions simulées et de comparer plusieurs hypothèses de conception. Il apporte un support à l'analyse, sans garantir les performances futures. La traçabilité visée distingue paramètres du produit, hypothèses de modèle, données utilisées et résultats calculés. Les conventions de marché, produits structurés, méthodes de valorisation, scénarios et indicateurs pertinents restent à définir et à valider ; aucune valorisation ou mesure de risque n'est présumée fiable sans cette validation.

## À qui s'adresse Njord ?

La plateforme est conçue pour répondre aux besoins de profils tels que :

- analystes quantitatifs et chercheurs qui explorent des données de marché et des hypothèses de modèle ;
- équipes de trading systématique qui prototypent et évaluent des stratégies ;
- développeurs d'outils financiers qui ont besoin de composants séparés et extensibles ;
- organisations qui souhaitent maîtriser localement leurs données, leurs configurations et leurs workflows d'analyse.

Ces profils décrivent les utilisateurs visés par la vision produit, et non des clients ou déploiements déjà établis.

## Une approche modulaire et extensible

La solution sépare les modèles métier des adaptateurs techniques. Les données provenant d'un fournisseur sont converties vers des modèles internes indépendants de son format, ce qui constitue la base d'une intégration graduelle d'autres sources.

L'architecture en cours de construction comprend des fondations de sérialisation JSON, des extensions techniques, une politique de retry configurable et des modèles provider-neutral d'instruments et de données de marché. Un connecteur de données historiques Massive est également présent pour récupérer des barres OHLC pour les classes d'actifs prises en charge par son API. La configuration est typée et chargée depuis des fichiers JSON locaux ; les secrets ne sont pas intégrés aux exemples ou au code suivi par le dépôt.

## État actuel et trajectoire

**Njord est en phase de développement.** La solution contient actuellement des modèles métier financiers, des utilitaires Core et un connecteur Massive testé avec des réponses simulées. Ces composants constituent un socle technique, pas encore un produit desktop prêt à l'emploi.

Les capacités plus larges de la vision — application WPF et dashboards, création et valorisation de produits financiers, workflow complet d'ingestion et de persistance, moteurs de pricing, backtesting, scripting de stratégies indicielles et de trading, système de plugins et interface intégrée d'observabilité — restent à concevoir et à développer. Les conventions financières, l'univers de données, les modèles de frais et de slippage, ainsi que les garanties de performance et de déterminisme devront être établis et validés avant de présenter des résultats comme adaptés à une décision d'investissement.

Cette distinction est importante pour un acquéreur potentiel : le dépôt démontre une direction produit et des composants de fondation, mais ne revendique pas aujourd'hui la disponibilité commerciale ni la livraison de l'ensemble de ces fonctionnalités.

## Intérêt potentiel pour un acquéreur

Njord peut constituer une base de travail pour une organisation souhaitant développer un environnement propriétaire d'analyse et de backtesting, avec une séparation claire entre domaine financier et intégrations techniques. Selon les besoins de l'acquéreur, la suite du développement pourrait prioriser un univers d'actifs, des fournisseurs, des workflows et des modèles particuliers, puis les valider dans un périmètre métier mesurable.

Une évaluation d'acquisition devrait notamment examiner la couverture fonctionnelle réellement livrée, les droits et conditions d'utilisation des données fournisseurs, les conventions financières retenues, les exigences de sécurité et les validations nécessaires avant tout usage opérationnel.

## Périmètre et responsabilité

Njord est un outil logiciel d'analyse en développement. Il ne constitue pas, en l'état, un service d'exécution d'ordres, un conseil financier, une garantie de performance ou une recommandation d'investissement. Les résultats futurs dépendront de la qualité des données, des hypothèses choisies, des conventions implémentées et de leur validation.
