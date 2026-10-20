---
description: "Implémenter ou valider un moteur de pricing et ses contrats de données"
name: "Pricing et modèles financiers"
argument-hint: "Précise le modèle, l'instrument, le mode et le comportement de pricing à traiter"
agent: "agent"
---

# Tâche : pricing et modèles financiers

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md). Inspecte les contrats, modèles numériques, conventions financières et tests existants avant de coder.

Applique la philosophie de traitement vectoriel des pratiques communes : les contrats de pricing acceptent des ensembles cohérents de requêtes et de scénarios et conservent un résultat identifiable par élément, sans forcer les appelants à orchestrer les cas scalaires.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : conserve la modification dans le modèle/responsabilité demandés, examine ses consommateurs et demande confirmation avant de changer les contrats ou l'architecture de pricing, ou d'affecter le comportement d'autres fonctionnalités.

Pour toute démo, utilise toujours directement les fichiers de settings définis dans les projets source lorsqu'ils existent, sans les remplacer ni les fusionner avec des overrides locaux. Un fichier local séparé peut uniquement configurer une dépendance distincte qui ne possède pas ses propres settings source ; les overrides des settings source sont réservés aux mocks de test.

La cible reste .NET 10. Utilise la dernière version stable compatible des packages NuGet nécessaires aux calculs ou tests concernés ; mets à jour les dépendances pertinentes pour lever une incompatibilité et documente toute contrainte qui impose une version compatible plus ancienne.

Place le moteur après la disponibilité des contrats et des données de marché dont le calcul dépend. Organise les projets dans `Pricing` ou le répertoire thématique existant, en séparant clairement contrat métier, calcul et adaptateurs si ces responsabilités existent indépendamment. Les helpers mathématiques purement techniques ne sont mutualisés que s'ils servent réellement plusieurs consommateurs.

## Résultat métier

Fournir des prix et observables reproductibles adaptés aux instruments couverts, avec des interfaces de pricing et de données de courbe/volatilité cohérentes. Les contrats envisagés comprennent `IPricingEngine`, `ICurveProvider` et `IVolSurfaceProvider`. La vision inclut Black-Scholes, Black-76, Bachelier, binomial, FDM et Monte-Carlo ; les premières implémentations minimales mentionnées sont Black-Scholes analytique et Monte-Carlo parallèle seedable.

## Consignes d'implémentation

- Implémente seulement le modèle demandé ; n'annonce pas la prise en charge des autres modèles par anticipation.
- Pour les modèles et records de données financières, déclare chaque propriété explicitement avec ses accesseurs et privilégie `required init`/`init` et les object initializers. N'utilise pas un constructeur uniquement pour recopier ses paramètres dans des propriétés ; conserve les constructeurs pour un comportement nécessaire qui ne peut pas être exprimé dans les accesseurs.
- Vérifie que l'instrument et les entrées respectent les hypothèses du modèle. Valide explicitement les domaines numériques et paramètres requis.
- Sépare les contrats métier des algorithmes et des fournisseurs de courbes/surfaces. Évite que les calculs dépendent directement de l'UI, EF Core ou Redis.
- Précise l'unité, la devise et la convention de chaque entrée/sortie selon les modèles existants. N'arrondis pas implicitement les résultats.
- Pour Black-Scholes, teste les cas de référence, les limites (échéance, volatilité ou taux selon domaine) et la sensibilité aux paramètres dans leurs domaines valides.
- Pour Monte-Carlo, injecte/contrôle la graine pour les tests, contrôle le degré de parallélisme, accepte `CancellationToken` et évite les résultats différents dus à un partage incorrect du générateur aléatoire entre workers.
- Place les paramètres configurables propres au pricing (mode, paramètres numériques et parallélisme selon les règles établies) dans le fichier JSON spécialisé du pricing. Valide-les et évite qu'ils changent implicitement les options de l'ingestion ou du backtest ; documente tout couplage réel selon le [prompt de configuration](./feature-configuration.prompt.md).
- Pour les modèles numériques, vérifie erreurs, convergence et comportement aux limites avec des références indépendantes adaptées.
- Implémente `Accurate`/`Fast` seulement si la demande définit leur comportement, ou si le dépôt le spécifie. « Précision nano » est une exigence à mesurer, pas une garantie implicite.
- Trace les opérations de pricing significatives avec identifiants corrélés, durée, modèle, statut et erreur utile (sans valeurs d'entrée sensibles ou logs par itération), afin que leur run soit consultable dans la GUI.
- Le cache doit être cohérent avec les entrées de marché et les versions de modèle ; traite son implémentation avec le prompt persistance/cache si cela dépasse le scope.

## Tests et critères de validation

Commence par un test technico-métier du résultat de pricing demandé : résultats déterministes conformes à des références indépendantes dans une tolérance explicite, avec les unités et hypothèses applicables. Si un scénario utilisateur traverse aussi acquisition de données, courbes/surfaces ou cache, ajoute un test d'intégration transversal qui vérifie le résultat métier complet. Complète avec :

- tests techniques unitaires de domaine, validation, cas limites et entrées invalides ;
- tests de convergence pour les algorithmes Monte-Carlo ou numériques, avec seuils, nombre d'échantillons/maillage et graine clairement fixés ;
- tests de déterminisme sous configuration seedable et tests d'annulation/parallélisme là où pertinents ;
- mesures de performance liées au scénario demandé, si un seuil de temps fait partie de l'acceptation.

- Rapporte les tolérances, unités, références et limites. Ne revendique pas une précision nanométrique sans protocole reproductible qui l'établit.

## Livrable

Fournis le moteur/contrat demandé, ses tests et la documentation des hypothèses et unités. Indique les classes d'actifs/modèles réellement couverts et les commandes de validation exécutées.
