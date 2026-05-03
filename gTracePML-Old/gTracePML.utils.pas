unit gTracePML.utils;

interface

uses
  Winapi.Windows,
  Winapi.Messages,
  System.SysUtils,
  System.Variants,
  System.Classes,
  System.JSON,
  System.JSON.Serializers,
  System.StrUtils,
  System.TypInfo,
  Vcl.Graphics,
  Vcl.Controls,
  Vcl.Forms,
  Vcl.Dialogs,
  Vcl.StdCtrls,
  Vcl.ExtCtrls,
  Xml.XmlIntf,
  Xml.XMLDoc,
  Xml.XMLDom,
  Xml.omnixmldom,

  muConst;

type
  TEnumConverter = class
  public
    class function EnumToInt<T>(const EnumValue: T): Integer;
    class function EnumToString<T>(EnumValue: T): string;
  end;

  TOrderStatus = (osConfirmed, osModified, osCancelled);

  TOrder = record
    Status: TOrderStatus;
    Title: string;
    Detail: string;
  end;

  // function GetProperty(line: string; prop: string; out value: string): Boolean; overload;
  // function GetProperty(line: string; prop: string; out value: Integer): Boolean; overload;

function StreamToString(aStream: TStream): string;
procedure SendCopyDataMessage(HWNDSender, HWNDReceiver: HWND; JsonString: UTF8String);

function GetNodeFromPath(xnRoot: IXmlNode; const nodePath: WideString): IXmlNode;
// function XMLQuery(doc: IXMLDocument; prov: String; var res: String): Boolean;
function GetXML(sl: TStrings; out i: Integer; out Xml: string; out error: string): Boolean;

function IsPMLOrder(doc: IXMLDocument; out refOrder: string; out qtyOrder: Integer): Boolean;
function IsPMLOrderResponse(doc: IXMLDocument; out refOrder: string; out qtyOrderProducts: Integer;
  out qtyOrderUnits: Integer; out refTour: string; out dateTour: string; out availabilityMessage: string;
  out prodLabel: string): Boolean;
function IsPMLDispoInfoResponse(doc: IXMLDocument): Boolean;
function IsPMLDeliveryResponse(doc: IXMLDocument): Boolean;
function IsPMLAcknowledgement(doc: IXMLDocument): Boolean;
function IsPMLServiceEnd(doc: IXMLDocument): Boolean;
function IsPMLEmptying(doc: IXMLDocument): Boolean;
function IsPMLResponseError(doc: IXMLDocument; out detailMessage: string): Boolean;
function IsPMLRequestInfoProduct(doc: IXMLDocument): Boolean;

implementation

// -----------------------------------------------------------------------------
// function GetProperty(line: string; prop: string; out value: string): Boolean; overload;
// var
// p, l: Integer;
// begin
// Result := False;
// prop := prop + '="';
// p := Pos(prop, line);
// if (p = -1) then
// Exit;
// line := Copy(line, p + Length(prop));
// l := Pos('"', line);
// if (l = -1) then
// Exit;
// value := Copy(line, 1, l - 1);
// Result := true;
// end;

// -----------------------------------------------------------------------------
// function GetProperty(line: string; prop: string; out value: Integer): Boolean; overload;
// var
// p, l: Integer;
// begin
// Result := False;
// try
// prop := prop + '="';
// p := Pos(prop, line);
// if (p = -1) then
// Exit;
// line := Copy(line, p + Length(prop));
// l := Pos('"', line);
// if (l = -1) then
// Exit;
// value := StrToInt(Copy(line, 1, l - 1));
// Result := true;
// except
// end;
// end;

// -----------------------------------------------------------------------------
function StreamToString(aStream: TStream): string;
var
  ss: TStringStream;
begin
  if aStream <> nil then
  begin
    ss := TStringStream.Create('');
    try
      ss.CopyFrom(aStream, 0); // No need to position at 0 nor provide size
      Result := ss.DataString;
    finally
      ss.Free;
    end;
  end
  else
  begin
    Result := '';
  end;
end;

