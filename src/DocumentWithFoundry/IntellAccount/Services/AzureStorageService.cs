using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using IntellAccount.Models;

namespace IntellAccount.Services;

public static class AzureStorageService
{
    public static async Task<bool> UploadFileToStorage(AzureStorageConfig _storageConfig, Stream fileStream, string fileName)
    {
        try
        {
            // Create a BlobServiceClient using the connection string
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

            // Get a reference to the blob container
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ImageContainer);

            // Create the container if it does not exist
            await containerClient.CreateIfNotExistsAsync();

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // Upload the file to the blob
            await blobClient.UploadAsync(fileStream, overwrite: true);

            return true;
        }
        catch (Exception error)
        {
            throw new Exception(error.Message);
        }
    }

    public static async Task<bool> UploadStringAsJsonToStorage(AzureStorageConfig _storageConfig, 
                                                      string jsonString, string fileName)
    {
        try
        {
            // Cria um BlobServiceClient usando a string de conexão
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

            // Obtém uma referência para o container de blobs
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ImageContainer);

            // Cria o container se ele não existir
            await containerClient.CreateIfNotExistsAsync();

            // Obtém uma referência para o blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // Converte a string JSON para um Stream
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonString);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                // Faz o upload do conteúdo do stream para o blob
                await blobClient.UploadAsync(stream, overwrite: true);

                // Definir o tipo de conteúdo como JSON
                var blobHttpHeaders = new BlobHttpHeaders { ContentType = "application/json" };
                await blobClient.SetHttpHeadersAsync(blobHttpHeaders);
            }

            return true;
        }
        catch (Exception error)
        {
            throw new Exception($"Erro ao fazer upload do JSON para o Blob Storage: {error.Message}");
        }
    }

    public static async Task<string> ReadJsonFromBlobAsync(AzureStorageConfig _storageConfig, 
                                                        string fileName)
    {
        try
        {
            // Cria um BlobServiceClient usando a string de conexão
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

            // Obtém uma referência para o container de blobs
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ImageContainer);

            // Obtém uma referência para o blob (arquivo JSON)
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // Verifica se o blob existe antes de tentar ler
            if (await blobClient.ExistsAsync())
            {
                // Faz o download do conteúdo do blob
                BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();

                // Obtém o conteúdo do blob como uma string
                string jsonContent = downloadResult.Content.ToString();

                return jsonContent;
            }
            else
            {
                //throw new Exception("O blob especificado não existe.");
            }
        }
        catch (Exception error)
        {
            throw new Exception($"Erro ao ler o JSON do Blob Storage: {error.Message}");
        }
        return string.Empty;
    }

    public static string BlobUrl(AzureStorageConfig _storageConfig, string imageFileName)
    {
        // Create a BlobServiceClient using the connection string
        BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

        // Get a reference to the blob container
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ImageContainer);

        // Get a reference to the blob
        BlobClient blobClient = containerClient.GetBlobClient(imageFileName);

        // Return the URI of the blob
        return blobClient.Uri.AbsoluteUri;
    }

    public static async Task<bool> DeleteSpecificBlob(AzureStorageConfig _storageConfig, string blobName)
    {
        try
        {
            // Create a BlobServiceClient using the connection string
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

            // Get a reference to the blob container
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ImageContainer);

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Delete the blob if it exists
            return await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception error)
        {
            throw new Exception(error.Message);
        }
    }

    public static async Task<List<string>> GetThumbNailUrls(AzureStorageConfig _storageConfig)
    {
        List<string> thumbnailUrls = new List<string>();

        // Create a BlobServiceClient using the connection string
        BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);

        // Get a reference to the blob container
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_storageConfig.ThumbnailContainer);

        // List all blobs in the container
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
            thumbnailUrls.Add(blobClient.Uri.AbsoluteUri);
        }

        return thumbnailUrls;
    }
}