namespace TracePML.Models;

public enum PmlMessageType
{
    // Emission (Request)
    Order,
    RequestInfoProduct,
    Acknowledgement,
    Emptying,

    // Reception (Response)
    OrderResponse,
    DispoInfoResponse,
    DeliveryResponse,
    ServiceEnd,
    ResponseError,

    Unknown
}
