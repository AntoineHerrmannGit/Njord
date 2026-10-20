---
description: "Implémenter l'édition, la compilation ou l'exécution de scripts de stratégie C#"
name: "Scripting C#"
argument-hint: "Précise le comportement du script, l'API exposée ou le flux de compilation concerné"
agent: "agent"
---

# Tâche : scripting C# avec Roslyn

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine les packages, contrats de stratégie, flux d'exécution et protections déjà présents.

Applique la philosophie vectorielle des pratiques communes : les APIs accessibles aux scripts représentent les ensembles d'instruments, observations et scénarios utiles comme des lots identifiables, dans les limites de l'API restreinte et du workflow appelant.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : localise le changement dans la responsabilité de scripting, suis son usage par le backtest et l'éditeur, et demande confirmation avant de modifier les contrats d'extension, le cycle de vie partagé ou l'architecture d'exécution.

Pour toute démo, utilise toujours directement les fichiers de settings définis dans les projets source lorsqu'ils existent, sans les remplacer ni les fusionner avec des overrides locaux. Un fichier local séparé peut uniquement configurer une dépendance distincte qui ne possède pas ses propres settings source ; les overrides des settings source sont réservés aux mocks de test.

Garde la cible .NET 10. Utilise les versions stables NuGet les plus récentes compatibles pour Roslyn et les dépendances impliquées ; si nécessaire, mets à jour les packages liés, puis retiens et documente la version compatible la plus récente si la version courante demeure inutilisable.

Construis la compilation sur les contrats de stratégie/API qui lui sont nécessaires, puis intègre-la au workflow de backtest et à l'éditeur UI sans coupler ces composants entre eux. Place les projets sous `Scripting` (ou l'équivalent existant) et sépare l'éditeur WPF, le compilateur et les contrats lorsque leurs responsabilités le justifient.

## Résultat métier

Permettre à l'utilisateur de créer des stratégies en C#, de les compiler à la demande avec Roslyn et de les faire interagir avec une API restreinte de l'application. Les scripts sont destinés à des cas tels que le backtesting ; ils ne doivent pas contourner le cycle de vie normal de la plateforme.

## Consignes d'implémentation

- Expose aux scripts uniquement des contrats explicites, versionnés et nécessaires à leur cas d'usage. N'expose pas directement des services d'accès aux secrets, au système de fichiers, au réseau ou à la base de données.
- Sépare compilation, diagnostic, chargement, exécution et déchargement des assemblies. Retourne des diagnostics actionnables (fichier, ligne/colonne, sévérité) sans réduire les erreurs à un message générique.
- Utilise `AssemblyLoadContext` pour isoler le chargement et permettre le déchargement lorsque le cycle de vie s'y prête.
- **Ne présente jamais `AssemblyLoadContext` comme une sandbox.** Le code C# exécuté dans le processus peut accéder aux capacités du processus. Si le besoin implique des scripts hostiles/non fiables, arrête-toi avant d'implémenter l'exécution in-process et signale qu'une isolation OS/processus adaptée est requise.
- Limite et valide les références compilées et les API disponibles dans la mesure permise par le design ; ne prétends pas que des listes de références seules garantissent une isolation de sécurité.
- Gère annulation, durée de vie, erreurs de compilation et exceptions d'exécution explicitement.
- Versionne les contrats consommés par les scripts et documente les règles de compatibilité si la plateforme persiste ou partage des scripts.
- Range les paramètres de compilation/exécution propres au scripting dans un fichier JSON spécialisé et validé. N'y mets pas les options UI de l'éditeur ni la configuration du moteur de backtest sauf couplage explicite documenté ; voir le [prompt de configuration](./feature-configuration.prompt.md).
- Émets des diagnostics et étapes de compilation structurés, corrélés au run, pour affichage et audit technique ; n'enregistre pas le code source intégral ou les références sensibles dans les logs par défaut.

## Tests et critères de validation

Commence par tester le résultat métier du flux demandé : un script valide peut être édité/compilé puis utilisé par la fonctionnalité prévue (par exemple une stratégie de backtest) et produit le résultat attendu ; en cas d'erreur, l'utilisateur reçoit un diagnostic actionnable. Complète avec :

- tests techniques de compilation, instanciation, exceptions et annulation ;
- tests transversaux de l'exécution via les API de stratégie autorisées si le scope inclut l'intégration au backtest ;
- tests de déchargement/versionnement lorsque l'implémentation le promet.

Aucun test ne doit laisser croire que l'AssemblyLoadContext empêche l'accès aux ressources OS.

## Livrable

Fournis le flux demandé, ses contrats, tests et exemples de script. Documente l'API exposée, les limites de sécurité et la différence entre isolation de chargement et sandbox.
