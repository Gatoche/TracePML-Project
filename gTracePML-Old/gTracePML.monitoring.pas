unit gTracePML.monitoring;

interface

uses
  Winapi.Windows,
  System.SysUtils,
  System.Classes;

type
  TThreadMonitoring = class(TThread)
  private
    { Déclarations privées }
    ListeAjouts: TStrings;
    HandleNotification: THandle;
    CheminEnCours: String;
    DateHeureVerification: TDateTime;
    FFichierASurveiller: string;
    FCheminASurveiller: string;
    FProcedure: TProc<string>;
    FileName: string;
    Procedure Notify;
  protected
    procedure Execute; override;
  public
    constructor Create(ACheminASurveiller, AFichierASurveiller: string; AProcedure: TProc<string>);
    // destructor Destroy; override;
  end;

implementation

constructor TThreadMonitoring.Create(ACheminASurveiller, AFichierASurveiller: string; AProcedure: TProc<string>);
begin
  inherited Create(True); // Create the thread suspended
  FCheminASurveiller := ACheminASurveiller;
  FFichierASurveiller := AFichierASurveiller;
  FProcedure := AProcedure;
  FreeOnTerminate := True; // Optional: Automatically free the thread when done
  Resume; // Start the thread
end;

// destructor TThreadSurveillance.Destroy;
// begin
// // Any cleanup code can go here
// ListeAjouts.Free;
// inherited Destroy;
// end;

procedure TThreadMonitoring.Execute;
Var
  Chemin: Array [0 .. 255] Of Char;
  Infos: TSearchRec;
begin
  // Initialisation ds données du Thread
  ListeAjouts := TStringList.Create;
  HandleNotification := INVALID_HANDLE_VALUE;
  CheminEnCours := '';

  // Boucle pincipale du Thread
  While Not Terminated Do
  Begin
    // Le chemin demandé à changé => On change la surveillance
    If FCheminASurveiller <> CheminEnCours
    Then
    Begin
      ListeAjouts.Clear;

      // Une demande est en cours, on libère le Handle
      If HandleNotification <> INVALID_HANDLE_VALUE
      Then
      Begin
        FindCloseChangeNotification(HandleNotification);
        HandleNotification := INVALID_HANDLE_VALUE;
        ListeAjouts.Add('<FIN>');
      End;

      // Prise en compte de la nouvelle demande
      CheminEnCours := FCheminASurveiller;
      If CheminEnCours <> ''
      Then
      Begin
        // Suppression du '\' final
        CheminEnCours := ExcludeTrailingPathDelimiter(CheminEnCours);
        // Création de la demande de notification
        HandleNotification := FindFirstChangeNotification(
          StrPCopy(Chemin, CheminEnCours), // Chemin à surveiler
          False, // Ne pas surveiller les sous-répertoires
          FILE_NOTIFY_CHANGE_LAST_WRITE); // FILE_NOTIFY_CHANGE_FILE_NAME +
        // Surveiller les écriture et changement de noms
        // Mémorisation de l'heure de la demande
        DateHeureVerification := Now;
        ListeAjouts.Add('<DEBUT:' + CheminEnCours + '>');
      End;

      // Synchronize(Notify);
    End;

    // Une demande de notification est en cours
    If HandleNotification <> INVALID_HANDLE_VALUE
    Then
    Begin
      // Il faut donc demander à Windows d'être prévenu en cas de modification
      // La sortie de WaitForSingleObject est effectuée dans le cas d'une notification
      // ou dans le cas d'un TimeOut. Il ne faut pas ici utiliser le timeout INFINITE
      // sinon le thread risque d'être bloqué en permanence.
      Case WaitForSingleObject(HandleNotification, 200) Of
        WAIT_OBJECT_0:
          Begin
            // Dans le cas d'une notification il faut rechercher
            // les fichiers modifiés depuis le dernier appel
            ListeAjouts.Clear;
            If FindFirst(CheminEnCours + PathDelim + '*.*', faAnyFile, Infos) = 0
            Then
            Begin
              Repeat
                // Le fichier à été modifié, on l'ajoute à la liste
                If FileDateToDateTime(Infos.Time) > DateHeureVerification
                Then
                Begin
                  FileName := StrPas(Infos.FindData.cFileName);
                  ListeAjouts.Add(FormatDateTime('DD/MM/YYYY HH:NN:SS', FileDateToDateTime(Infos.Time)) + '=' +
                    FileName);

                  Synchronize(Notify);
                End;
              Until FindNext(Infos) <> 0;
              FindClose(Infos);

              // Ajout des fichiers modifiés dans le Memo
              // Comme c'est un Thread, il n'est pas possible de modifier directement Memo1
              // Synchronize(Notify);
            End;
            // Mémorisation de l'heure en cours pour le prochain test
            DateHeureVerification := Now;
          End;
        WAIT_TIMEOUT:
          ;
      End;

      // Le handle doit être mis à jour pour pouvoir effectuer une nouvelle demande
      FindNextChangeNotification(HandleNotification);
    End;

    Sleep(1); // On redonne du temps au système ***PERSO
  End;

  // Libération du Handle en cas de besoin
  If HandleNotification <> INVALID_HANDLE_VALUE Then
    FindCloseChangeNotification(HandleNotification);
  HandleNotification := INVALID_HANDLE_VALUE;

  // Libération des objets
  ListeAjouts.Free;
end;

// Cette procédure ne doit être appelée que par l'intermédiaire de Synchronize
procedure TThreadMonitoring.Notify;
Begin
  if Assigned(FProcedure) then
    if (FileName <> '.') then
      FProcedure(FileName); // Execute the procedure in the context of the main thread
End;

end.
