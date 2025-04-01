namespace CP2496H07Group1.Models;

public class Fqa
{
    public required long Id { get; set; }

    public required string Question { get; set; }

    public required string Answer { get; set; }

    public bool IsConfirm { get; set; } = true;
}