# Bonnes pratiques d'implémentation

Ces règles s'appliquent à toute modification de l'application financière. Elles expliquent concrètement à une IA de développement comment intervenir dans un dépôt existant.

## 1. Commencer par comprendre le dépôt

1. Lire [BusinessContext.md](./BusinessContext.md) et le prompt de fonctionnalité sélectionné.
2. Examiner les solutions et projets, leurs fichiers de configuration, les dépendances et les conventions locales.
3. Rechercher les contrats, composants et tests existants avant d'ajouter une nouvelle abstraction.
4. Vérifier le framework cible, les versions de .NET et des packages NuGet, ainsi que les commandes de build et de test documentées.
5. Préserver les changements préexistants. Ne pas supprimer ni réécrire du code sans nécessité pour la demande.

Si le dépôt contredit une hypothèse des documents, prendre le dépôt comme état réel du système, expliquer l'écart et éviter une migration implicite.

Respecter l'organisation et l'ordre de développement indicatifs décrits dans [BusinessContext.md](./BusinessContext.md). Utiliser la feuille de route comme graphe de dépendances : vérifier les prérequis avant une tranche de travail, mais ne pas reconstruire les phases déjà satisfaites ni imposer un ordre artificiel lorsqu'un incrément peut être réalisé proprement.

## 2. Cadrer chaque modification

- Chaque modification doit rester **aussi locale que possible** : modifier le composant responsable et le plus petit ensemble de fichiers nécessaire, sans étendre spontanément le périmètre à des modules voisins.
- Avant de coder, suivre le comportement concerné dans le code : qui l'appelle, de quelles fonctionnalités il dépend, quelles interfaces/configurations/données il affecte et quel résultat visible côté utilisateur ou métier doit changer. Examiner les dépendances et usages existants, pas uniquement le fichier ciblé.
- Vérifier à la fois la cohérence locale (conventions et contrats du composant) et les impacts de bout en bout (appelants, consommateurs, persistance, configuration, plugins, UI, audit, performances et compatibilité).
- Définir un périmètre explicite : comportement visé, composants qui doivent changer, composants qui ne doivent pas changer et validations qui démontreront le résultat. Implémenter uniquement ce périmètre et ses adaptations nécessaires.
- Si une solution propre nécessite de modifier l'architecture du composant ou de sa fonctionnalité (contrats, frontières, responsabilités, dépendances ou découpage), ou si la modification a des impacts globaux/transversaux au-delà de cette fonctionnalité, **arrêter avant ces changements et demander confirmation**. Expliquer le problème, les impacts, les options raisonnables et la recommandation. Ne pas effectuer ces changements silencieusement.
- Ne pas demander confirmation pour une adaptation directement nécessaire, locale et compatible avec l'architecture existante. Une fois le périmètre accepté, adapter tous les appelants, consommateurs et tests affectés afin que le comportement reste cohérent, maintenable, modulaire et local à la fonctionnalité.
- Si l'impact réel s'avère plus large que prévu pendant le travail, suspendre l'élargissement et demander confirmation avant de continuer au-delà du périmètre accepté.
- Découper une demande trop vaste en incréments compilables et testables ; ne pas créer une solution entière en un seul bloc.
- Avant un choix architectural irréversible ou une décision métier qui affecte les résultats financiers, poser une question ciblée.
- Ne pas combler les lacunes par des comportements présentés comme des faits métier.
- Signaler les dépendances externes, credentials, accès réseau ou services locaux requis.

## 3. Cible .NET et dépendances NuGet

