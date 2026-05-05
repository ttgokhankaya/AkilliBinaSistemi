namespace WebApi.Data;

public class Area
{
    public int ID { get; set; }
    public string? Name { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int? AreaID { get; set; }
    public int? AreaTypeID { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
}
