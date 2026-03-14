namespace IntellAccount.Models;

public class DocumentInfo
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public string FileName { get; set; }
    public bool IaProcessed { get; set; }
    public DateTime UploadDate { get; set; }
    public string DocumentType { get; set; }
}
