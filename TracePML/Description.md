# TracePML

Service wipiSoft qui **surveille les communications PML/CSRP** entre WinPharma
et les fournisseurs grossistes pharmaceutiques (CERP, OCP, etc.) et **émet des
notifications toast** lors des changements de statut de commande.

## Fonctionnement

L'app surveille le fichier `C:\WPHARMA\TELECOM\pml.log` (write-only par
WinPharma) en lisant le delta à chaque modification. Elle parse les enveloppes
CSRP (XML wrappées entre des en-têtes texte) et identifie :

- **Émissions** : `COMMANDE`, `REQ_INFO_PRODUIT`, `ACQUITTEMENT`, `VIDAGE`
- **Réceptions** : `REP_COMMANDE`, `REP_INFO_PRODUIT`, `BON_LIVRAISON`,
  `FIN_SERVICE`, `ERREUR`

Pour chaque commande envoyée, TracePML mémorise la référence + la quantité
totale. À la réception de la `REP_COMMANDE` correspondante, elle compare
les quantités et émet une notification :

- **Commande validée** : tout livré
- **Commande modifiée** : partiellement livré (avec motif d'indisponibilité)
- **Commande annulée** : rien livré

## UI

- **Systray** par défaut (démarrage silencieux)
- **Mode Debug** : fenêtre principale avec onglet PML log filtré + onglet
  diagnostic toast (preview, log d'événements)

## Persistance

`HKCU\Software\wipiSoft\TracePML\TestMode` et `LocalTest` (bool) pour les
modes de développement.

## Méthodes exposées (à venir, refactor ModuleService)

- *(en cours de design)* `GetLastNotification()` — dernière notification émise
- *(en cours de design)* `GetStats()` — compteurs (commandes, réponses, erreurs)