- Écrire l'application et ses projets sous .NET 10 ; conserver les TFM appropriés à leur rôle, par exemple `net10.0` pour les bibliothèques et `net10.0-windows` pour WPF.
- Pour chaque projet concerné par une modification, examiner les références NuGet directes existantes ainsi que tout nouveau package envisagé. Privilégier leur dernière version stable disponible compatible avec .NET 10, le framework ciblé et les autres dépendances ; ne pas choisir une préversion sauf demande explicite.
- Avant d'ajouter un package, rechercher si les dépendances existantes ou les APIs .NET couvrent déjà le besoin.
- Si une version récente ne satisfait pas le besoin ou n'est pas compatible, examiner et mettre à jour les dépendances directement concernées (y compris leurs dépendances liées si nécessaire), puis vérifier la compatibilité, les breaking changes et le comportement métier.
- Si la dernière version ne peut toujours pas être utilisée sans enfreindre une contrainte technique, casser la compatibilité ou étendre le changement au-delà du périmètre accepté, retenir la version stable la plus récente compatible. Documenter la contrainte, la version choisie et les conséquences ; ne pas forcer une version incompatible.
- Une mise à jour NuGet peut affecter plusieurs composants : analyser le graphe de dépendances, usages, build et tests. Si elle nécessite une migration globale/transversale ou un impact architectural dépassant le périmètre accepté, demander confirmation avant de l'entreprendre.
- Ne pas mettre à jour sans discernement tous les packages sans rapport avec le changement. Les mises à jour doivent soutenir la demande et respecter l'exigence générale de versions récentes compatibles.
- Après toute modification de dépendance, restaurer les packages, exécuter les tests pertinents et compiler les projets concernés ; signaler les versions utilisées et tout échec ou avertissement restant.

## 4. Architecture et code

### Philosophie de traitement vectoriel

- Gérer par défaut les informations sous forme de collections ou de lots cohérents dès qu'un même flux métier concerne plusieurs instruments, dates, requêtes, observations, courbes, scénarios ou résultats. Concevoir les contrats publics pour exprimer ce lot directement, plutôt que d'obliger les appelants à lancer une suite d'opérations scalaires.
- Appliquer ce principe à tout le produit : ingestion et market data, requêtes de pricing, jeux de scénarios, simulation et résultats de backtest, persistance/cache, scripting, plugins, observabilité et interface.
- Garder le dispatch technique interne aux adaptateurs et workflows. Par exemple, un connecteur peut répartir un lot par symbole, classe d'actif ou endpoint fournisseur sans exposer cette mécanique à l'appelant ; le référentiel demeure l'autorité pour les identifiants canoniques, les fournisseurs, les classes d'actifs et leurs périodes de validité.
- Préserver l'identité, l'ordre, les unités, les timestamps et la provenance de chaque élément. Définir explicitement l'échec du lot et les diagnostics par élément ; ne pas ignorer silencieusement les entrées invalides ou non résolues.
- Un lot ne signifie pas un calcul SIMD, une exécution parallèle ni un batch sans limite. Respecter les contraintes métier, les quotas fournisseurs, l'annulation et les limites de ressources ; ne paralléliser que si le comportement et ses garanties sont adaptés et testés.
- Utiliser une opération scalaire uniquement lorsqu'elle est intrinsèque au domaine ou imposée par une frontière technique ; documenter ce motif et offrir une surface vectorielle aux consommateurs lorsqu'ils traitent plusieurs éléments.

