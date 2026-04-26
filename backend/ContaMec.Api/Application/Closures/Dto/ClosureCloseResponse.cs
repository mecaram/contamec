namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureCloseResponse
{
    public int ClosedClosureId { get; set; }
    public DateTime CloseDate { get; set; }
    public decimal Result { get; set; }
    public int NewClosureId { get; set; }
    public DateTime NewOpenDate { get; set; }
}
