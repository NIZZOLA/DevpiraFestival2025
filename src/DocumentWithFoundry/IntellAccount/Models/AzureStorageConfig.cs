namespace IntellAccount.Models;

public class AzureStorageConfig
{
    public string AccountName { get; set; }
    public string AccountKey { get; set; }
    public string QueueName { get; set; }
    public string ImageContainer { get; set; }
    public string ThumbnailContainer { get; set; }
    public string DocumentListFileName { get; set; }
    public string Url { get; set; }

    // Monta a string de conexão
    public string ConnectionString
    {
        get
        {
            return BuildConnectionString(this.AccountName, this.AccountKey);
        }
    }
    private string BuildConnectionString(string accountName, string accountKey)
    {
        return $"DefaultEndpointsProtocol=https;AccountName={this.AccountName};AccountKey={this.AccountKey}";
    }

}