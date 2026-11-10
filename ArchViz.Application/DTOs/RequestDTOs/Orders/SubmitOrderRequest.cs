using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Orders;

public record SubmitOrderRequest(
    [Required]
    [StringLength(200)]
    string Title,

    [Required]
    string Description);