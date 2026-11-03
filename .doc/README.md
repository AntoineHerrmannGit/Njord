# Documentation Njord

Ce dossier centralise la documentation destinée à accompagner Njord. Il distingue les guides techniques pour les futurs développeurs des guides métier et utilisateur pouvant être remis aux clients.

## Sources de vérité

- Le code, les tests, les configurations et les démos indiquent le comportement réellement présent.
- Les documents de [.prompts/](../.prompts/ProjectOverview.md) décrivent le contexte produit, les contraintes et la vision cible ; ils ne prouvent pas qu'une capacité est déjà livrée.
- Le [README du dépôt](../README.md) présente la vision générale et l'état actuel du projet.
- Les [démos](../demo/README.md) illustrent les composants effectivement montrés.

La documentation utilisateur ne doit pas présenter une fonctionnalité envisagée comme disponible. Les résultats financiers et backtests doivent indiquer leurs données, hypothèses et limites vérifiées.

## Organisation

- `developer/` : architecture réelle, démarrage, conventions et procédures de développement.
- `user/` : guides de fonctionnalités et workflows métier réellement disponibles.
- Les README locaux restent dans leurs projets pour les informations propres à ceux-ci ; ajouter ici des liens plutôt que de dupliquer leur contenu.

Le contrat commun des transformations est décrit dans le [guide de mapping technique](./developer/technical-mapping.md).

Les dossiers seront créés au rythme des premiers guides validés, sans créer de pages vides ni documenter des capacités inexistantes.

## Maintenance

Mettre à jour la documentation concernée avec chaque changement accepté. Vérifier les liens et commandes décrits et signaler explicitement les validations qui n'ont pas pu être effectuées. En cas d'ambiguïté sur une règle métier ou une affirmation client, demander la décision du propriétaire du produit avant de la documenter comme confirmée.

## Secrets et fichiers de configuration

Ne jamais placer de mots de passe, tokens, secrets clients ou clés API privées/payantes dans le code ou les fichiers suivis par Git. Les conserver dans un fichier local spécifique à la fonctionnalité, exclu de Git et dont les accès sont restreints, conformément aux consignes du projet. Une valeur ne peut être suivie que si elle est réellement publique et non sensible et que les consignes Njord l'autorisent explicitement. Le fait qu'un fichier soit local ne rend pas ses secrets sûrs à committer.
