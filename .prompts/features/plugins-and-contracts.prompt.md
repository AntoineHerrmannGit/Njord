---
description: "Créer ou faire évoluer le système de plugins MEF et ses contrats versionnés"
name: "Plugins et contrats"
argument-hint: "Précise le type de plugin, le contrat ou le scénario de compatibilité à traiter"
agent: "agent"
---

# Tâche : plugins et contrats versionnés

Lis [le contexte métier](../BusinessContext.md) et [les pratiques d'implémentation](../EngineeringPractices.md), puis examine les projets, contrats d'extension, mécanismes de découverte et packages déjà présents.

Applique le cadrage de portée et d'impact décrit dans les pratiques communes : concentre le changement sur le point d'extension demandé, identifie tous ses consommateurs et demande confirmation avant de changer un contrat partagé, la stratégie de versionnement ou l'architecture de découverte avec un impact sur les plugins existants.

Les projets hôtes et contrats restent ciblés sur .NET 10 et les packages NuGet MEF/extension doivent être aux versions stables les plus récentes compatibles. Mets à jour les dépendances directement concernées si besoin ; si la dernière version reste incompatible avec les contraintes de plugins, utilise la plus récente version compatible et documente les conséquences.

Introduis les contrats plugins au moment où les points d'extension concrets sont identifiés, avant d'implémenter les plugins qui en dépendent ; ne bloque pas les fonctionnalités internes sur un système MEF spéculatif. Organise contrats et découverte sous `Extensions` ou `Core` selon leur responsabilité, sans les confondre avec les workflows métier.

## Résultat métier

Permettre à l'application de découvrir dynamiquement des modules utilisateur avec MEF, d'intégrer des modules lourds sous forme de plugins et de faire évoluer leurs contrats sans rendre les extensions existantes incompatibles de manière imprévisible.

## Consignes d'implémentation

- Commence par identifier les points d'extension concrets et les besoins des modules. Ne transforme pas chaque service interne en plugin.
- Définis des contrats petits, explicites et stables, placés dans une assembly de contrats dédiée uniquement si le découpage actuel le justifie.
- Versionne les contrats de manière vérifiable. Définis comment une version est déclarée, comment l'hôte vérifie la compatibilité et ce qui arrive à un plugin incompatible ; ne laisse pas l'échec de découverte devenir silencieux.
- Garde la découverte MEF derrière un composant d'infrastructure, afin que le cœur métier ne dépende pas de l'implémentation MEF.
- Valide les métadonnées, identifiants, versions et dépendances d'un plugin avant activation. Détecte et rapporte les collisions ou erreurs de chargement avec le nom du module et un diagnostic utile.
- Gère explicitement cycle de vie, activation/désactivation, ressources et annulation lorsque ces capacités sont nécessaires.
- Ne prétends pas qu'un plugin chargé dans le processus est isolé ou sûr parce qu'il utilise MEF. Si les plugins ne sont pas entièrement fiables, ne les exécute pas in-process sans une frontière de sécurité adaptée.
- Garde les connecteurs fournisseurs enfichables selon le même principe, mais ne duplique pas leur logique d'acquisition : voir [le prompt d'ingestion](./data-ingestion.prompt.md).

## Tests et critères de validation

Commence par un test transversal du cas d'usage métier : le module compatible est découvert, activé et fournit le comportement attendu à la fonctionnalité qui l'utilise. Vérifie qu'un module rejeté est diagnostiqué et ne crée pas de résultat partiel trompeur. Complète avec :

- tests techniques de découverte d'un plugin valide, d'absence de plugin et de plusieurs plugins ;
- tests de contrat incompatible, métadonnées invalides, identifiant dupliqué et assembly défectueuse ;
- tests de compatibilité entre versions supportées et tests du cycle de vie si celui-ci est implémenté.

- Vérifie build et tests ciblés, avec les versions réelles des packages MEF utilisés.

## Livrable

Fournis le contrat et le comportement de découverte demandés, les tests et une documentation d'ajout de plugin. Précise les versions compatibles, les règles de rejet, les erreurs visibles et les limites de sécurité.