// -----------------------------------------------------------------------------
procedure SendCopyDataMessage(HWNDSender, HWNDReceiver: HWND; JsonString: UTF8String);
var
  Data: TJSONObject;
  Message: COPYDATASTRUCT;
begin
  Message.dwData := 1; // 1: Delphi UTF8 / 0: CSharp UNICode
  Message.cbData := Length(JsonString) * SizeOf(WideChar);
  Message.lpData := PWideChar(JsonString);

  SendMessage(HWNDReceiver, WM_COPYDATA, WPARAM(HWNDSender), LPARAM(@Message));
end;

// -----------------------------------------------------------------------------
function GetNodeFromPath(xnRoot: IXmlNode; const nodePath: WideString): IXmlNode;
var
  intfSelect: IDomNodeSelect;
  dnResult: IDomNode;
  intfDocAccess: IXmlDocumentAccess;
  doc: TXmlDocument;
begin
  Result := nil;
  if not Assigned(xnRoot) or not Supports(xnRoot.DOMNode, IDomNodeSelect, intfSelect) then
    Exit;

  dnResult := intfSelect.SelectNode(nodePath);
  if Assigned(dnResult) then
  begin
    if Supports(xnRoot.OwnerDocument, IXmlDocumentAccess, intfDocAccess) then
      doc := intfDocAccess.DocumentObject
    else
      doc := nil;
    Result := TXmlNode.Create(dnResult, nil, doc);
  end;
end;

// -----------------------------------------------------------------------------
// function XMLQuery(doc: IXMLDocument; prov: String; var res: String): Boolean;
// var
// Node: IXmlNode;
// childID: Integer;
// disp, dispadd: String;
// prix: string;
// begin
// Result := False;
// Node := GetNodeFromPath(doc.DocumentElement, '//REP_INFO_PRODUIT');
// if Assigned(Node) then
// if Node.ChildNodes.Count = 1 then
// if (Node.ChildNodes[0].NodeName = 'LIGNE')
// and Node.ChildNodes[0].HasAttribute('Code_Produit') then
// begin
// disp := '';
// prix := ';';
// for childID := 0 to Node.ChildNodes[0].ChildNodes.Count - 1 do
// begin
// if Node.ChildNodes[0].ChildNodes[childID].NodeName = 'PRIX' then
// begin
// if Node.ChildNodes[0].ChildNodes[childID].HasAttribute('Nature')
// and Node.ChildNodes[0].ChildNodes[childID].HasAttribute('Valeur') then
// prix := prix + Node.ChildNodes[0].ChildNodes[childID].Attributes['Nature'] + ':' +
// Node.ChildNodes[0].ChildNodes[childID].Attributes['Valeur'] + ';'
// end
// else
// if Node.ChildNodes[0].ChildNodes[childID].NodeName = 'DISPO' then
// disp := 'DISPO'
// else
// if Node.ChildNodes[0].ChildNodes[childID].NodeName = 'NON_DISPO' then
// disp := 'NON_DISPO';
// end;
// if disp <> 'DISPO' then
// prix := ';';
// res := prov + ';REP_INFO_PRODUIT;' + Node.ChildNodes[0].Attributes['Code_Produit'] + ';' + disp + prix;
// Result := disp <> '';
// end;
// end;

// -----------------------------------------------------------------------------
function GetXML(sl: TStrings; out i: Integer; out Xml: string; out error: string): Boolean;
begin
  Result := False;

  if (Pos('<?xml', sl[i]) <> 1) then
  begin
    error := Format('Arrêt de l''analyse: erreur de formattage en ligne %d', [i + 1]);
    Exit;
  end;

  Xml := sl[i];
  // mmoDispo.Lines.Add(sl[i]);
  Inc(i);
  if (Pos('<CSRP_ENVELOPPE', sl[i]) <> 1) then
  begin
    error := Format('Arrêt de l''analyse: erreur de formattage en ligne %d', [i + 1]);
    Exit;
  end;

  Xml := Xml + CR + sl[i];
  // mmoDispo.Lines.Add(sl[i]);
  Inc(i);
  repeat
    Xml := Xml + CR + sl[i];
    // mmoDispo.Lines.Add(sl[i]);
    if (Pos('</CSRP_ENVELOPPE>', sl[i]) = 1) then
      Break;
    Inc(i);
  until (i = sl.Count);

  Result := True;
