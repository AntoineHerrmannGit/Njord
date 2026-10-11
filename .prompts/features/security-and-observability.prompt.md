---
description: "Implémenter la sécurité locale, la protection des secrets, l'audit ou l'observabilité"
name: "Sécurité et observabilité"
argument-hint: "Précise le flux de sécurité, l'événement d'audit ou la métrique à traiter"
agent: "agent"
---

# Tâche : sécurité et observabilité

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis inspecte la configuration, l'authentification, les secrets, l'audit et les fournisseurs de télémétrie déjà utilisés.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : limite le changement au contrôle ou à l'opération demandée, suis ses interactions avec les fonctions qui l'utilisent et demande confirmation avant de changer une politique transversale de sécurité, d'audit ou de collecte.

Maintiens les projets en .NET 10. Pour les packages NuGet de sécurité, logging, OpenTelemetry et export Prometheus touchés, utilise la dernière version stable compatible ; mets à jour les dépendances associées si cela résout le problème, et documente toute contrainte nécessitant une version stable compatible plus ancienne. Ne force pas une version incompatible.

La sécurité et l'observabilité sont transversales : définir tôt les mécanismes minimaux et les intégrer à chaque workflow au fil des développements, puis construire les vues d'observabilité après disponibilité des événements structurés. Garder les bibliothèques/adaptateurs techniques partagés séparés des audits et règles métier ; placer les projets sous un thème explicite tel que `Core`, `Infrastructure` ou `Observability` selon l'organisation existante.

## Résultat métier

Fournir authentification locale, chiffrement des secrets et audit trail, avec logs structurés via `Microsoft.Extensions.Logging`, traces et métriques OpenTelemetry, ainsi qu'une exposition compatible Prometheus selon l'architecture existante. La GUI doit permettre de consulter les logs et l'audit utile, et de visualiser le graphe d'exécution de runs fonctionnels (ingestion, pricing, compilation, backtest et autres opérations instrumentées).

## Consignes d'implémentation

- Avant de coder, précise quel actif est protégé, contre quel acteur et dans quel flux ; respecte les mécanismes OS/plateforme et packages de confiance déjà présents.
- N'invente pas de primitives cryptographiques. Utilise les APIs reconnues et le modèle de gestion de clés déjà retenu ; distingue chiffrement, stockage de clés et protection de la configuration.
- Ne journalise jamais secrets, tokens, chaînes de connexion complètes ou données sensibles. Masque/minimise les informations sensibles sans dégrader l'audit nécessaire.
- Définis les événements d'audit avec identité disponible, action, cible, horodatage et résultat appropriés. Évite qu'un échec d'audit soit silencieusement présenté comme une opération sûre lorsque la politique exige un audit complet.
- Sépare l'audit métier (actions et transitions pertinentes imputables à un acteur/run) des traces techniques (étapes, durées, dépendances et erreurs). La complétude requise pour l'audit obligatoire doit être distincte de la collecte diagnostique facultative.
- N'enregistre pas des données de marché à haute cardinalité dans les labels de métriques. Garde les métriques bornées, agrégées et utiles.
- Instrumente les frontières importantes (fournisseurs, stockage, calcul, backtest) sans ajouter de dépendance à OpenTelemetry dans le cœur métier si une instrumentation externe suffit.
- Utilise des noms d'opérations stables, une parenté de spans, horodatages, statuts et identifiants de corrélation/run afin que l'interface puisse construire une chronologie/graph d'exécution exploitable. Instrumente au minimum la récupération de données, le pricing, la compilation Roslyn et le backtest.
- Limite le bruit : trace les frontières et étapes significatives, pas chaque boucle, tick, ligne de marché ou événement répétitif à haut débit. Préfère les résumés/agrégations et un échantillonnage configurable pour les diagnostics détaillés.
- Conserve pour chaque run observé les spans essentiels de toutes ses étapes significatives afin que son graphe reste disponible après exécution. Réserve l'échantillonnage aux détails de bas niveau ; ne supprime ni n'échantillonne les événements d'audit obligatoires.
- Définis une capacité de tampon bornée, une politique de flush, rotation/rétention et plafonds de stockage. Rends explicite le comportement en saturation/indisponibilité : une télémétrie diagnostique secondaire ne doit pas bloquer le traitement métier ; l'audit obligatoire suit sa garantie documentée et doit signaler clairement toute perte.
- Fournis des requêtes paginées/filtrables ou un chargement incrémental pour l'interface. Ne lis pas tous les logs et traces en mémoire pour afficher une seule opération.
- Respecte les paramètres de consentement/configuration et les contraintes de vie privée du dépôt. Aucun export réseau implicite de télémétrie.
- Pour l'authentification locale, vérifie les hypothèses sur l'identité et le stockage des utilisateurs avant de décider d'un modèle de comptes.

## Tests et critères de validation

Commence par le résultat métier/opérationnel du cas visé : l'action permise ou refusée donne le résultat attendu et son événement d'audit requis est visible/corrélé ; une opération instrumentée peut être retrouvée dans la GUI avec un graphe fidèle à ses étapes. Complète avec :

- tests transversaux d'accès autorisé/refusé et de protection, dont absence de fuite de secrets dans les logs ;
- tests des événements d'audit sur succès et échec, selon les garanties demandées ;
- vérification technique de l'émission et du format des métriques/traces et du comportement en configuration désactivée ou indisponible ;
- tests de corrélation d'un run multi-étapes, de génération d'un graphe d'exécution, de limites de volume/rétention et de comportement lorsque le tampon est plein.

- Build/tests ciblés ; documente les dépendances à l'OS et les étapes de configuration sans jamais publier de secret.

## Livrable

Fournis le changement ciblé, tests et documentation de configuration/menaces/limites. Décris précisément ce qui est chiffré, où résident les clés, quels événements sont audités et quelles données sont instrumentées.
