using System.Xml.Linq;

namespace TracePML.Services;

public record OrderResponseData(
    string RefClientOrder,
    int QtyProducts,
    int QtyUnits,
    string RefTour,
    string DateTour,
    string AvailabilityMessage,
    string ProdLabel
);

public static class PmlXmlParser
{
    private static XElement? FindByLocalName(XContainer container, string localName)
        => container.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string Attr(XElement element, string name)
        => element.Attribute(name)?.Value ?? "";

    private static int AttrInt(XElement element, string name)
        => int.TryParse(element.Attribute(name)?.Value, out var v) ? v : 0;

    // COMMANDE
    public static bool TryParseOrder(XDocument doc, out string refOrder, out int qtyOrder)
    {
        refOrder = "";
        qtyOrder = 0;

        var node = FindByLocalName(doc, "COMMANDE");
        if (node is null) return false;

        refOrder = Attr(node, "Ref_Cde_Client");

        // NORMALE ou SPECIALE
        var orderNode = node.Elements().FirstOrDefault(e =>
            e.Name.LocalName is "NORMALE" or "SPECIALE");

        if (orderNode is not null)
        {
            foreach (var item in orderNode.Elements())
            {
                if (item.Name.LocalName.StartsWith("LIGNE_"))
                    qtyOrder += AttrInt(item, "Quantite");
            }
        }

        return true;
    }

    // REP_COMMANDE
    public static bool TryParseOrderResponse(XDocument doc, out OrderResponseData data)
    {
        data = default!;

        var node = FindByLocalName(doc, "REP_COMMANDE");
        if (node is null) return false;

        string refOrder = Attr(node, "Ref_Cde_Client");
        string refTour = "";
        string dateTour = "";
        string availabilityMessage = "";
        string prodLabel = "";
        int qtyOrderUnits = 0;

        var tourNode = FindByLocalName(node, "TOURNEE");
        if (tourNode is not null)
        {
            refTour = Attr(tourNode, "Reference");
            dateTour = Attr(tourNode, "Date");
        }

        // NORMALE ou SPECIALE
        var orderResponseNode = FindByLocalName(node, "NORMALE")
            ?? FindByLocalName(node, "SPECIALE");

        if (orderResponseNode is null)
        {
            data = new OrderResponseData(refOrder, 0, 0, refTour, dateTour, "", "");
            return true;
        }

        char orderType = orderResponseNode.Name.LocalName[0];
        var items = orderResponseNode.Elements().ToList();
        int qtyOrderProducts = items.Count;

        foreach (var item in items)
        {
            if (!item.Name.LocalName.StartsWith("LIGNE_"))
                continue;

            int qtyItemDelivered = AttrInt(item, "Quantite_livree");

            // INDISPONIBILITE_N ou INDISPONIBILITE_S
            var unavailNode = FindByLocalName(item, $"INDISPONIBILITE_{orderType}");
            if (unavailNode is not null)
            {
                var dispoInfoNode = FindByLocalName(unavailNode, "INFOS_DISPO");
                if (dispoInfoNode is not null)
                {
                    if (qtyOrderProducts == 1 && dispoInfoNode.Attribute("Date") is not null)
                    {
                        dateTour = Attr(dispoInfoNode, "Date");
                        refTour = "";
                    }

                    // INFOS_DISPO présent → compter sa Quantite AU LIEU de Quantite_livree
                    int qtyAvail = AttrInt(dispoInfoNode, "Quantite");
                    qtyOrderUnits += qtyAvail;

                    if (qtyOrderProducts == 1 && unavailNode.Attribute("Additif") is not null)
                    {
                        availabilityMessage = qtyAvail > 0
                            ? $"{Attr(unavailNode, "Additif")}({qtyAvail})"
                            : Attr(unavailNode, "Additif");
                    }
                }
                else
                {
                    // INDISPONIBILITE sans INFOS_DISPO → compter Quantite_livree
                    qtyOrderUnits += qtyItemDelivered;

                    if (qtyOrderProducts == 1 && unavailNode.Attribute("Additif") is not null)
                        availabilityMessage = Attr(unavailNode, "Additif");
                }
            }
            else
            {
                // Pas d'INDISPONIBILITE → compter Quantite_livree normalement
                qtyOrderUnits += qtyItemDelivered;
            }

            // Si un seul produit, on renvoie le libellé
            if (qtyOrderProducts == 1)
                prodLabel = Attr(item, "Designation");
        }

        data = new OrderResponseData(refOrder, qtyOrderProducts, qtyOrderUnits, refTour, dateTour, availabilityMessage, prodLabel);
        return true;
    }

    // REP_INFO_PRODUIT
    public static bool IsDispoInfoResponse(XDocument doc)
        => FindByLocalName(doc, "REP_INFO_PRODUIT") is not null;

    // BON_LIVRAISON
    public static bool IsDeliveryResponse(XDocument doc)
        => FindByLocalName(doc, "BON_LIVRAISON") is not null;

    // ACTION = ACQUITTEMENT
    public static bool IsAcknowledgement(XDocument doc)
    {
        var node = FindByLocalName(doc, "ACTION");
        return node is not null && node.Value == "ACQUITTEMENT";
    }

    // ACTION = FIN_SERVICE
    public static bool IsServiceEnd(XDocument doc)
    {
        var node = FindByLocalName(doc, "ACTION");
        return node is not null && node.Value == "FIN_SERVICE";
    }

    // ACTION = VIDAGE
    public static bool IsEmptying(XDocument doc)
    {
        var node = FindByLocalName(doc, "ACTION");
        return node is not null && node.Value == "VIDAGE";
    }

    // ERREUR
    public static bool TryParseResponseError(XDocument doc, out string detailMessage)
    {
        detailMessage = "";
        var node = FindByLocalName(doc, "ERREUR");
        if (node is null) return false;
        detailMessage = Attr(node, "Detail");
        return true;
    }

    // REQ_INFO_PRODUIT
    public static bool IsRequestInfoProduct(XDocument doc)
        => FindByLocalName(doc, "REQ_INFO_PRODUIT") is not null;
}
