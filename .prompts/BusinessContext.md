# Contexte métier et contraintes fonctionnelles

Ce document décrit le produit visé. Il conserve les besoins exprimés dans `ProjectOverview.md` et les rend utilisables par une IA chargée d'implémenter une fonctionnalité. Il ne constitue pas une autorisation d'inventer des règles métier absentes.

## 1. Produit et utilisateurs

Le produit est une application desktop financière écrite en .NET 10 pour l'analyse des marchés, le pricing et l'évaluation de stratégies de trading. Elle doit permettre à l'utilisateur de charger des données de marché, de consulter des prix et indicateurs, de lancer des simulations et d'examiner les résultats depuis une interface riche.

Les projets de la solution doivent cibler .NET 10 (par exemple `net10.0` ou `net10.0-windows` pour WPF). Utiliser les versions stables NuGet les plus récentes qui restent compatibles avec .NET 10, le framework concerné et les autres dépendances nécessaires. Si une version récente ne convient pas, mettre à jour les dépendances concernées pour lever l'incompatibilité ; si la dernière version reste impossible à utiliser sans casser une contrainte technique ou élargir le changement, retenir la version stable la plus récente compatible et documenter la raison.

L'application vise les classes d'actifs suivantes :

- actions et indices ;
- options ;
- futures ;
- swaps et repos ;
- taux ;
- devises (FX).

Le cahier des charges ne définit pas encore les profils d'utilisateurs, les règles de permissions détaillées, les marchés prioritaires, les calendriers de cotation ni les conventions propres à chaque instrument. Ne pas inventer ces règles lorsqu'elles influencent un résultat financier.

## 2. Fonctionnalités attendues

### Données de marché et ingestion

- Connecteurs REST pluggables, notamment Massive/Polygon et Yahoo Finance.
- Import et parsing de fichiers CSV et Parquet.
- Normalisation des symbologies et usage d'un ticker unique en mode référentiel.
- Validation, enrichissement et backfill des données.
- Persistance SQL Server par Entity Framework Core.
- Export ou archivage des données historiques dans des fichiers JSON.
- Récupération et rechargement, dans le workflow d'ingestion, des données historiques déjà persistées dans les fichiers JSON ; ces archives doivent pouvoir servir de source de données pour les étapes suivantes du workflow.
- Cache Redis local pour les séries temporelles et les résultats fréquemment consultés.

Les formats détaillés, les champs canoniques, les règles de résolution des symboles, les politiques de correction des données et les conditions d'utilisation des fournisseurs restent à définir si le dépôt ne les établit pas déjà.

### Pricing et données de marché dérivées

Les contrats envisagés sont `IPricingEngine`, `ICurveProvider` et `IVolSurfaceProvider`.

La vision inclut les modèles Black-Scholes, Black-76, Bachelier, binomial, différences finies (FDM) et Monte-Carlo, avec des modes `Accurate` et `Fast`, des tests de convergence et du caching.

Les premières implémentations explicitement minimales sont :

- un moteur analytique `BlackScholesPricingEngine` ;
- un moteur `MonteCarloPricingEngine` utilisant TPL avec une graine contrôlable ;
- des tests de convergence.

Ne pas appliquer un modèle à un instrument sans vérifier ses hypothèses et les conventions de marché pertinentes. Le sens exact de « précision nano » et les critères de comparaison doivent être rendus mesurables pour chaque calcul.

### Stratégies et backtesting

- Contrat de stratégie `IStrategy`.
- Rejeu historique événementiel, convenant aux stratégies systématiques comme aux simulations au carnet d'ordres.
- Simulation du slippage, des frais et de la latence.
- Exécution fonctionnelle sur une machine, avec une architecture qui puisse évoluer vers une distribution par queue et workers.
- Persistance des runs dans SQL Server.
- Simulation incrémentale, reprise d'un run partiel et relance d'un run interrompu.
- Schéma JSON de tâches en vue d'une distribution future.

