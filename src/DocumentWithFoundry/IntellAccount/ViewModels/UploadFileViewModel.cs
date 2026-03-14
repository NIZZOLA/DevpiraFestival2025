namespace IntellAccount.ViewModels;

public class UploadFileViewModel
{
    public string? Description { get; set; }
    public IFormFile? File { get; set; }
    public string DocumentType { get; set; }
}
