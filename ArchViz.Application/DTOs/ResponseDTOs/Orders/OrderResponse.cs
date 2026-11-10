namespace ArchViz.Application.Orders;

public record OrderResponse(
    int Id,
    string Title,
    string Description,
    string Status);