end;

// -----------------------------------------------------------------------------
function IsPMLOrder(doc: IXMLDocument; out refOrder: string; out qtyOrder: Integer): Boolean;
var
  Node: IXmlNode;
  orderNode: IXmlNode;
  itemNode: IXmlNode;
  i: Integer;
begin
  refOrder := '';
  qtyOrder := 0;

  Node := GetNodeFromPath(doc.DocumentElement, '//COMMANDE');
  Result := Assigned(Node);
  if Result then
  begin
    if Node.HasAttribute('Ref_Cde_Client') then
      refOrder := VarToStr(Node.Attributes['Ref_Cde_Client']);

    if (Node.ChildNodes[0].NodeName = 'NORMALE') or (Node.ChildNodes[0].NodeName = 'SPECIALE') then
    begin
      orderNode := Node.ChildNodes[0];
      for i := 0 to orderNode.ChildNodes.Count - 1 do
      begin
        itemNode := orderNode.ChildNodes[i];
        if Pos('LIGNE_', itemNode.NodeName) = 1 then
          if itemNode.HasAttribute('Quantite') then
            Inc(qtyOrder, StrToIntDef(VarToStr(itemNode.Attributes['Quantite']), 0));
      end;
    end;
  end;
end;

// -----------------------------------------------------------------------------
function IsPMLOrderResponse(doc: IXMLDocument; out refOrder: string; out qtyOrderProducts: Integer;
  out qtyOrderUnits: Integer; out refTour: string; out dateTour: string; out availabilityMessage: string;
  out prodLabel: string): Boolean;
var
  Node: IXmlNode;
  tourNode: IXmlNode;
  orderResponseNode: IXmlNode;
  itemNode: IXmlNode;
  i: Integer;
  qtyItemOrderResponse: Integer;
  orderType: char;
  unavailabilityNode: IXmlNode;
  availabilityInfoNode: IXmlNode;
  qtyAvailabilityResponse: Integer;
begin
  refOrder := '';
  qtyOrderProducts := 0;
  qtyOrderUnits := 0;
  refTour := '';
  dateTour := '';
  availabilityMessage := '';
  prodLabel := '';
  qtyAvailabilityResponse := 0;

  Node := GetNodeFromPath(doc.DocumentElement, '//REP_COMMANDE');
  Result := Assigned(Node);
  if Result then
  begin
    if Node.HasAttribute('Ref_Cde_Client') then
      refOrder := VarToStr(Node.Attributes['Ref_Cde_Client']);

    tourNode := GetNodeFromPath(Node, '//TOURNEE');
    if Assigned(tourNode) then
    begin
      if tourNode.HasAttribute('Reference') then
        refTour := VarToStr(tourNode.Attributes['Reference']);
      if tourNode.HasAttribute('Date') then
        dateTour := VarToStr(tourNode.Attributes['Date']);
    end;

    orderResponseNode := GetNodeFromPath(Node, '//NORMALE');
    if not Assigned(orderResponseNode) then
    begin
      orderResponseNode := GetNodeFromPath(Node, '//SPECIALE');
      if not Assigned(orderResponseNode) then
        Exit;
    end;
    orderType := orderResponseNode.NodeName[1];

    qtyOrderProducts := orderResponseNode.ChildNodes.Count;
    for i := 0 to qtyOrderProducts - 1 do
    begin
      itemNode := orderResponseNode.ChildNodes[i];
      if Pos('LIGNE_', itemNode.NodeName) = 1 then
      begin

        if itemNode.HasAttribute('Quantite_livree') then
        begin
          qtyItemOrderResponse := StrToIntDef(VarToStr(itemNode.Attributes['Quantite_livree']), 0);
          Inc(qtyOrderUnits, qtyItemOrderResponse);

          unavailabilityNode := GetNodeFromPath(itemNode, '//INDISPONIBILITE_' + orderType);
          if Assigned(unavailabilityNode) then
          begin
            availabilityInfoNode := GetNodeFromPath(unavailabilityNode, '//INFOS_DISPO');
            if Assigned(availabilityInfoNode) then
            begin
              if (qtyOrderProducts = 1) then
                if availabilityInfoNode.HasAttribute('Date') then
                begin
                  dateTour := VarToStr(availabilityInfoNode.Attributes['Date']);
                  refTour := '';
                end;
              if availabilityInfoNode.HasAttribute('Quantite') then
              begin
                qtyAvailabilityResponse := StrToIntDef(VarToStr(availabilityInfoNode.Attributes['Quantite']), 0);
                if (refOrder= '168493') then
                 ShowMessage(IntToStr(qtyAvailabilityResponse));
                Inc(qtyOrderUnits, qtyAvailabilityResponse);
              end;
            end;

            if (qtyOrderProducts = 1) then
              if unavailabilityNode.HasAttribute('Additif') then
              begin
                if (qtyAvailabilityResponse > 0) then
                  availabilityMessage :=
                    Format('%s(%d)', [VarToStr(unavailabilityNode.Attributes['Additif']), qtyAvailabilityResponse])
                else
                  availabilityMessage := VarToStr(unavailabilityNode.Attributes['Additif']);
              end;
          end;

        end;

        // Si un seul produit, on renvoi le libellé
        if (qtyOrderProducts = 1) then
          if itemNode.HasAttribute('Designation') then
            prodLabel := VarToStr(itemNode.Attributes['Designation']);
      end;
    end;

  end;
