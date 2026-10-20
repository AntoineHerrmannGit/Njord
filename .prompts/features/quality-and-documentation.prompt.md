---
description: "Ajouter les tests, exemples ou documents techniques pour une fonctionnalité du produit"
name: "Qualité et documentation"
argument-hint: "Indique la fonctionnalité, les tests ou les documents à produire"
agent: "agent"
---

# Tâche : qualité et documentation

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine l'organisation réelle des tests et de la documentation. Reste dans le périmètre demandé ; ce prompt ne remplace pas le prompt de fonctionnalité qui définit le comportement à tester.

Vérifie la philosophie vectorielle des pratiques communes : les tests métier couvrent les comportements de lot (requêtes, instruments, scénarios et résultats) ainsi que l'identité, l'ordre et le traitement des erreurs par élément.

Applique le cadrage de portée et d'impact des pratiques communes : n'élargis pas le périmètre de tests ou de documentation à d'autres fonctionnalités sans nécessité, mais vérifie et mets à jour toute documentation/test directement affecté par un changement accepté.

Les projets et outils de test doivent rester compatibles avec .NET 10. Pour les packages NuGet de test, documentation ou build touchés, privilégie la dernière version stable compatible ; mets à jour les dépendances concernées pour résoudre une incompatibilité et documente le choix de la version stable la plus récente compatible si la dernière version ne peut pas être retenue.

Applique la stratégie de validation à chaque étape de la feuille de route plutôt qu'à la fin seulement. Organise les projets de tests par responsabilité et par flux métier sous un répertoire `tests` thématique ; vérifie que les projets de production restent identifiables et ne regroupent pas des responsabilités indépendantes.

## Objectif

Rendre le comportement demandé vérifiable, reproductible et utilisable par les développeurs, sans documenter des capacités qui n'existent pas réellement.

## Consignes de test

- Réutilise framework, nommage, fixtures et commandes présents. N'ajoute pas de dépendance de test sans nécessité.
- Commence par identifier le résultat métier que la suite de tests doit prouver : parcours utilisateur, résultat financier, données produites ou comportement opérationnel attendu.
- Priorise les tests métier et d'intégration qui vérifient ce résultat. Complète par des tests techniques unitaires pour les transformations/manipulations de données, technico-métier pour les calculs déterministes et leur exactitude/performance, et transversaux pour les workflows faisant intervenir plusieurs modules ou fonctionnalités.
- Couvre cas nominal, limites, entrées invalides, erreurs et annulation utiles à la fonctionnalité ; ces tests techniques ne remplacent pas la validation métier.
- Pour les calculs financiers, explicite unités, conventions, source de référence, tolérance et hypothèses ; ajoute des cas de convergence/propriétés quand pertinent.
- Pour tests intégration, identifie clairement les services réels requis (SQL Server, Redis, fournisseur HTTP) et utilise les doubles de test déjà établis lorsque l'intégration réelle n'est pas nécessaire.
- Assure le déterminisme des tests métier et techniques : graine contrôlée, clock injectée, données de test fixes et absence d'appels réseau non maîtrisés.
- N'assouplis pas un seuil ni ne retire un test seulement pour faire passer la suite ; diagnostique la cause.

## Consignes de documentation

- Organise la documentation dans l'emplacement déjà adopté par la solution et par fonctionnalité.
- Respecte le principe « un concept par fichier » pour les guides détaillés.
- Mets à jour README d'installation seulement si les prérequis ou étapes d'exécution changent.
- Documente les diagrammes d'architecture/dépendances et les flux de composants à partir du code réel (Mermaid ou ASCII).
- Ajoute les procédures d'ajout de connecteur, de scripting et de développement quand le scope touche ces domaines.
- Ajoute un exemple d'utilisation de toute nouvelle capacité exposée, avec données/valeurs non sensibles et résultats clairement présentés comme exemples.
- Pour les critères de performance, précise environnement, dataset, warm-up, nombre de répétitions et calcul du percentile/latence. Ne transforme pas une mesure unique en garantie.
- Pour la configuration et l'observabilité, documente la propriété des fichiers JSON locaux, l'isolation entre fonctionnalités, les couplages éventuels, la corrélation des runs, les spans conservés pour le graphe et les limites/règles de rétention.
- Indique les limites connues, choix à clarifier et commandes nécessaires pour reproduire les résultats.

## Validation et livraison

Exécute les tests et validations adaptées ; donne les commandes exactes et leurs résultats. Signale les prérequis qui empêchent certaines validations. Assure-toi que les liens vers les documents et exemples existent et que la documentation ne contredit pas le comportement implémenté.
