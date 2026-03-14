using IntellAccount.Models;

namespace IntellAccount.ViewModels;

public class AnalyzeResult
{
    public string JsonResult { get; set; }
    public DocumentInfo Document { get; set; }
    public string IaResponse { get; set; }
    public string FileNamePath { get; set; }
}