Le cahier des charges ne fixe pas les règles précises d'exécution des ordres, le modèle de carnet, les hypothèses de slippage, la définition des frais, ni les garanties de déterminisme. Ces éléments doivent être recherchés dans le code ou clarifiés avant de produire des résultats qui seraient présentés comme fiables.

### Scripting

- Compilation à la volée de scripts C# avec Roslyn.
- Chargement isolé via `AssemblyLoadContext`.
- API restreinte exposée aux scripts.

L'isolation par `AssemblyLoadContext` ne constitue pas à elle seule une sandbox de sécurité. Toute exécution de code non fiable doit être traitée comme un risque majeur ; ne pas prétendre qu'un chargement isolé empêche un script d'accéder aux ressources du processus ou de la machine.

### Interface desktop

- Application desktop sous .NET 10.
- Framework hôte choisi : WPF.
- AvalonEdit pour l'édition de scripts.
- Console de pricing et visualisation de données de marché, de backtests et d'exécutions en temps réel.
- Tableaux de bord P&L, séries, courbes, scatter et données 2D/3D.
- Bibliothèque de visualisation à choisir parmi OxyPlot, ScottPlot ou LiveCharts, en tenant compte du support réel du framework retenu.
- Transitions et mises à jour en temps réel fluides.
- La vue de backtest doit présenter à la fois le résultat final et la progression pendant l'exécution, avec les données et indicateurs disponibles au fil des événements.
- L'application doit fournir une interface d'observabilité permettant de consulter les logs et les informations d'exécution des fonctionnalités.
- Après un run, l'utilisateur doit pouvoir consulter un graphe d'exécution retraçant les étapes et durées d'une fonctionnalité, notamment ingestion/récupération de données, pricing, compilation de scripts et backtest.
- L'observabilité doit distinguer les informations techniques nécessaires au diagnostic et les événements métier nécessaires à l'audit.

Les vues de logs et de graphes doivent refléter les données réellement collectées, signaler les informations indisponibles et permettre de relier les étapes d'un run sans bloquer l'interface.

### Configuration fonctionnelle

- L'application doit permettre de configurer ses fonctionnalités à partir de fichiers JSON spécialisés par domaine fonctionnel, stockés et chargés localement sur la machine. Aucun service centralisé de configuration n'est présumé.
- Le format privilégié pour les configurations est JSON.
- La configuration d'une fonctionnalité doit être isolée : sa modification ne doit pas changer implicitement le comportement d'une autre fonctionnalité.
- Si un paramètre ou un composant est partagé et crée un couplage fonctionnel, ce couplage doit être explicitement documenté dans le code, la documentation et les fichiers de configuration concernés.
- Ne pas utiliser de variables d'environnement pour la configuration de l'application, y compris les credentials : les placer dans le fichier JSON local spécialisé de leur fonctionnalité.
- Les fichiers contenant des secrets sont locaux, exclus de Git, accessibles uniquement au compte de l'application et ne doivent jamais être publiés dans le dépôt. En CI/CD, fournir le fichier JSON comme artefact secret protégé et le rendre disponible au runtime sans passer par des variables d'environnement.
- Les fichiers JSON d'exemple suivis par Git ne contiennent aucun secret ; ils utilisent une valeur vide ou un placeholder non fonctionnel.

### Opérations transversales

- Authentification locale.
- Chiffrement des secrets.
- Journal d'audit complet.
- Logs structurés, métriques et traces.
- `Microsoft.Extensions.Logging`, OpenTelemetry et compatibilité Prometheus.
- Audit technique et métier avec corrélation des opérations, incluant les runs de récupération de données, pricing, compilation et backtest.
- Rétention et volume d'observabilité maîtrisés afin d'éviter une consommation excessive de mémoire et de stockage.
- Chaque run doit conserver suffisamment de spans structurés pour afficher son graphe d'exécution ; limiter ou échantillonner les détails de bas niveau, pas les étapes essentielles du graphe ni les événements d'audit obligatoires.
- Injection des dépendances via `IServiceCollection`.
- Système de plugins avec découverte dynamique MEF et contrats versionnés.
- Microservices locaux uniquement si une justification technique l'exige ; communication UI vers application in-process par défaut.

