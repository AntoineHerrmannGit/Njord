---
description: "Créer ou faire évoluer les fichiers JSON de configuration spécialisés par fonctionnalité"
name: "Configuration par fonctionnalité"
argument-hint: "Indique la fonctionnalité et les paramètres à configurer"
agent: "agent"
---

# Tâche : configuration spécialisée par fonctionnalité

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine le chargement de configuration, les options typées et les fichiers JSON déjà présents.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : ne modifie que la configuration de la fonctionnalité demandée, suis tous ses consommateurs et demande confirmation avant de changer le mécanisme global de configuration ou d'introduire un couplage inter-fonctionnalités.

Les projets restent sur .NET 10. Utilise les APIs intégrées ou la dernière version stable compatible des packages NuGet de configuration/validation ; mets à jour les dépendances concernées si nécessaire et documente tout recours à une version compatible plus ancienne.

Établis le mécanisme commun minimal de chargement/validation lorsque les premières fonctionnalités configurables en ont besoin, puis ajoute chaque schéma et fichier avec sa fonctionnalité propriétaire. Ne centralise pas toutes les options métier dans un projet/configuration unique ; garde les options et responsabilités dans leur domaine thématique.

## Résultat métier

Permettre à l'utilisateur ou à l'opérateur de paramétrer les fonctionnalités de l'application sans qu'une modification locale à l'une d'elles affecte les autres. Les fichiers de configuration JSON sont stockés et chargés localement sur la machine ; aucun service centralisé n'est présumé. Les configurations correspondent à des domaines fonctionnels clairement identifiés (par exemple ingestion, pricing, backtesting, scripting, observabilité ou interface).

## Consignes d'implémentation

- Avant de créer un nouveau fichier, vérifie la structure de configuration existante et les moyens de déploiement. Réutilise les conventions sans regrouper artificiellement des paramètres indépendants dans un unique fichier global.
- Donne à chaque fichier et section un propriétaire fonctionnel clair. Choisis des noms prévisibles, une structure JSON lisible, une version de schéma si le format persiste ou évolue, et des valeurs d'exemple sans secret.
- Lie chaque fichier JSON à ses options typées et au composant propriétaire via le mécanisme .NET de configuration et DI déjà utilisé. Évite que des classes métier lisent elles-mêmes des fichiers ou que des chaînes de configuration soient dispersées dans l'application.
- Ne déplace pas ces paramètres vers un service distant ou une configuration machine centralisée sans demande explicite.
- Garantir l'isolation : charger et appliquer la configuration du domaine visé uniquement. Tester qu'une clé, une valeur ou une erreur dans le fichier d'une fonctionnalité ne change pas les options d'une autre.
- Valider types, plages, champs obligatoires, valeurs inconnues et combinaisons incohérentes. Produire un diagnostic qui nomme le fichier/chemin JSON et le paramètre erroné ; ne pas ignorer silencieusement une valeur invalide ni démarrer avec un réglage par défaut qui change le sens métier.
- Ne pas ajouter un paramètre pour chaque constante. N'exposer que les décisions qui doivent réellement être configurables et documenter leurs valeurs par défaut.
- Ne pas copier un paramètre commun dans plusieurs fichiers sans définir son autorité. Si deux fonctionnalités partagent réellement un réglage ou qu'un composant dépend des options d'un autre, rendre le lien explicite et documenter le couplage dans le code, dans la documentation et dans chacun des fichiers JSON concernés.
- Privilégier un réglage local au domaine fonctionnel. Si un réglage global est indispensable (par exemple niveau et rétention d'observabilité), le placer dans un domaine transversal identifié et documenter les fonctionnalités impactées plutôt que le rendre implicitement global.
- Ne jamais stocker de clés API, mots de passe, tokens ou autres secrets en clair dans un fichier JSON committé. Employer les mécanismes de User Secrets, variables d'environnement, magasin sécurisé ou chiffrement déjà prévus ; les fichiers d'exemple doivent utiliser des placeholders non fonctionnels.
- Si le rechargement à chaud est demandé, définir les composants affectés, les garanties atomiques et les conséquences sur les opérations déjà démarrées. Ne pas introduire de rechargement dynamique implicite.
- Documenter pour chaque fichier sa finalité, son propriétaire, les valeurs par défaut, la validation, les secrets externes requis et les dépendances/couplages entre fonctionnalités.

## Tests et critères de validation

Commence par un test du cas d'usage métier configurable : une valeur autorisée dans le fichier de la fonctionnalité change le comportement prévu de cette fonctionnalité, sans altérer le résultat d'une autre. Complète avec :

- tests techniques de chargement JSON valide et diagnostics actionnables sur JSON malformé, valeur manquante, type incorrect ou valeur hors limites ;
- test transversal d'isolation démontrant qu'une modification d'un fichier n'altère pas les options ou comportements d'autres fonctionnalités ;
- tests des valeurs par défaut et des relations/couplages explicitement définis.

- Vérifier que les exemples de configuration sont valides et ne contiennent aucun secret.
- Exécuter tests ciblés et build, puis rapporter fichiers créés, paramètres et limites.

## Livrable

Fournis le ou les fichiers JSON spécialisés, les options typées, l'enregistrement DI, la validation, les tests et la documentation correspondants. Si aucun code de fonctionnalité n'est modifié, veille à ce que les fichiers de configuration décrivent exactement les options réellement prises en charge.
