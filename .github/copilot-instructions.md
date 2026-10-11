# Consignes de développement Njord

## Références du projet

- Avant chaque tâche de développement, consulte `.prompts/ProjectOverview.md`, `.prompts/BusinessContext.md` et `.prompts/EngineeringPractices.md`.
- Pour une fonctionnalité donnée, consulte également le prompt correspondant dans `.prompts/features/`. Pour une tâche transversale, consulte chaque prompt directement concerné.
- Traite ces fichiers comme les références durables des objectifs produit, contraintes, architecture visée et pratiques de développement. Ne suppose pas que leur contenu est automatiquement chargé en mémoire : relis les documents pertinents au début de la tâche et si le contexte est condensé ou incertain.
- Le dépôt et son code/configuration présents décrivent l'état réel du projet. Vérifie-les avant toute modification ; ils priment sur les hypothèses ou l'architecture envisagée dans les prompts. Signale les écarts et ne crée pas implicitement des fonctionnalités ou projets que la tâche ne requiert pas.
- N'édite pas les documents de `.prompts` pour y consigner le travail courant. Mets à jour la documentation du produit dans son emplacement établi seulement lorsque le changement le justifie.

## Principes de mise en œuvre

- Garde les changements ciblés, respecte les responsabilités et dépendances existantes, puis adapte et teste les usages directement affectés.
- Avant d'ajouter un helper ou un mécanisme réutilisable, recherche son propriétaire dans la solution. Centralise chaque responsabilité transversale dans une implémentation faisant autorité et réutilise-la ; ne duplique pas le comportement dans les fonctionnalités. Garde toutefois le parsing de schéma externe et les règles métier dans leurs adaptateurs propriétaires.
- Maintiens la cible .NET 10 et privilégie les versions stables compatibles avec les frameworks et dépendances du dépôt.
- Valide le résultat métier demandé avec les tests pertinents avant de conclure ; indique les commandes exécutées et toute validation impossible.
- Ne suppose pas de conventions financières, de sécurité ou de comportement non établies par le code ou les spécifications. Demande une clarification lorsqu'un choix manquant affecterait ces résultats ou l'architecture au-delà du périmètre accepté.
