namespace HireIQ.Application.DTOs;

public class CreateCustomFieldDTO
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public int Order { get; set; } = 0;
}

public class UpdateCustomFieldDTO
{
    public string FieldValue { get; set; } = string.Empty;
}

public class CustomFieldResponseDTO
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int Order { get; set; }
}