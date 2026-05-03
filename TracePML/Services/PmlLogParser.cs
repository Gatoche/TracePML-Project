using System.Xml.Linq;
using TracePML.Models;

namespace TracePML.Services;

public class PmlLogParser
{
    private string _refLastOrder = "";
    private int _qtyLastOrder;

    public record ParseResult(
        List<LogEntry> Entries,
        List<OrderNotification> Notifications
    );

    public ParseResult Parse(string newLogContent)
    {
        var entries = new List<LogEntry>();
        var notifications = new List<OrderNotification>();

        // Retirer le LF final s'il existe (comme le Delphi)
        if (newLogContent.EndsWith('\n'))
            newLogContent = newLogContent[..^1];

        var lines = newLogContent.Split('\n');
        // Nettoyage CR
        for (int j = 0; j < lines.Length; j++)
            lines[j] = lines[j].TrimEnd('\r');

        int i = 0;
        while (i < lines.Length)
        {
            // Vérifier le délimiteur : positions 0-1 == "==" et positions 10-11 == "=="
            if (lines[i].Length >= 12 &&
                lines[i][..2] == "==" &&
                lines[i][10..12] == "==")
            {
                string prov = lines[i][2..10].Trim();
                string header = lines[i];

                if (lines[i].Contains("Request XML-body"))
                {
                    i++;
                    if (TryExtractXml(lines, ref i, out string xml))
                    {
                        ProcessRequest(xml, header, prov, entries);
                    }
                }
                else if (lines[i].Contains("Response XML-body"))
                {
                    i++;
                    if (TryExtractXml(lines, ref i, out string xml))
                    {
                        ProcessResponse(xml, header, prov, entries, notifications);
                    }
                }
            }

            i++;
        }

        return new ParseResult(entries, notifications);
    }

    private void ProcessRequest(string xml, string header, string prov, List<LogEntry> entries)
    {
        try
        {
            var doc = XDocument.Parse(xml);

            if (PmlXmlParser.TryParseOrder(doc, out string refOrder, out int qtyOrder))
            {
                _refLastOrder = refOrder;
                _qtyLastOrder = qtyOrder;
                System.Diagnostics.Debug.WriteLine($"[ORDER] refLastOrder={_refLastOrder} qtyLastOrder={_qtyLastOrder}");
                entries.Add(new LogEntry(header, PmlMessageType.Order,
                    $"COMMANDE {refOrder} {qtyOrder} [ref memorisee]", true));
            }
            else if (PmlXmlParser.IsRequestInfoProduct(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.RequestInfoProduct,
                    "REQ_INFO_PRODUIT", true));
            }
            else if (PmlXmlParser.IsAcknowledgement(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.Acknowledgement,
                    "ACQUITTEMENT", true));
            }
            else if (PmlXmlParser.IsEmptying(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.Emptying,
                    "VIDAGE", true));
            }
            else
            {
                entries.Add(new LogEntry(header, PmlMessageType.Unknown, "", true));
            }
        }
        catch (Exception ex)
        {
            entries.Add(new LogEntry(header, PmlMessageType.Unknown,
                $"ERREUR PARSE: {ex.Message}", true));
        }
    }

    private void ProcessResponse(string xml, string header, string prov,
        List<LogEntry> entries, List<OrderNotification> notifications)
    {
        try
        {
            var doc = XDocument.Parse(xml);

            if (PmlXmlParser.TryParseOrderResponse(doc, out var resp))
            {
                // Log visible dans le summary pour debug
                string matchInfo = $"[ref={resp.RefClientOrder} vs last={_refLastOrder}]";
                System.Diagnostics.Debug.WriteLine($"[MATCH] REP refClient={resp.RefClientOrder} vs refLast={_refLastOrder} | qtyUnits={resp.QtyUnits} qtyLast={_qtyLastOrder} | MATCH={_refLastOrder == resp.RefClientOrder}");

                string summary = _refLastOrder == resp.RefClientOrder
                    ? $"REP_COMMANDE {resp.RefClientOrder} {resp.QtyProducts} {resp.QtyUnits}/{_qtyLastOrder} {resp.RefTour} {resp.DateTour} {resp.AvailabilityMessage} {resp.ProdLabel}"
                    : $"REP_COMMANDE {resp.RefClientOrder} {resp.QtyProducts} {resp.QtyUnits} {resp.RefTour} {resp.DateTour} {resp.AvailabilityMessage} {resp.ProdLabel}";

                entries.Add(new LogEntry(header, PmlMessageType.OrderResponse, summary.TrimEnd(), false));

                // Notification seulement si la réponse correspond à la dernière commande
                if (_refLastOrder == resp.RefClientOrder)
                {
                    string motif = resp.AvailabilityMessage;
                    if (motif != "")
                        motif = $"  |  {motif}";

                    OrderStatus status;
                    string title;

                    if (resp.QtyUnits == 0)
                    {
                        status = OrderStatus.Cancelled;
                        title = "COMMANDE ANNULEE";
                    }
                    else if (resp.QtyUnits == _qtyLastOrder)
                    {
                        status = OrderStatus.Confirmed;
                        title = "COMMANDE VALIDEE";
                    }
                    else
                    {
                        status = OrderStatus.Modified;
                        title = "COMMANDE MODIFIEE";
                    }

                    string detail = $"Cmde  {_qtyLastOrder}  |  Livre  {resp.QtyUnits}{motif}";
                    notifications.Add(new OrderNotification(status, title, detail));

                    // Reset pour ne pas re-notifier sur une 2e REP_COMMANDE du même ref
                    _refLastOrder = "";
                }
            }
            else if (PmlXmlParser.IsDispoInfoResponse(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.DispoInfoResponse,
                    "REP_INFO_PRODUIT", false));
            }
            else if (PmlXmlParser.IsDeliveryResponse(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.DeliveryResponse,
                    "BON_LIVRAISON", false));
            }
            else if (PmlXmlParser.TryParseResponseError(doc, out string detailMsg))
            {
                entries.Add(new LogEntry(header, PmlMessageType.ResponseError,
                    $"ERREUR [{detailMsg}]", false));
            }
            else if (PmlXmlParser.IsServiceEnd(doc))
            {
                entries.Add(new LogEntry(header, PmlMessageType.ServiceEnd,
                    "FIN_SERVICE", false));
            }
            else
            {
                entries.Add(new LogEntry(header, PmlMessageType.Unknown, "", false));
            }
        }
        catch (Exception ex)
        {
            entries.Add(new LogEntry(header, PmlMessageType.Unknown,
                $"ERREUR PARSE: {ex.Message}", false));
        }
    }

    /// <summary>
    /// Extrait un bloc XML du tableau de lignes, de &lt;?xml à &lt;/CSRP_ENVELOPPE&gt;.
    /// Port de GetXML dans gTracePML.utils.pas.
    /// </summary>
    private static bool TryExtractXml(string[] lines, ref int i, out string xml)
    {
        xml = "";

        if (i >= lines.Length || !lines[i].StartsWith("<?xml"))
            return false;

        xml = lines[i];
        i++;

        if (i >= lines.Length || !lines[i].StartsWith("<CSRP_ENVELOPPE"))
            return false;

        xml += "\n" + lines[i];
        i++;

        while (i < lines.Length)
        {
            xml += "\n" + lines[i];
            if (lines[i].StartsWith("</CSRP_ENVELOPPE>"))
                break;
            i++;
        }

        return true;
    }
}