## 3. Vision d'architecture

Le produit vise une solution modulaire .NET 10 suivant les principes SOLID et une séparation de type Clean Architecture. Les couches, de la plus technique/profonde à la plus proche de l'utilisateur, sont :

1. Technique ;
2. stockage des données (`DataStorage`) ;
3. gestion des données (`DataManagement`) ;
4. interfaces et mapping ;
5. services métier transversaux ;
6. logique métier spécifique ;
7. expérience utilisateur ;
8. interface utilisateur.

Ces noms décrivent des responsabilités, pas une obligation de créer huit projets pour chaque fonctionnalité. Garder les dépendances dirigées vers le cœur métier : les règles métier ne doivent pas dépendre d'EF Core, de Redis, d'un fournisseur web ou de l'UI. Utiliser les projets et patterns déjà présents si le dépôt en possède.

### Organisation des projets

Chaque projet de la solution doit être identifiable par son nom et porter une responsabilité technique ou métier unique. Par exemple : persistance SQL, cache Redis, workflow d'ingestion, moteur de pricing, moteur de backtest, compilation de scripts, contrats/extensions techniques, modèles métier, interface WPF ou ressources UI. Ne regroupe pas dans un même projet des responsabilités métier/techniques indépendantes simplement parce qu'elles sont utilisées par la même interface.

Regroupe les projets sous des répertoires thématiques pour que leur rôle soit immédiatement compréhensible. Les noms exacts doivent suivre la solution existante ; une organisation indicative pourrait ressembler à :

```text
src/
  Core/           # contrats et primitives partagées réellement nécessaires
  Data/           # ingestion, persistance, cache, séparés par responsabilité
  Pricing/        # contrats ou moteur de pricing selon le découpage retenu
  Backtesting/    # workflow et moteur de simulation
  Scripting/      # compilation et intégration des scripts
  Extensions/     # contrats et découverte des plugins si distincts
  Models/         # modèles métier partagés seulement si responsabilité distincte
  UI/             # application WPF, vues et ressources UI selon responsabilité
tests/            # projets de test organisés par responsabilité/flux
```

Ce schéma guide les nouveaux projets ; il n'impose pas de déplacer tous les projets existants. Les briques purement techniques (helpers, abstractions de transport, primitives d'observabilité, outils de sérialisation, etc.) peuvent être partagées par plusieurs fonctionnalités métier ou UI si elles ont une responsabilité cohérente et des consommateurs réels, ou si une capacité transversale explicitement demandée a des consommateurs prévus identifiables. Une telle responsabilité possède une implémentation faisant autorité, réutilisée par les consommateurs plutôt que réécrite dans chaque fonctionnalité. Le partage centralise le mécanisme générique, pas le mapping ni les règles métier propres à une fonctionnalité ou un fournisseur. Ces briques ne doivent pas créer de dépendance circulaire ni devenir un fourre-tout `Common`.

### Ordre de développement indicatif

Développer et valider l'application par incréments, dans l'ordre de dépendance suivant, en adaptant l'ordre aux composants déjà présents :

