---
description: "Créer ou faire évoluer le moteur de backtest événementiel et ses stratégies"
name: "Backtesting et stratégies"
argument-hint: "Décris la stratégie, le cycle d'exécution ou le mécanisme de reprise à traiter"
agent: "agent"
---

# Tâche : backtest et stratégies

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine le moteur, les contrats de stratégie, les événements et la persistance existants.

Applique la philosophie de traitement vectoriel des pratiques communes : représenter et traiter les ensembles de scénarios, stratégies et instruments comme des lots identifiés ; conserver les résultats et erreurs par scénario sans imposer un dispatch scalaire au consommateur.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : garde le changement aussi local que possible, suis le parcours complet stratégie → replay → exécution → résultats/persistance → GUI, et demande confirmation avant de changer l'architecture ou les contrats partagés du moteur ou d'étendre son impact global.

Maintiens les projets en .NET 10. Pour les packages NuGet du moteur, de sérialisation ou d'exécution concernés par le changement, choisis la dernière version stable compatible ; mets à jour les dépendances nécessaires si elle ne convient pas, et documente le recours à la version stable la plus récente compatible si l'obstacle demeure.

Développe le workflow après les contrats et services de données/pricing dont le scénario demandé a besoin, puis intègre scripting et UI à partir de contrats stables. Regroupe le code dans des projets à responsabilité unique sous `Backtesting` (ou répertoire thématique existant) ; ne fusionne pas moteur, stockage et interface.

## Résultat métier

Permettre de rejouer des données historiques dans un moteur mono-machine événementiel, pour évaluer des stratégies systématiques ou des approches orientées carnet d'ordres. Le moteur doit pouvoir évoluer vers une distribution queue/workers, et prendre en charge simulation incrémentale, persistance, relance et reprise de runs partiels.

## Consignes d'implémentation

- Réutilise `IStrategy` s'il existe ; définis un contrat minimal et explicite seulement s'il manque et que le scope le requiert.
- Garde les règles de stratégie séparées de l'ordonnanceur de replay, du chargement des données, des adaptateurs de stockage et de l'interface utilisateur.
- Définis et respecte l'ordre déterministe des événements, les timestamps et le traitement des événements simultanés selon le code ou la spécification. Ne fabrique pas de convention implicite.
- Modélise les effets simulés (ordres, exécutions, slippage, frais, latence) de façon explicite et testable. Si la demande ne précise pas leurs règles financières, identifie le manque et demande clarification avant d'affirmer la validité des résultats.
- Traite incrémentalité et reprise comme des garanties de cohérence : identifiant de run, état/checkpoint, position de replay et résultats persistés doivent permettre d'éviter omissions et doubles exécutions.
- Rends les tâches distribuables sérialisables en JSON uniquement selon un schéma versionné, stable et validé. N'ajoute pas de service de queue ou workers distribués si le périmètre exige seulement une extension future.
- Range les paramètres du moteur et de simulation dans la configuration JSON spécialisée du backtest. Ne fais pas hériter au moteur des réglages d'une autre fonctionnalité sans contrat documenté ; voir le [prompt de configuration](./feature-configuration.prompt.md).
- Propague annulation et erreurs ; distingue arrêt volontaire, run terminé et run échoué.
- Publie des mises à jour de progression structurées et corrélées au run, avec une fréquence bornée/configurable (réglage du domaine backtest dans sa configuration JSON) ou un mécanisme de coalescence afin que la GUI puisse montrer l'évolution en temps réel sans recevoir chaque événement interne.
- Instrumente les étapes du run avec des spans ou événements structurés (nom stable, parenté, début/fin, durée, résultat et erreur pertinente) pour permettre de construire le graphe d'exécution après le run.
- Conserve dans les événements d'audit métier les transitions importantes du run (démarrage, reprise, annulation, réussite/échec et modification métier pertinente), pas chaque tick ou changement interne à haute fréquence.
- Coordonne la collecte avec les règles globales de rétention et de capacité décrites dans [les pratiques d'implémentation](../EngineeringPractices.md) ; ne duplique pas des payloads de données de marché dans les logs ou traces.
- La persistance SQL est décrite dans [le prompt stockage/cache](./persistence-and-cache.prompt.md) : réutilise ses abstractions plutôt que de lier le moteur à EF Core.

## Tests et critères de validation

Commence par un test métier du workflow de backtest demandé : le run termine dans l'état attendu et retourne les métriques/résultats fonctionnels attendus avec les données et règles de simulation spécifiées. Pour les comportements visibles pendant le run, vérifie également que la progression présentée à la GUI correspond à l'état réel. Complète avec :

- tests technico-métier déterministes d'ordre des événements, états de stratégie et calculs de P&L ;
- tests de frais, slippage et latence selon les règles effectivement spécifiées ;
- test transversal comparant exécution continue et reprise d'un run, y compris prévention des doublons ;
- tests d'exécution incrémentale et validation du schéma JSON ;
- tests de progression corrélée, fréquence/coalescence des mises à jour et spans sans modification du résultat financier.

Exécute d'abord les tests de workflow métier, puis les tests techniques pertinents et un build. Énonce les hypothèses de simulation dans les tests et la documentation.

## Livrable

Fournis l'évolution demandée avec contrats, mécanismes de persistance nécessaires et tests. Explique les garanties réelles de déterminisme/reprise, les hypothèses de marché simulées et ce qui reste hors périmètre.
