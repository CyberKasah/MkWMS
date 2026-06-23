namespace MkWMS.API.DTOs;

public class SerialNumberDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = "НаСкладе";
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? DataMatrix { get; set; }
    public string? RfidTag { get; set; }

}