namespace GreaseMate.Models;

public class ReminderSettings
{
    public int Id { get; set; } = 1;
    public int LeadDays { get; set; } = 30;
    public int LeadMileage { get; set; } = 1000;
}