1. **Comprendre et cadrer** : inventorier les projets, confirmer les responsabilités, les TFM .NET 10, les dépendances et les flux métier. Définir ou ajuster localement l'organisation de la solution avant d'ajouter une fonctionnalité.
2. **Fondations partagées minimales** : établir seulement les contrats/primitives transversaux effectivement nécessaires (DI, erreurs, configuration, cancellation, identifiants/corrélation, tests). Ne pas construire une plateforme technique spéculative sans consommateurs concrets.
3. **Stockage et accès aux données** : mettre en place ou réutiliser SQL Server/EF Core, les archives JSON lisibles/écrites et le cache Redis si requis par les workflows ciblés.
4. **Ingestion** : connecter/importer, recharger les archives JSON, normaliser et valider les données, puis vérifier leur disponibilité dans le stockage.
5. **Pricing** : implémenter les modèles et observables demandés sur des entrées définies, avec références de résultats et tests technico-métier.
6. **Backtesting** : construire le workflow événementiel, les stratégies, les résultats, les sauvegardes/reprises et la progression live à partir des contrats de données/pricing nécessaires.
7. **Scripting et extensions** : intégrer compilation/exécution contrôlée aux contrats de stratégie ; introduire MEF et les contrats plugins au moment où les points d'extension concernés sont définis et peuvent être testés.
8. **Interface WPF** : construire les vues métier au-dessus de workflows fonctionnels (données, pricing, backtest, scripts), puis les vues d'observabilité à partir des événements et traces structurés.
9. **Validation et durcissement transversaux** : compléter les tests bout en bout, sécurité, audit, observabilité, performance et documentation à chaque incrément, puis vérifier l'ensemble des parcours concernés.

Cet ordre est un guide de dépendances, pas une obligation de livrer chaque étape en bloc. Une fonctionnalité demandée peut être livrée en tranche verticale (comportement métier, UI si nécessaire, persistance, observabilité et tests) à condition que ses prérequis soient satisfaits. Le socle technique et les projets partagés doivent être créés au fur et à mesure des besoins constatés, et non tous en amont.

## 4. Contraintes mesurables exprimées

- Pricing en mode `Accurate` : tolérance dite « nano », sans définition plus précise dans le cahier des charges.
- Accès aux données avec Redis : latence inférieure à 200 ms, sans protocole de benchmark ni taille de jeu de données définis.
- Monte-Carlo : parallélisme contrôlé.
- Robustesse : retry exponentiel, circuit breaker, validation des entrées et propagation des `CancellationToken`.
- Sécurité : secrets chiffrés et audit trail complet.
- Extensibilité : découverte des plugins documentée et contrats versionnés.
- Observabilité : les opérations doivent être corrélables de bout en bout et consultables dans l'interface, sans conserver indéfiniment toutes les traces détaillées.
- Configuration : paramètres métier rangés dans des fichiers JSON spécialisés, isolés par fonctionnalité ; tout couplage explicite est documenté dans le code, la documentation et la configuration.

Ne pas annoncer qu'un critère de performance ou de précision est atteint sans test mesurable et reproductible. Si son interprétation change le design, poser une question plutôt que choisir arbitrairement.

## 5. Livrables visés à terme

- Solution .NET 10 complète et compilable.
- Implémentations fonctionnelles des fonctionnalités décrites ci-dessus.
- Tests unitaires et d'intégration, avec scénarios métier réels et mode mock.
- Migrations initiales et entités de persistance pour instruments, métadonnées et backtests.
- Documentation d'architecture, dépendances et fonctionnement des composants.
- Guides d'installation locale de SQL Server et Redis, d'ajout de connecteurs, de scripting et de développement.
- Checklist de performance et exemples d'utilisation, dont des scripts C#.
- Documentation découpée par fonctionnalité et par concept.

Ces livrables constituent une vision globale. Une tâche donnée ne doit livrer que le périmètre demandé et ses prérequis immédiats, sans générer artificiellement toute la solution.

## 6. Points à clarifier quand ils sont nécessaires

Ne pas bloquer une tâche pour les questions sans impact sur celle-ci. En revanche, demander une décision avant d'implémenter lorsqu'un choix non spécifié conditionne la compatibilité, les résultats financiers ou la sécurité, notamment :

- framework desktop hôte si aucune décision n'existe ;
- instruments, marchés ou fournisseurs prioritaires lorsque « toutes les classes » est trop large pour la tâche ;
- conventions de données manquantes ou conflictuelles ;
- définition d'une précision « nano », d'un seuil de performance ou du benchmark correspondant ;
- sémantique de reprise, déterminisme et compatibilité de versions des runs de backtest ;
- niveau de confiance accordé aux scripts et environnement d'exécution réellement isolé.
