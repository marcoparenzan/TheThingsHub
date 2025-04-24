namespace IoTMgmtApp.Models;

public class AIOMessagePayload
{
    public DateTimeOffset Timestamp { get; set; }
    public string MessageType { get; set; }
    public Dictionary<string, AIOMessagePayloadItem> Payload { get; set; }
    public string DataSetWriterName { get; set; }
    public int SequenceNumber { get; set; }
}