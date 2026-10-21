namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureSearchRequest
{
    public int? Id { get; set; }
    public int? ClosureNumber { get; set; }
    public bool? IsClosed { get; set; }
    public DateTime? OpenDateFrom { get; set; }
    public DateTime? OpenDateTo { get; set; }
}
