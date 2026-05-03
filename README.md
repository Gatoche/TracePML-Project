# TracePML

App **wipiSoft** qui surveille le fichier `pml.log` de **WinPharma** (logiciel de
pharmacie) pour émettre des notifications toast lors des changements de statut
de commande vers le fournisseur (CERP / OCP / autres grossistes pharmaceutiques
parlant le protocole CSRP / pharmaML).

## Origine

TracePML est la fusion de deux projets précédents (conservés en archive dans le repo) :

- **`gTracePML-Old/`** — version Delphi historique (`fgTracePML.main.pas`).
  Surveillait `pml.log` et affichait un panneau de diagnostic.
- **`Notify-Old/`** — version Windows Forms C# qui gérait les notifications
  toast (popup Windows custom), indépendamment du parsing PML.

La version actuelle (`TracePML/`) refond le tout en **WPF .NET 8** avec
architecture MVVM, parsing XML moderne (`System.Xml.Linq`), et affichage
de notifications via la lib **Hardcodet.NotifyIcon.Wpf** (systray).

## Ce que fait l'app

1. **Surveille** `C:\WPHARMA\TELECOM\pml.log` (FileSystemWatcher avec debounce 500 ms)
2. **Lit le delta** depuis la dernière position (le fichier est append-only par WinPharma)
3. **Parse** les blocs CSRP : en-tête `==xxxxx==Request|Response XML-body...`
   suivi du XML inscrit entre `<?xml ...>` et `</CSRP_ENVELOPPE>`
4. **Identifie le type** de message :
   - **Émission** : `COMMANDE`, `REQ_INFO_PRODUIT`, `ACTION=ACQUITTEMENT`, `ACTION=VIDAGE`
   - **Réception** : `REP_COMMANDE`, `REP_INFO_PRODUIT`, `BON_LIVRAISON`,
     `ACTION=FIN_SERVICE`, `ERREUR`
5. **Mémorise la dernière commande envoyée** (référence + quantité) pour la
   comparer à la `REP_COMMANDE` correspondante
6. **Émet une notification** quand `REP_COMMANDE` matche la dernière commande :
   - **Confirmée** : `qtyLivrée == qtyCmde`
   - **Modifiée** : `qtyLivrée < qtyCmde` (avec affichage du motif `Additif`
     d'`INDISPONIBILITE` quand un seul produit)
   - **Annulée** : `qtyLivrée == 0`
7. **Affiche un toast** custom (`ToastBanner`) avec titre + détail

## UI

- **Systray** par défaut (icône `tracepml.ico`), démarrage silencieux
- Menu contextuel systray → "Quitter"
- Double-clic systray → ouvre la fenêtre principale (mode Debug)
- **Fenêtre principale** (mode Debug uniquement) :
  - Onglet **PML** : affichage filtré du log parsé (filtres par type
    d'émission / réception)
  - Onglet **Toast** : preview de toast (Confirmé / Modifié / Annulé) +
    log diagnostique de l'app

## Persistance

`HKCU\Software\wipiSoft\TracePML\` :
- `TestMode` (bool) : désactive le monitoring de `pml.log` (mode dev)
- `LocalTest` (bool) : utilise un `pml.log` local au lieu de
  `C:\WPHARMA\TELECOM\pml.log` (pour tests sans WinPharma)

## Architecture (MVVM)

```
TracePML/
├── App.xaml.cs           ← Entry point + DI manuelle + systray
├── Models/
│   ├── PmlMessageType.cs ← enum (Order, OrderResponse, Acknowledgement, …)
│   ├── OrderStatus.cs    ← enum (Confirmed | Modified | Cancelled)
│   ├── OrderNotification.cs ← record (Status, Title, Detail)
│   └── LogEntry.cs       ← record (RawHeader, MessageType, Summary, IsRequest)
├── Services/
│   ├── PmlFileMonitor.cs ← FileSystemWatcher + debounce + lecture delta
│   ├── PmlLogParser.cs   ← split blocs CSRP + dispatch Request/Response
│   ├── PmlXmlParser.cs   ← parsing XML (TryParseOrder, TryParseOrderResponse, …)
│   ├── ToastService.cs   ← rendu toast (ToastWindow / ToastBanner)
│   └── RegistrySettings.cs ← persistance HKCU
├── ViewModels/
│   └── MainViewModel.cs  ← INotifyPropertyChanged + RelayCommand
└── Views/
    ├── MainWindow.xaml(.cs) ← tabs PML + Toast (Debug only)
    ├── ToastWindow.xaml(.cs) ← fenêtre du toast
    └── ToastBanner.xaml(.cs) ← contenu du toast (titre + détail)
```

## Stack technique

- **.NET 8** + **WPF** (`net8.0-windows`, `UseWPF=true`)
- `OutputType=WinExe` (pas de console)
- `Hardcodet.NotifyIcon.Wpf` 1.1.0 (systray + toasts)
- Helpers `_libs` (source-linked) :
  - `WpsDebugSender` (logging UDP vers wipiLOG)
  - `WpsXamlAdjust` (éditeur runtime de propriétés XAML, debug only)
  - `WpsHKCU` (HKCU helpers)

## Évolution prévue

**Refactor en ModuleService wipiSoft** : TracePML est conceptuellement parfait
pour devenir un **ModuleService** (app headless invocable par un host
orchestrateur — ModuloSlot, futur wipiTools).

- **Daemon** : lancé au démarrage du host, surveille `pml.log` en arrière-plan
- **Settings window** : la fenêtre actuelle (mode Debug) devient la "fenêtre de
  paramétrage" exposée via `WpsModuleService.RegisterSettingsWindow(...)`
- **Toast** : reste local au service (pas besoin de remonter au host)
- **Invoke** futur : exposer `GetLastNotification()`, `GetStats()` pour qu'un
  module hôte puisse interroger le service

Le mutex `TracePML_SingleInstance` sera retiré (le host gère l'unicité via le
session ID Wps.Module.Sdk).

## Dev

```
cd TracePML
dotnet build
```

Pas de tests unitaires pour l'instant. Les services parsing sont assez purs
pour être testables facilement quand le besoin viendra.
