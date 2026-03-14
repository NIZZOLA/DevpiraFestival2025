using IntellAccount.Constants;
using IntellAccount.Models;
using IntellAccount.Services;
using IntellAccount.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;

namespace IntellAccount.Controllers;
public class UploadController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly AzureStorageConfig _azureStorageConfig;
    private readonly AzureDocumentConfig _azureDocumentConfig;
    private readonly OpenAiConfig _openAiConfig;
    public UploadController(IConfiguration configuration, IOptions<AzureDocumentConfig> azureDocumentConfig,
               IOptions<OpenAiConfig> openAiConfig)
    {
        _configuration = configuration;
        _azureStorageConfig = new AzureStorageConfig()
        {
            AccountName = _configuration["AzureStorage:accountName"],
            AccountKey = _configuration["AzureStorage:accountKey"],
            ImageContainer = _configuration["AzureStorage:imageContainer"],
            QueueName = _configuration["AzureStorage:queueName"],
            Url = _configuration["AzureStorage:url"],
            DocumentListFileName = _configuration["AzureStorage:documentListFileName"]
        };
        _azureDocumentConfig = azureDocumentConfig.Value;
        _openAiConfig = openAiConfig.Value;
    }
    public async Task<IActionResult> Index()
    {
        var documents = await GetDocumentList();
        return View(documents);
    }

    [HttpGet]
    public async Task<IActionResult> Send()
    {
        ViewBag.Models = DocumentAiModels.Models;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(UploadFileViewModel model)
    {
        ViewBag.Models = DocumentAiModels.Models;
        if (!string.IsNullOrEmpty(model.Description) && (model.File is not null && model.File.Length > 0))
        {
            if (!model.File.IsImage())
            {
                ViewData["Erro"] = "Error: Arquivo enviado não é imagem válida";
                return View(ViewData);
            }

            var Id = Guid.NewGuid().ToString();
            var subTitle = Id.Substring(0, 5);
            string filename = subTitle + Path.GetFileName(model.File.FileName.Replace(" ", ""));
            var result = await AzureStorageService.UploadFileToStorage(_azureStorageConfig, model.File.OpenReadStream(), filename);

            if (result)
            {
                DocumentInfo doc = new DocumentInfo()
                {
                    FileName = filename,
                    Id = Guid.Parse(Id),
                    Description = model.Description,
                    IaProcessed = false,
                    UploadDate = DateTime.Now,
                    DocumentType = model.DocumentType
                };

                await UpdateDocumentList(doc);

                return RedirectToAction(nameof(Index));
            }
        }
        else
        {
            //retorna a viewdata com erro
            ViewData["Erro"] = "Error: Arquivo(s) não selecionado(s) ou descrição vazia";
            return View(ViewData);
        }
        return View();
    }

    public async Task UpdateDocumentList(DocumentInfo doc)
    {
        List<DocumentInfo> list = new List<DocumentInfo>();
        var documentList = await AzureStorageService.ReadJsonFromBlobAsync(_azureStorageConfig, _azureStorageConfig.DocumentListFileName);
        if (!string.IsNullOrEmpty(documentList))
        {
            list = JsonSerializer.Deserialize<List<DocumentInfo>>(documentList);
        }
        list.Add(doc);
        await AzureStorageService.UploadStringAsJsonToStorage(_azureStorageConfig, JsonSerializer.Serialize(list), _azureStorageConfig.DocumentListFileName);
    }

    public async Task<IEnumerable<DocumentInfo>> GetDocumentList()
    {
        List<DocumentInfo> list = new List<DocumentInfo>();
        var documentList = await AzureStorageService.ReadJsonFromBlobAsync(_azureStorageConfig, _azureStorageConfig.DocumentListFileName);
        if (!string.IsNullOrEmpty(documentList))
        {
            list = JsonSerializer.Deserialize<List<DocumentInfo>>(documentList);
        }
        return list;
    }

    [HttpGet]
    public async Task<IActionResult> Analyze(string id)
    {
        var documents = await GetDocumentList();
        if (documents != null)
        {
            var document = documents.Where(b => b.Id == Guid.Parse(id)).FirstOrDefault();
            if (document is not null)
            {
                var documentService = new DocumentIAService(_azureDocumentConfig.Key, _azureDocumentConfig.Endpoint);
                string filenamePath = _azureStorageConfig.Url + "/" + document.FileName;
                var response = await documentService.RecognizeAsJson(filenamePath);

                var azureOpenaiService = new OpenAiService(_openAiConfig);
                
                var iaResponse = await azureOpenaiService.GetResponseFromQuestion(PreparePrompt(response, document.DocumentType));

                await SaveAIResponseToTextStorage(document.FileName, iaResponse);

                var result = new AnalyzeResult()
                {
                    JsonResult = response,
                    Document = document,
                    IaResponse = iaResponse,
                    FileNamePath = filenamePath
                };
                return View(result);
            }
        }
        return View();
    }

    private async Task SaveAIResponseToTextStorage(string fileName, string jsonString)
    {
        var indexConfig = new AzureStorageConfig()
        {
            AccountKey = _azureStorageConfig.AccountKey,
            AccountName = _azureStorageConfig.AccountName,
            Url = _azureStorageConfig.Url,
            ImageContainer = "documentdata"
        };

        var result = await AzureStorageService.UploadStringAsJsonToStorage(indexConfig, jsonString, fileName + ".json");
    }

    private string PreparePrompt(string response, string model)
    {
        switch (model)
        {
            case "prebuilt-layout":
                return PromptConstants.DocumentQuestion + " " + response;
                
            case "prebuilt-receipt":
                return PromptConstants.Receipt + " " + response;
                
            case "prebuilt-businessCard":
                return PromptConstants.BusinessCard + " " + response;
            case "prebuilt-idDocument":
                return PromptConstants.IdDocument + " " + response;
            default:
                return PromptConstants.DocumentQuestion + " " + response;
                
        }
        return string.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> Read(string id)
    {
        ViewBag.Models = DocumentAiModels.Models;
        ViewBag.Id = id;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Read(string id, string selectedModel)
    {
        ViewBag.Models = DocumentAiModels.Models;
        if (string.IsNullOrEmpty(selectedModel))
        {
            ViewData["Erro"] = "Error: Modelo não selecionado";
            return View(ViewData);
        }
        ViewBag.Id = id;
        var documents = await GetDocumentList();
        if (documents != null)
        {
            var document = documents.Where(b => b.Id == Guid.Parse(id)).FirstOrDefault();
            if (document is not null)
            {
                var documentService = new DocumentIAService(_azureDocumentConfig.Key, _azureDocumentConfig.Endpoint);
                string filenamePath = _azureStorageConfig.Url + "/" + document.FileName;
                var response = await documentService.RecognizeAsString(filenamePath);

                var result = new AnalyzeResult()
                {
                    JsonResult = response,
                    Document = document,
                    IaResponse = string.Empty,
                    FileNamePath = filenamePath
                };
                return View(result);
            }
        }
        return View();
    }
}
