namespace Client.Models;

public class Token
{
    public string? Value { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }

}
