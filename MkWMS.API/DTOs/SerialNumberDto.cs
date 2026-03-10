namespace MkWMS.API.DTOs;

public class SerialNumberDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int Status { get; set; }
    public int ProductId { get; set; }
}