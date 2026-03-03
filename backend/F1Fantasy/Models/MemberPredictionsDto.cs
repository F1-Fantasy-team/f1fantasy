namespace F1Fantasy.Models;

public class MemberPredictionsDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DriverChampionshipPrediction? DriverChampionship { get; set; }
    public ConstructorChampionshipPrediction? ConstructorChampionship { get; set; }
    public DriverDraftPrediction? DriverDraft { get; set; }
    public DestructorPrediction? Destructor { get; set; }
    public MrSaturdayPrediction? MrSaturday { get; set; }
    public ZeroPointerPrediction? ZeroPointer { get; set; }
    public WildcardPrediction? Wildcard { get; set; }
}
