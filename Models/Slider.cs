namespace CP2496H07Group1.Models;

public class Slider
{
    public required long Id { get; set; }

    public required string Name { get; set; }

    public required string Detail { get; set; }

    public required string Image { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;
    
    public bool Status { get; set; } = true;
}
