namespace TracePML.Models;

public record LogEntry(
    string RawHeader,
    PmlMessageType MessageType,
    string Summary,
    bool IsRequest
);
