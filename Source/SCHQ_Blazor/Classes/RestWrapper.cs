using SCHQ_Protos;

namespace SCHQ_Blazor.Classes;
public class RestSetRelationsRequest {
  public string? Channel { get; set; }
  public string? Password { get; set; }
  public List<RelationInfo>? Relations { get; set; }
}
