program gTracePML;

uses
  Vcl.Forms,
  muCheckPrevious,
  fgTracePML.main in 'fgTracePML.main.pas' {frmMain} ,
  gTracePML.utils in 'gTracePML.utils.pas',
  gTracePML.monitoring in 'gTracePML.monitoring.pas';

{$R *.res}


begin
//  ReportMemoryLeaksOnShutdown := True;
  If Not muCheckPrevious.RestoreIfRunningAndSendMessage(Application.Handle, ' ') Then
  begin
    Application.Initialize;
    Application.MainFormOnTaskbar := False;
    Application.CreateForm(TfrmMain, frmMain);
    Application.Run;
  end;

end.
