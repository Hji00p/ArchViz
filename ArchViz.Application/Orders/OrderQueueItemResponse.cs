namespace ArchViz.Application.Orders;

public record OrderQueueItemResponse(
    int Id,
    string Title,
    string DescriptionPreview);