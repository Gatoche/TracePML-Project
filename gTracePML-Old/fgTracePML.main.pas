unit fgTracePML.main;

interface

uses
  Winapi.Windows,
  Winapi.Messages,
  System.SysUtils,
  System.Variants,
  System.Classes,
  System.JSON,
  System.JSON.Serializers,
  System.Win.Registry,
  Vcl.Graphics,
  Vcl.Controls,
  Vcl.Forms,
  Vcl.Dialogs,
  Vcl.StdCtrls,
  Vcl.ExtCtrls,
  Vcl.Menus,
  Xml.XmlIntf,
  Xml.XMLDoc,
  Xml.XMLDom,
  Xml.omnixmldom,

  PiconeBarreTache,

  muConst,
  muBase,

  gTracePML.utils,
  gTracePML.monitoring;

type
  TfrmMain = class(TForm)
    mmoPML: TMemo;
    tmrLogParse: TTimer;
    Button1: TButton;
    mmoNotify: TMemo;
    lblCommande: TLabel;
    btnTest: TButton;
    ckbOrder: TCheckBox;
    ckbRequestInfoProduct: TCheckBox;
    ckbAcknowledgement: TCheckBox;
    ckbEmptying: TCheckBox;
    Label1: TLabel;
    Label2: TLabel;
    ckbOrderResponse: TCheckBox;
    ckbDispoInfoResponse: TCheckBox;
    ckbServiceEnd: TCheckBox;
    ckbDeliveryResponse: TCheckBox;
    pbt: TPiconeBarreTache;
    pmu: TPopupMenu;
    Q1: TMenuItem;
    Label3: TLabel;
    ckbTestMode: TCheckBox;
    rbtWinpharmaTest: TRadioButton;
    rbtLocalTest: TRadioButton;
    ckbDebugMode: TCheckBox;
    ckbResponseError: TCheckBox;
    lblSizeLog: TLabel;
    procedure FormCreate(Sender: TObject);
    procedure tmrLogParseTimer(Sender: TObject);
    procedure Button1Click(Sender: TObject);
    procedure btnTestClick(Sender: TObject);
    procedure Q1Click(Sender: TObject);
    procedure pbtDblClick(Sender: TObject);
    procedure ckbTestModeClick(Sender: TObject);
    procedure ckbDebugModeClick(Sender: TObject);
    procedure FormClose(Sender: TObject; var Action: TCloseAction);
  private
    procedure SavePreferences;
    procedure LoadPreferences;
    Procedure WndProc(Var Msg: TMessage); Override;
    procedure FormAfterCreate(Sender: TObject; var Done: Boolean);
    procedure Log(s: string);
    procedure LogParse;
    function OrderNotify(aStatus: TOrderStatus; aTitle: string; aDetail: string): Boolean;
  public
  end;

const
  ver = 'gTracePML 0.8 - ';
  KL = '>>           ';
  pathPML_WP = 'C:\WPHARMA\TELECOM\pml.log';

var
  frmMain: TfrmMain;
  bDebugMode: Boolean;
  // bTestMode: Boolean;
  AppName: string;
  pathPML: string;
  fss: TFileStream;
  fss_size: Integer;
  fs: TFileStream;
  fs_size_old: Integer;
  fs_size: Integer;
  PMLprov: String;
  isXML: Boolean;
  Xml: String;
  refLastOrder: string;
  qtyLastOrder: Integer;
  gTracePML_SHOW_WM: UINT;
  ThreadSurveillance: TThreadMonitoring;

implementation

{$R *.dfm}