- Respecter l'architecture, les noms, le formatage et les helpers existants.
- L'usage de `Semaphore` et `SemaphoreSlim` est interdit dans toute l'application, tous ses projets et ses tests, y compris via un helper ou une bibliothèque qui les emploie pour la synchronisation. Ne pas utiliser `Wait`, `WaitAsync` ou `Release` sur ces primitives. Pour respecter une limite d'appels distante, utiliser une politique de retry avec délai et annulation, pas un sémaphore ou un verrou de cadence ; pour les autres besoins de coordination, choisir un mécanisme sans sémaphore adapté au flux et documenter ses garanties.
- Pour les structures de contrôle dont le langage autorise un corps sans accolades (`if`, `else`, `for`, `foreach`, `while`), omettre les accolades lorsque le corps contient une seule instruction. Placer la condition et l'instruction sur des lignes distinctes, avec l'instruction indentée sous la condition ; ne pas écrire de code compact sur une seule ligne.
- Ne pas omettre les accolades lorsqu'elles sont requises par la syntaxe ou nécessaires pour grouper plusieurs instructions : préserver le comportement et utiliser alors un bloc multilignes correctement indenté. Cette règle ne concerne pas les accolades de déclaration (classes, méthodes, namespaces, etc.) ni les initialiseurs.
- En C#, organiser les membres des classes en régions `#region` nommées selon leur responsabilité ou fonctionnalité afin de repérer rapidement les méthodes et comportements. Utiliser des groupes cohérents et lisibles, sans créer une région par membre ni remplacer le découpage en classes lorsque les responsabilités sont distinctes.
- Pour les classes et records de données, déclarer explicitement chaque propriété avec ses accesseurs. Préférer `required init` pour les propriétés obligatoires et `init` pour les propriétés facultatives, afin que leur forme soit visible et leur initialisation explicite par object initializer. Ne pas utiliser un constructeur (y compris primaire) uniquement pour transférer ses paramètres vers des propriétés ; conserver un constructeur lorsque son comportement ou ses validations ne s'expriment pas correctement dans les accesseurs `init`.
- Pour les réponses structurées d'un système externe, définir des DTO typés internes à l'adaptateur, les désérialiser via le service partagé, puis valider et mapper explicitement vers les modèles métier. Ne pas exposer les DTO hors de l'adaptateur ni utiliser une lecture directe de `JsonElement` comme substitut à un modèle DTO.
- Un DTO temporaire utilisé uniquement pour désérialiser ou sérialiser un contrat d'API externe est un artefact technique, pas un modèle de données métier. Le garder dans le projet de l'adaptateur propriétaire, dans une zone technique appropriée (par exemple `Serialization` ou `Transport`), et ne jamais le placer dans un projet/dossier `Models` ni l'exposer comme modèle métier public. Réserver `Models` aux modèles métier partagés et provider-neutral.
- Appliquer SOLID et le principe de responsabilité unique sans multiplier les projets, couches ou interfaces inutiles.
- Chaque projet doit avoir un nom qui révèle une seule responsabilité technique ou métier, clairement délimitée (par exemple persistance SQL, cache Redis, workflow d'ingestion, moteur de pricing, modèle métier, contrat/extension technique, application WPF ou ressources UI). Ne pas réunir par commodité des capacités indépendantes (par exemple persistance et cache, workflow et moteur, logique métier et UI) dans un projet fourre-tout.
- Regrouper les projets sous des répertoires thématiques tels que `Core`, `Data`, `Pricing`, `Backtesting`, `Scripting`, `Extensions` ou `UI`, en cohérence avec l'organisation existante. Les répertoires thématiques regroupent des projets ; ils ne justifient pas de fusionner leurs responsabilités.
- Avant d'ajouter un projet, expliquer son rôle, son propriétaire et ses dépendances. Créer un projet séparé lorsque la responsabilité est indépendante et identifiable ; ne pas créer de projet vide ou de découpage artificiel sans besoin concret.
- Les briques purement techniques peuvent être réutilisées par plusieurs fonctionnalités métier ou UI. Les placer dans un projet technique partagé au nom explicite lorsque plusieurs consommateurs réels les justifient ou qu'une capacité transversale explicitement demandée a des consommateurs prévus identifiables ; garder ces briques indépendantes des règles métier, sans dépendances circulaires et sans en faire un fourre-tout `Common`.
- Avant de créer ou de réimplémenter un helper, convertisseur, validateur ou mécanisme technique, rechercher son propriétaire dans toute la solution. Une responsabilité technique transversale doit avoir une implémentation faisant autorité et être consommée via son API partagée, plutôt que recodée par fonctionnalité.
- Centraliser les mécanismes génériques dans un projet dédié de responsabilité explicite lorsqu'ils servent plusieurs consommateurs réels ou constituent une capacité transversale demandée avec des consommateurs prévus identifiables. Ne pas créer un socle abstrait sur la seule base d'une similarité supposée.
- Partager le mécanisme sans déplacer la sémantique spécifique : un service JSON commun possède la configuration et les opérations génériques de sérialisation/désérialisation ; chaque adaptateur conserve ses DTO fournisseur, la validation de son schéma externe et le mapping vers les modèles métier. Les projets partagés ne doivent pas dépendre des modèles de fournisseurs ou des règles d'une fonctionnalité.
- Tester le comportement au projet propriétaire et tester son intégration dans les projets consommateurs. Toute implémentation parallèle doit être justifiée par une frontière ou contrainte réelle et documentée.
- Développer les fondations selon l'ordre réel des dépendances : stockage/données avant ingestion qui en dépend, données et contrats nécessaires avant les moteurs consommateurs, workflows avant leur UI, et instrumentation structurée avant les vues d'observabilité. Le détail de la séquence est dans le contexte métier ; adapter seulement si le dépôt ou la tranche verticale l'autorise clairement.
- Pour une tranche fonctionnelle, relier les projets concernés par des contrats explicites et orienter les dépendances vers les règles métier ; ne pas exposer les détails internes d'un projet à tous les autres.
- Séparer les règles métier des détails d'interface, de stockage, de réseau et de fournisseur.
- Garder la dépendance des couches orientée vers les contrats et le cœur métier ; les adaptateurs techniques implémentent ces contrats.
- Utiliser `IServiceCollection` et l'injection de dépendances selon les patterns existants.
- Une classe par fichier, namespaces reflétant l'organisation retenue par la solution, sauf convention existante différente.
- Préférer les types explicites, les validations et les erreurs métier/techniques utiles aux casts non sûrs, aux valeurs par défaut trompeuses ou aux échecs silencieux.
- Ajouter une documentation XML aux membres publics conformément aux exigences du projet.
- Éviter les commentaires qui répètent le code ; documenter uniquement les décisions ou contraintes non évidentes.
- Garder les paramètres de configuration au plus près de la fonctionnalité propriétaire ; ne pas faire dépendre une fonctionnalité des options privées d'une autre.
- Lorsqu'un paramètre doit réellement être partagé, déclarer et documenter explicitement le couplage dans le code, la documentation et chaque fichier de configuration concerné.

## 5. Fiabilité et erreurs

- Valider les entrées aux frontières du système : API, fichiers, scripts, UI et messages de tâche.
- Distinguer une donnée manquante, invalide, périmée et une défaillance fournisseur ; ne pas les convertir en succès ou en valeur financière par défaut.
- Propager les erreurs avec le contexte utile et employer le mécanisme de logs/erreurs établi dans la solution.
- Ne pas utiliser de catch général qui masque l'erreur. Intercepter uniquement lorsque le code peut traiter, enrichir ou convertir explicitement l'échec.
- Propager les `CancellationToken` aux opérations asynchrones et respecter l'annulation.
- Configurer retry, backoff et circuit breaker uniquement aux frontières appropriées ; ne pas répéter des opérations non idempotentes sans garantie.
- Protéger les ressources et fermer/disposer correctement les flux, connexions et contextes.

## 6. Exactitude financière et données

- Vérifier les unités, devises, fuseaux horaires, calendriers, conventions et hypothèses d'un calcul dans le code ou dans les spécifications.
- Préserver la précision et les valeurs manquantes ; ne pas arrondir, convertir ou interpoler implicitement.
- Définir et tester les cas limites pertinents : zéro, valeurs négatives si admises, limites de domaine, données périmées, séries désordonnées ou incomplètes.
- Les algorithmes numériques doivent avoir des tests de référence, des cas limites et, quand pertinent, des tests de convergence ou de propriétés.
- N'affirmer une précision ni une performance qu'après validation par un test reproductible avec tolérance et conditions documentées.
- Pour les simulations, documenter les hypothèses de frais, slippage, latence, ordre des événements et déterminisme.

## 7. Sécurité

- Ne jamais écrire ou committer de secrets, tokens, clés API ou données personnelles.
- Utiliser les mécanismes de configuration et de chiffrement déjà présents ; ne pas inventer une cryptographie maison.
- Traiter comme non fiable tout fichier importé, contenu réseau, plugin et script.
- `AssemblyLoadContext` isole le chargement d'assemblies, mais ne fournit pas une sandbox de sécurité pour du code arbitraire.
- Éviter les journaux contenant des secrets, des credentials ou des données sensibles.
- Enregistrer les événements d'audit exigés sans altérer leur ordre ni leurs données essentielles.

## 8. Configuration de l'application

- Privilégier des fichiers JSON locaux (stockés et chargés sur la machine) spécialisés par domaine fonctionnel, par exemple un fichier de configuration dédié à l'ingestion, un au pricing et un au backtest. Aucun service centralisé n'est présumé. Respecter le découpage déjà présent s'il existe.
- Chaque fichier et chaque section doivent avoir un propriétaire clair. Une modification de la configuration d'une fonctionnalité ne doit pas modifier le comportement d'une autre par effet de bord.
- Utiliser des sections/options fortement typées et validées au démarrage ou au chargement, ainsi que les mécanismes de configuration standard du projet. Éviter les lectures ad hoc de fichiers dispersées dans les composants métier.
- Ne pas utiliser de variables d'environnement pour la configuration de l'application, y compris les clés API et autres credentials. Charger ces valeurs depuis les fichiers JSON locaux spécialisés et les lier à des options typées injectées par DI.
- Toute valeur déterminée par l'environnement, le déploiement ou un choix de fournisseur, ainsi que tout réglage qui doit rester fixe pendant la durée d'exécution après le démarrage, doit provenir du fichier JSON local spécialisé de sa fonctionnalité, être liée à des options typées et être fournie via l'injection de dépendances. Ne hardcode pas ces réglages dans des `const` ou des champs privés.
- Distinguer ces réglages des invariants de protocole, des constantes mathématiques et des valeurs intrinsèques à une règle métier, qui restent dans le code. Ne rends pas chaque constante configurable sans besoin fonctionnel. N'active pas le rechargement à chaud implicitement ; choisis un contrat d'options adapté au cycle de vie demandé et documente-le.
- Ne pas dupliquer une valeur globale dans plusieurs fichiers si cela crée des divergences. Si un réglage partagé est nécessaire, le rendre explicitement commun, indiquer les fonctionnalités qui en dépendent et documenter ce couplage dans le code, la documentation et les fichiers de configuration.
- Traiter les secrets séparément : ne jamais inscrire de secret dans un fichier suivi par Git ni dans un fichier JSON d'exemple. Les secrets de runtime peuvent être conservés en clair dans un fichier JSON local non suivi, dont l'accès est restreint au compte de l'application. En CI/CD, fournir ce JSON via un stockage/artefact de secrets protégé et le rendre disponible comme fichier au runtime ; ne pas injecter sa valeur par variable d'environnement. Ne pas prétendre que l'exclusion Git chiffre le fichier.
- Définir et tester les valeurs absentes, invalides, par défaut et les changements de configuration à chaud si cette capacité est demandée. Ne pas appliquer silencieusement une valeur de repli qui modifie les résultats métier.

## 9. Observabilité et audit

- Rendre les opérations de chaque fonctionnalité observables : ingestion/récupération de données, pricing, compilation et exécution de scripts, backtest, persistance et autres opérations coûteuses ou métier importantes.
- Utiliser des logs structurés pour les faits diagnostiques, traces/spans pour les étapes et durées, métriques agrégées pour les tendances et événements d'audit pour les actions techniques ou métier qui doivent être attribuables.
- Corréler les étapes d'une opération avec des identifiants de trace/run et des noms de fonctions stables. Ne pas utiliser d'identifiants non bornés (ticker, texte de requête, identifiant arbitraire) comme labels de métriques.
- Distinguer audit métier (qui a demandé quelle action métier, sur quelle cible, quand et avec quel résultat) du diagnostic technique (étapes, durées, erreurs). N'enregistre pas chaque message interne comme un événement d'audit.
- Concevoir la collecte pour limiter mémoire et stockage : privilégier les événements de début/fin, changements d'état importants, erreurs et résumés ; agréger ou échantillonner les traces détaillées lorsque c'est acceptable.
- Conserver, pour chaque run observé, un minimum de spans structurés représentant toutes ses étapes significatives afin que son graphe d'exécution reste consultable. Appliquer l'échantillonnage aux détails de bas niveau non essentiels, jamais aux événements d'audit obligatoires.
- Définir les limites de file tampon, la politique de rétention/rotation, le niveau de détail configurable et le comportement en cas de collecteur indisponible. Ne jamais bloquer ou faire échouer un calcul métier uniquement parce qu'une télémétrie diagnostique secondaire est saturée ; les événements d'audit obligatoires doivent suivre leur garantie documentée.
- Éviter de journaliser secrets, données personnelles ou volumes complets de données de marché. Préférer des résumés et des références sûres aux payloads détaillés.
- Fournir un moyen d'afficher logs et exécutions dans la GUI à partir des données instrumentées ; ne pas reconstruire des graphes à partir de texte libre de logs si des spans/événements structurés peuvent être utilisés.

## 10. Tests et validation

Les tests doivent d'abord démontrer que l'application remplit correctement son rôle métier. Les tests techniques viennent soutenir cette preuve ; ils ne la remplacent pas.

1. **Commencer par le résultat métier** : identifier le cas d'usage ou le flux utilisateur visé, son résultat attendu et ses critères d'acceptation. Ajouter ou adapter un test métier/intégration qui vérifie ce résultat à travers les responsabilités réellement nécessaires, sans coupler le test à des détails d'implémentation sans intérêt métier.
2. **Ajouter les tests techniques appropriés au changement** :
   - *Responsabilité technique* : tests unitaires ciblés d'une transformation, validation, sérialisation, mapping ou manipulation de données.
   - *Responsabilité technico-métier* : tests d'un calcul ou comportement soumis à une règle métier mesurable, par exemple pricing déterministe, exactitude mathématique par rapport à une référence, ou latence acceptable pour le scénario et le volume concernés.
   - *Responsabilité transversale* : tests d'intégration d'un plugin, d'un workflow ou d'une fonctionnalité qui appelle plusieurs modules/fonctionnalités dépendantes et doit produire le résultat métier attendu.
3. Couvrir les erreurs, limites, annulations, compatibilités et régressions qui peuvent changer le résultat métier. Choisir la couche de test la plus pertinente ; éviter de multiplier des tests redondants qui vérifient uniquement la même implémentation.

- Suivre les frameworks, fixtures, conventions de nommage et stratégies de mock déjà présents.
- Garder les tests reproductibles : graine contrôlée, clock injectée, données fixes et doubles pour les dépendances externes lorsque cela convient.
- Tester l'isolation des configurations entre fonctionnalités, la validation des fichiers JSON et le diagnostic d'une configuration invalide si le changement touche la configuration.
- Tester corrélation et forme des événements/traces, affichage du statut d'un run et limites de mémoire/rétention si l'observabilité est modifiée.
- Exécuter d'abord les tests métier qui couvrent le changement, puis les tests techniques ciblés pertinents ; compléter par build/type-check/lint si la modification l'exige.
- Ne pas remplacer une vraie validation du comportement métier par le fait que le code « semble compilable » ou par des tests de détails internes uniquement.
- Rapporter les commandes exécutées, les résultats métier observés et les validations impossibles (ex. service Redis absent).

## 11. Documentation et livraison

- Mettre à jour la documentation directement liée au comportement modifié.
- Fournir un exemple d'utilisation pour toute fonctionnalité nouvellement exposée lorsque cela est pertinent.
- Documenter configuration, prérequis, limites, hypothèses et étapes de validation.
- Documenter chaque fichier JSON, la fonctionnalité propriétaire de ses paramètres, les valeurs par défaut et les éventuels couplages partagés.
- Documenter les catégories d'audit/observabilité, la corrélation, les règles de rétention et la manière de consulter les runs dans la GUI.
- Garder les diagrammes et guides alignés avec le code réellement livré.
- Terminer par un résumé du périmètre, des fichiers importants, des tests exécutés et des décisions ou limites restantes.
