---
description: "Créer ou faire évoluer l'interface desktop WPF, les vues, l'observabilité et l'éditeur AvalonEdit"
name: "Interface desktop"
argument-hint: "Décris l'écran, la visualisation ou le flux UI à réaliser"
agent: "agent"
---

# Tâche : interface desktop

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine le framework desktop, le modèle de présentation et les bibliothèques de visualisation déjà utilisés dans le dépôt.

Applique la philosophie vectorielle des pratiques communes à l'expérience utilisateur : permettre de visualiser, sélectionner et lancer les workflows sur des ensembles d'instruments, séries, requêtes ou scénarios plutôt que de limiter l'interface aux opérations unitaires.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : garde le changement au plus près de l'écran/flux demandé, suis ses interactions avec services métier, navigation et données, et demande confirmation avant de modifier le framework WPF, l'architecture UI partagée ou un comportement global de navigation.

Le projet WPF doit cibler .NET 10 avec un TFM Windows adapté, généralement `net10.0-windows`. Pour WPF, AvalonEdit et les bibliothèques de graphiques, privilégie les dernières versions stables compatibles avec ce TFM ; mets à jour les dépendances directement concernées pour résoudre les incompatibilités, sinon utilise la plus récente version compatible et documente la limite. N'introduis pas de préversions sans demande explicite.

Dans l'ordre indicatif, implémente les vues métier au-dessus des workflows et contrats qu'elles consomment ; un squelette WPF peut être livré plus tôt s'il ne fige pas les contrats métier. Regroupe l'application, les vues et ressources dans des projets UI à responsabilité claire sous `UI`, et évite de placer des règles métier dans ces projets.

## Résultat métier

Construire une interface WPF sous .NET 10 pour consulter les données de marché et les résultats de pricing, éditer des scripts, suivre des backtests et visualiser des exécutions en temps réel. Les vues envisagées incluent console de pricing, dashboard P&L, courbes, séries, scatter et visualisations 2D/3D. La GUI inclut aussi un espace d'observabilité où consulter les logs et l'exécution détaillée des fonctionnalités.

## Choix établis et limites

- Le framework hôte choisi est WPF. Vérifie le framework cible et la compatibilité des packages dans le dépôt ; ne remplace pas ce choix par WinUI, MAUI ou un autre framework.
- AvalonEdit est demandé pour l'éditeur de scripts. Vérifie sa compatibilité avec les versions .NET et WPF déjà ciblées.
- Choisis une bibliothèque de visualisation uniquement après avoir vérifié la compatibilité, la maintenance et les patterns déjà établis dans la solution. Ne réimplémente pas une bibliothèque de graphiques.

## Consignes d'implémentation

- Suis le modèle UI et le système de navigation existants. Si rien n'existe, propose la plus petite structure adaptée à la fonctionnalité demandée plutôt qu'un framework d'interface complet.
- Garde les règles financières et l'accès aux données hors des contrôles UI ; utilise les services métier existants et l'injection de dépendances.
- Pour les vues de données, affiche clairement unités, devises, horodatages, état de fraîcheur, erreurs et états de chargement quand ces informations sont disponibles.
- Évite de bloquer le thread UI lors d'un pricing, import, replay ou appel réseau ; prends en charge annulation et état de progression lorsque cela est pertinent.
- Pour les flux temps réel, gère mises à jour, désabonnement, durée de vie des vues et volume de données afin d'éviter fuites et ralentissements.
- Le suivi d'un backtest doit afficher sa progression pendant le run, pas seulement le rapport final : statut, étape/position de replay, métriques et événements disponibles, sans ralentir le moteur ni bloquer le thread UI.
- Prévois une vue d'observabilité permettant de suivre les logs récents en direct et de rechercher/filtrer l'historique. Permets de sélectionner un run de fonctionnalité pour afficher son graphe d'exécution après terminaison. Le graphe doit refléter les spans/événements structurés (étapes, parenté, durées, statut et erreurs), pas tenter d'inférer sa chronologie à partir de texte libre.
- La vue doit prendre en charge au minimum les opérations de récupération/ingestion de données, pricing, compilation de scripts et backtest, et être extensible aux autres fonctionnalités instrumentées.
- Sépare les logs techniques des événements d'audit métier et rends leur type, horodatage, niveau/statut et contexte de corrélation lisibles. Protège les secrets et données sensibles dans l'affichage.
- Les écrans d'observabilité doivent gérer le volume : pagination ou chargement incrémental, filtres par période/run/fonctionnalité, plafonds de résultats, tampon borné pour le flux live et rafraîchissement contrôlé. Ne charge pas tout l'historique en mémoire.
- Affiche clairement une télémétrie partielle, expirée ou indisponible ; ne présente pas un graphe incomplet comme une trace exhaustive.
- Rends les visualisations lisibles et explicites sur les données manquantes ; ne transforme pas silencieusement des valeurs invalides en graphiques plausibles.
- Utilise les contrôles d'AvalonEdit uniquement pour l'édition. La compilation/exécution des scripts relève des contrats du système de scripting.

## Tests et critères de validation

Commence par valider le parcours métier de l'écran : l'utilisateur peut effectuer l'action visée, voit les données ou résultats attendus et reçoit un état intelligible en cas de chargement, absence de données, erreur ou annulation. Ajoute un test d'intégration UI/service si le comportement dépend de plusieurs composants. Complète avec :

- tests techniques des modèles de présentation et commandes si le dépôt possède un framework de test UI adapté ;
- validation du flux de mise à jour live d'un backtest, du désabonnement en quittant la vue et du rendu d'un run terminé ;
- tests de filtrage/pagination et de représentation des traces incomplètes si la GUI possède un cadre de tests adapté.

- Build du framework cible et vérification de la compatibilité des packages UI utilisés.
- Si possible, vérification visuelle de l'écran ciblé ; ne prétends pas qu'elle a eu lieu si aucun rendu n'a été exécuté.

## Livrable

Fournis seulement l'écran ou le flux demandé, l'intégration métier nécessaire, tests pertinents et documentation d'usage. Signale tout choix de framework ou package non validé et ne modifie pas le framework hôte sans confirmation.