end;

// -----------------------------------------------------------------------------
function IsPMLDispoInfoResponse(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Node := GetNodeFromPath(doc.DocumentElement, '//REP_INFO_PRODUIT');
  Result := Assigned(Node);
end;

// -----------------------------------------------------------------------------
function IsPMLDeliveryResponse(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Node := GetNodeFromPath(doc.DocumentElement, '//BON_LIVRAISON');
  Result := Assigned(Node);
end;

// -----------------------------------------------------------------------------
function IsPMLAcknowledgement(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Result := False;
  Node := GetNodeFromPath(doc.DocumentElement, '//ACTION');
  if not Assigned(Node) then
    Exit;
  Result := (VarToStr(Node.NodeValue) = 'ACQUITTEMENT');
end;

// -----------------------------------------------------------------------------
function IsPMLServiceEnd(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Result := False;
  Node := GetNodeFromPath(doc.DocumentElement, '//ACTION');
  if not Assigned(Node) then
    Exit;
  Result := (VarToStr(Node.NodeValue) = 'FIN_SERVICE');
end;

// -----------------------------------------------------------------------------
function IsPMLEmptying(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Result := False;
  Node := GetNodeFromPath(doc.DocumentElement, '//ACTION');
  if not Assigned(Node) then
    Exit;
  Result := (VarToStr(Node.NodeValue) = 'VIDAGE');
end;

// -----------------------------------------------------------------------------
function IsPMLResponseError(doc: IXMLDocument; out detailMessage: string): Boolean;
var
  Node: IXmlNode;
begin
  Result := False;
  Node := GetNodeFromPath(doc.DocumentElement, '//ERREUR');
  Result := Assigned(Node);
  if Result then
  begin
    if Node.HasAttribute('Detail') then
      detailMessage := VarToStr(Node.Attributes['Detail']);
  end;
end;

// -----------------------------------------------------------------------------
function IsPMLRequestInfoProduct(doc: IXMLDocument): Boolean;
var
  Node: IXmlNode;
begin
  Node := GetNodeFromPath(doc.DocumentElement, '//REQ_INFO_PRODUIT');
  Result := Assigned(Node);
end;

// -----------------------------------------------------------------------------
class function TEnumConverter.EnumToInt<T>(const EnumValue: T): Integer;
begin
  Result := 0;
  Move(EnumValue, Result, sizeOf(EnumValue));
end;

// -----------------------------------------------------------------------------
class function TEnumConverter.EnumToString<T>(EnumValue: T): string;
begin
  Result := GetEnumName(TypeInfo(T), EnumToInt(EnumValue));
end;

end.