procedure MonitorProc(FileName: string);
begin
  // frmMain.mmoCmde.Lines.Add(FileName);

  if (FileName = 'pml.log') then
    if not frmMain.ckbTestMode.Checked then
    begin
      // frmMain.mmoCmde.Lines.Add('*');
      if Assigned(fs) then
        try
          if fs_size < fs.Size then
          begin
            frmMain.lblSizeLog.Caption := IntToStr(fs.Size);
            fs_size := fs.Size;

            frmMain.tmrLogParse.Enabled := False;
            frmMain.tmrLogParse.Enabled := True;
          end

          else if fs_size > fs.Size then
          begin
            frmMain.lblSizeLog.Caption := IntToStr(fs.Size);
            fs_size := fs.Size;
          end;

        except
        end;
    end;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.SavePreferences;
var
  Reg: TRegistry;
begin
  Reg := TRegistry.Create;
  try
    Reg.RootKey := HKEY_CURRENT_USER;
    if Reg.OpenKey('Software\wipiSoft\' + AppName, True) then
    begin
      Reg.WriteString('TestMode', kBoolE[frmMain.ckbTestMode.Checked]);
      // ShowMessage('LocalTest:' + kBoolE[frmMain.rbtLocalTest.Checked]);
      Reg.WriteString('LocalTest', kBoolE[frmMain.rbtLocalTest.Checked]);
      // Reg.WriteInteger('Pref2', 123);
      Reg.CloseKey;
    end;
  finally
    Reg.Free;
  end;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.LoadPreferences;
var
  Reg: TRegistry;
  Pref1: string;
  Pref2: Integer;
begin
  Reg := TRegistry.Create;
  try
    Reg.RootKey := HKEY_CURRENT_USER;
    if Reg.OpenKey('Software\wipiSoft\' + AppName, False) then
    begin
      if Reg.ValueExists('TestMode') then
        frmMain.ckbTestMode.Checked := (UpperCase(Reg.ReadString('TestMode')) = UpperCase(kBoolE[True]));
      if Reg.ValueExists('LocalTest') then
      begin
        frmMain.rbtLocalTest.Checked := (UpperCase(Reg.ReadString('LocalTest')) = UpperCase(kBoolE[True]));
        frmMain.rbtWinpharmaTest.Checked := not frmMain.rbtLocalTest.Checked;
      end;

      // if Reg.ValueExists('Pref2') then
      // Pref2 := Reg.ReadInteger('Pref2');
      Reg.CloseKey;
    end;
  finally
    Reg.Free;
  end;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.Log(s: string);
begin
  if (s = '') then
    mmoPML.Lines.Add('')
  else
    mmoPML.Lines.Add(KL + s);
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.Button1Click(Sender: TObject);
begin
  if not OrderNotify(osModified, 'Ligne de Titre', 'Ligne de détail') then
    MessageDlg('Fenêtre Notify introuvable !', mtError, [mbOK], 0);
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.btnTestClick(Sender: TObject);
begin
  btnTest.Enabled := False;
  Application.ProcessMessages;

  try
    pathPML := IfThen(rbtWinpharmaTest.Checked, pathPML_WP, 'pml.log');

    if FileExists(pathPML) then
    begin
      fs := TFileStream.Create(pathPML, fmOpenRead or fmShareDenyNone);
      fs_size_old := 0; // fs.Size;
      lblSizeLog.Caption := IntToStr(fs.Size);

      // try
      LogParse;
      // except
      // on E: Exception do
      // ShowMessage(E.ClassName + ': ' + E.Message);
      // end;
    end;
  finally
    btnTest.Enabled := True;
  end;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.ckbDebugModeClick(Sender: TObject);
begin
  pbt.PetiteIconeVisible := ckbDebugMode.Checked;
  pbt.ApplicationCachee := not ckbDebugMode.Checked;
  pbt.ReduireSiFin := not ckbDebugMode.Checked;

  if not ckbDebugMode.Checked then
    ckbTestMode.Checked := False;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.ckbTestModeClick(Sender: TObject);
begin
  rbtWinpharmaTest.Enabled := ckbTestMode.Checked;
  rbtLocalTest.Enabled := ckbTestMode.Checked;
  btnTest.Enabled := ckbTestMode.Checked;
  mmoPML.Clear;
  mmoNotify.Clear;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.FormClose(Sender: TObject; var Action: TCloseAction);
begin
  If ThreadSurveillance <> Nil Then
    ThreadSurveillance.Terminate;

  Sleep(100);

  SavePreferences;

  if Assigned(fss) then
    fss.Free;

  if Assigned(fs) then
    fs.Free;

end;

// -----------------------------------------------------------------------------
procedure TfrmMain.FormCreate(Sender: TObject);
begin
  // Debogage ?
  bDebugMode := True;

  AppName := ChangeFileExt(ExtractFileName(Application.ExeName), '');
  gTracePML_SHOW_WM := RegisterWindowMessage('gTracePML_SHOW_WM');

  Application.OnIdle := FormAfterCreate;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.FormAfterCreate(Sender: TObject; var Done: Boolean);
begin
  Application.OnIdle := nil;
  // ...

  ThreadSurveillance := TThreadMonitoring.Create('C:\WPHARMA\TELECOM', '', MonitorProc);
  ckbDebugMode.Checked := bDebugMode;

  LoadPreferences;

  if not ckbTestMode.Checked then
  begin
    pathPML := pathPML_WP;
    if FileExists(pathPML) then
    begin
      fs := TFileStream.Create(pathPML, fmOpenRead or fmShareDenyNone);
      fs_size_old := fs.Size;
      lblSizeLog.Caption := IntToStr(fs.Size);
    end;
  end;
end;

// -----------------------------------------------------------------------------
Procedure TfrmMain.WndProc(Var Msg: TMessage);
Begin
  Inherited;
  If Msg.Msg = gTracePML_SHOW_WM Then
  Begin
    ckbDebugMode.Checked := True;
    // ShowMessage('Yes !');
  End;
End;

// -----------------------------------------------------------------------------
function TfrmMain.OrderNotify(aStatus: TOrderStatus; aTitle: string; aDetail: string): Boolean;
var
  order: TOrder;
  JsonString: UTF8String;
  serializer: TJsonSerializer;
  HWNDReceiver: HWND;
begin
  Result := False;

  mmoNotify.Lines.Add('Status: ' + TEnumConverter.EnumToString(aStatus));
  mmoNotify.Lines.Add('Title: ' + aTitle);
  mmoNotify.Lines.Add('Detail: ' + aDetail);

  with order do
  begin
    status := aStatus;
    title := aTitle;
    detail := aDetail;
  end;

  serializer := TJsonSerializer.Create;
  try
    JsonString := serializer.Serialize(order);
  finally
    serializer.Free;
  end;

  HWNDReceiver := FindWindow(nil, PChar('Notify')); // Assurez-vous que le nom de la fenêtre est correct

  if HWNDReceiver = 0 then
    Exit;

  SendCopyDataMessage(Handle, HWNDReceiver, JsonString);

  Result := True;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.pbtDblClick(Sender: TObject);
begin
  Show;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.Q1Click(Sender: TObject);
begin
  frmMain.Close;
  Application.Terminate;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.tmrLogParseTimer(Sender: TObject);
begin
  tmrLogParse.Enabled := False;

  if fs_size_old < fs.Size then
    LogParse;
end;

// -----------------------------------------------------------------------------
procedure TfrmMain.LogParse;
var
  ss: TStringStream;
  sl: TStrings;
  update: UTF8String;
  pXML: Integer;
  doc: IXMLDocument;
  res: string;
  error: string;

  // mmoPML: TMemo;
  req: string;
  prov: string;
  bPMLClear: Boolean;
  provCmde, provRepCmde: String;
  codeCmde, codeRepCmde: string;
  qtyCmde, qtyRepCmde: string;
  parseError: Boolean;
  cmdMulti: Boolean;
  nbProd: Integer;
  numLigne: Integer;
  maxLigne: Integer;
  i, x: Integer;
  header: string;
  refClientOrder: string;
  qtyOrderProducts: Integer;
  qtyOrderUnits: Integer;
  refTour: string;
  dateTour: string;
  availabilityMessage: string;
  prodLabel: string;
  detailResponse: string;
  motif: string;

  aStatus: TOrderStatus;
  aTitle: string;
  aDetail: string;

  procedure mmoPMLClear;
  begin
    if not bPMLClear then
    begin
      bPMLClear := True;
      mmoPML.Clear;
      mmoNotify.Clear;
    end;
  end;

begin
  parseError := False;
  bPMLClear := False;

  ss := TStringStream.Create('');
  sl := TStringList.Create;

  mmoPML.Lines.BeginUpdate;
  mmoNotify.Lines.BeginUpdate;

  try
    // Beep;

    fs.Position := fs_size_old;
    ss.CopyFrom(fs, fs.Size - fs_size_old);
    update := ss.DataString;
    if Copy(update, Length(update), 1) = LF then
      update := Copy(update, 1, Length(update) - 1);
    fs_size_old := fs.Size;
    // Caption := ver + IntToStr(fs_size);
    sl.Text := update;

    i := 0;
    repeat
      if (Copy(sl[i], 1, 2) + Copy(sl[i], 11, 2) = '====') then
      begin
        prov := Trim(Copy(sl[i], 3, 8));

        if (Pos('Request XML-body', sl[i]) > 0) then
        begin
          header := sl[i];
          Inc(i);

          if not GetXML(sl, i, Xml, error) then
          begin
            mmoPML.Lines.Add(error);
            Exit;
          end;

          DefaultDOMVendor := sOmniXmlVendor;
          doc := TXmlDocument.Create(nil);
          try
            try
              doc.LoadFromXML(Xml);

              if IsPMLOrder(doc, refLastOrder, qtyLastOrder) then
              begin
                if ckbOrder.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log(Format('COMMANDE %s %d', [refLastOrder, qtyLastOrder]));
                  Log('');
                end;
              end

              else if IsPMLRequestInfoProduct(doc) then
              begin
                if ckbRequestInfoProduct.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('REQ_INFO_PRODUIT');
                  Log('');
                end;
              end

              else if IsPMLAcknowledgement(doc) then
              begin
                if ckbAcknowledgement.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('ACQUITTEMENT');
                  Log('');
                end;
              end

              else if IsPMLEmptying(doc) then
              begin
                if ckbEmptying.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('VIDAGE');
                  Log('');
                end;
              end

              else
              begin
                mmoPMLClear;
                mmoPML.Lines.Add(header);
              end;

            except
              on E: Exception do
              begin
                mmoPMLClear;
                mmoPML.Lines.Add(E.ClassName + ': ' + E.Message);
                Exit;
              end;
            end;

          finally
            doc := nil;
          end;

        end

        else if (Pos('Response XML-body', sl[i]) > 0) then
        begin
          header := sl[i];
          Inc(i);

          if not GetXML(sl, i, Xml, error) then
          begin
            mmoPML.Lines.Add(error);
            Exit;
          end;

          DefaultDOMVendor := sOmniXmlVendor;
          doc := TXmlDocument.Create(nil);
          try
            try
              doc.LoadFromXML(Xml);

              if IsPMLOrderResponse(doc, refClientOrder, qtyOrderProducts, qtyOrderUnits, refTour, dateTour,
                availabilityMessage,
                prodLabel) then
              begin
                if ckbOrderResponse.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  if (refLastOrder = refClientOrder) then
                  begin
                    Log(Format(
                      'REP_COMMANDE %s %d %d/%d %s %s %s %s',
                      [refClientOrder, qtyOrderProducts, qtyOrderUnits, qtyLastOrder, refTour, dateTour,
                      availabilityMessage, prodLabel]));
                    Log('Notify');

                    mmoNotify.Lines.Add(Format('REP_COMMANDE %s %d %d/%d %s %s %s %s',
                      [prov, qtyOrderProducts, qtyOrderUnits, qtyLastOrder, refTour, dateTour,
                      availabilityMessage, prodLabel]));

                    // OrderNotify
                    motif := availabilityMessage;
                    if (motif <> '') then
                      motif := Format('  │  %s', [motif]);

                    if (qtyOrderUnits = 0) then
                    begin
                      aStatus := osCancelled;
                      aTitle := 'COMMANDE ANNULÉE'; // Format('COMMANDE << %s >> ANNULÉE', [prov]);
                      // aDetail := Format('Cmdé  %d  │  Livré  %d%s', [qtyLastOrder, qtyOrderUnits, motif]);
                    end
                    else if (qtyOrderUnits = qtyLastOrder) then
                    begin
                      aStatus := osConfirmed;
                      aTitle := 'COMMANDE VALIDÉE'; // Format('COMMANDE << %s >> VALIDÉE', [prov]);
                      // aDetail := Format('Cmdé  %d  │  Livré  %d%s', [qtyLastOrder, qtyOrderUnits, motif]);
                    end
                    else
                    begin
                      aStatus := osModified;
                      aTitle := 'COMMANDE MODIFIÉE'; // Format('COMMANDE << %s >> MODIFIÉE', [prov]);
                      // aDetail := Format('Cmdé  %d  │  Livré  %d%s', [qtyLastOrder, qtyOrderUnits, motif]);
                    end;
                    aDetail := Format('Cmdé  %d  │  Livré  %d%s', [qtyLastOrder, qtyOrderUnits, motif]);

                    if not OrderNotify(aStatus, aTitle, aDetail) then
                      mmoNotify.Lines.Add('Fenêtre Notify introuvable !');

                  end
                  else
                  begin
                    Log(Format(
                      'REP_COMMANDE %s %d %d %s %s %s %s',
                      [refClientOrder, qtyOrderProducts, qtyOrderUnits, refTour, dateTour, availabilityMessage,
                      prodLabel]));
                  end;
                  Log('');
                end;
              end

              else if IsPMLDispoInfoResponse(doc) then
              begin
                if ckbDispoInfoResponse.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('REP_INFO_PRODUIT');
                  Log('');
                end;
              end

              else if IsPMLDeliveryResponse(doc) then
              begin
                if ckbDeliveryResponse.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('BON_LIVRAISON');
                  Log('');
                end;
              end

              else if IsPMLResponseError(doc, detailResponse) then
              begin
                if ckbResponseError.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log(Format('ERREUR [%s]', [detailResponse]));
                  Log('');
                end;
              end

              else if IsPMLServiceEnd(doc) then // IsPMLError
              begin
                if ckbServiceEnd.Checked then
                begin
                  mmoPMLClear;
                  mmoPML.Lines.Add(header);
                  Log('FIN_SERVICE');
                  Log('');
                end;
              end

              else
              begin
                mmoPMLClear;
                mmoPML.Lines.Add(header);
              end;

            except
              on E: Exception do
              begin
                mmoPMLClear;
                mmoPML.Lines.Add(E.ClassName + ': ' + E.Message);
                Exit;
              end;
            end;

          finally
            doc := nil;
          end;

        end

        else
        begin
          if (Pos('<?xml', sl[i]) <> 0) or (Pos('<CSRP_ENVELOPPE', sl[i]) <> 0) then
          begin
            mmoPMLClear;
            mmoNotify.Lines.Add(sl[i]);
          end;
        end;

      end;

      Inc(i);
    until (i = sl.Count);

  finally
    SendMessage(mmoPML.Handle, EM_LINESCROLL, 0, mmoPML.Lines.Count);
    SendMessage(mmoNotify.Handle, EM_LINESCROLL, 0, mmoNotify.Lines.Count);

    mmoPML.Lines.EndUpdate;
    mmoNotify.Lines.EndUpdate;

    sl.Free;
    ss.Free;
  end;
end;

end.
