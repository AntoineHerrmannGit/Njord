# Prompts du projet d'application financière

Ce dossier rassemble le contexte produit et les consignes réutilisables pour faire évoluer l'application financière .NET 10. Il s'adresse à une IA de développement : les documents expliquent les responsabilités et les limites de chaque composant, sans supposer que l'IA connaît déjà l'architecture du projet.

Toutes les modifications doivent conserver .NET 10 comme cible et privilégier les versions stables NuGet les plus récentes compatibles. La procédure de mise à jour et de validation est définie dans [EngineeringPractices.md](./EngineeringPractices.md).

L'utilisation de `Semaphore` et `SemaphoreSlim` est interdite dans toute l'application, ses projets et ses tests. Pour les détails et les mécanismes à privilégier, voir la section architecture de [EngineeringPractices.md](./EngineeringPractices.md).

## Documents de référence

- [BusinessContext.md](./BusinessContext.md) est la source de vérité pour les objectifs métier, les fonctionnalités attendues et les contraintes formulées dans le cahier des charges.
- [EngineeringPractices.md](./EngineeringPractices.md) définit la manière de travailler dans le dépôt : exploration, architecture, implémentation, tests, documentation et validation.
- [BusinessContext.md](./BusinessContext.md) décrit également une séquence de développement indicative et une organisation thématique des projets par responsabilité.

Avant toute modification, lire ces deux documents et examiner l'état réel du dépôt. Le code et la configuration déjà présents priment sur toute hypothèse : préserver les choix déjà établis et ne pas recréer ce qui existe.

## Prompts par fonctionnalité

Lancer le prompt correspondant au travail demandé. Pour une fonctionnalité transversale, lancer les prompts concernés séparément et respecter leurs prérequis. La séquence indicative est : fondations partagées minimales → stockage/données → ingestion → pricing → backtesting → scripting/extensions → UI WPF, avec sécurité, observabilité, tests et documentation intégrés tout au long du développement. Voir [BusinessContext.md](./BusinessContext.md) pour les dépendances et exceptions.

1. [persistence-and-cache.prompt.md](./features/persistence-and-cache.prompt.md) — SQL Server, EF Core, stockage JSON et Redis.
2. [data-ingestion.prompt.md](./features/data-ingestion.prompt.md) — fournisseurs de données, import CSV/Parquet, rechargement d'archives JSON dans le workflow, normalisation et qualité des données.
3. [pricing.prompt.md](./features/pricing.prompt.md) — contrats de pricing, données de marché, modèles et validation numérique.
4. [backtesting.prompt.md](./features/backtesting.prompt.md) — stratégies, simulation événementielle, reprise et persistance des runs.
5. [scripting.prompt.md](./features/scripting.prompt.md) — édition et exécution contrôlée de scripts C# avec Roslyn.
6. [desktop-ui.prompt.md](./features/desktop-ui.prompt.md) — interface desktop, AvalonEdit, visualisations et expérience utilisateur.
7. [plugins-and-contracts.prompt.md](./features/plugins-and-contracts.prompt.md) — découverte MEF, contrats de plugins et compatibilité de versions.
8. [feature-configuration.prompt.md](./features/feature-configuration.prompt.md) — fichiers JSON spécialisés, validation et isolation des paramètres.
9. [security-and-observability.prompt.md](./features/security-and-observability.prompt.md) — authentification locale, secrets, audit, logs, métriques, traces et graphes d'exécution.
10. [quality-and-documentation.prompt.md](./features/quality-and-documentation.prompt.md) — tests, documentation technique, exemples et critères de livraison.

## Utilisation

Les fichiers `*.prompt.md` sont des modèles de tâches réutilisables. Dans VS Code, les prompts de workspace sont normalement découverts sous `.github/prompts/`. Si ce dossier est le répertoire de travail des prompts et non la racine du dépôt, copier ou adapter les fichiers de fonctionnalités dans `.github/prompts/` du dépôt pour les rendre disponibles comme prompts VS Code. Placer les règles communes dans `.github/copilot-instructions.md` si elles doivent être chargées automatiquement pour chaque tâche.

Pour chaque demande :

1. Lire les documents de référence et le prompt de fonctionnalité sélectionné.
2. Examiner les projets, dépendances, conventions et tests existants.
3. Situer la tâche dans la séquence de développement et vérifier que ses prérequis fonctionnels/techniques existent.
4. Suivre les appelants, dépendances et consommateurs pour analyser les impacts locaux et de bout en bout ; identifier le résultat métier attendu.
5. Délimiter un changement aussi local que possible. Demander confirmation avant toute modification architecturale de la fonctionnalité ou tout impact global/transversal non explicitement demandé.
6. Implémenter le changement accepté dans les projets dont les responsabilités sont clairement identifiées, puis adapter les usages directement affectés pour préserver cohérence, maintenabilité, modularité et localité.
7. Vérifier en priorité le comportement métier attendu, puis les tests techniques pertinents (techniques, technico-métier ou transversaux selon la responsabilité modifiée).
8. Signaler clairement le périmètre réalisé, les impacts couverts, les résultats de validation et les limites restantes.

Ne pas traiter l'ensemble de la vision produit comme une seule tâche de génération. Construire et valider l'application fonctionnalité par fonctionnalité.
