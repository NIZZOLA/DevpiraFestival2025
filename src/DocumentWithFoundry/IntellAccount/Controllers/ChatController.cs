using Azure;
using IntellAccount.Models;
using IntellAccount.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IntellAccount.Controllers;

public class ChatController : Controller
{
    private readonly OpenAiConfig _openAiConfig;
    private readonly SearchApiConfig _searchApiConfig;

    public ChatController(IOptions<OpenAiConfig> openAiConfig, IOptions<SearchApiConfig> searchConfig)
    {
        _openAiConfig = openAiConfig.Value;
        _searchApiConfig = searchConfig.Value;
    }

    public IActionResult Index()
    {
        return View(new ChatHistory());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string textMessage)
    {
        var azureOpenaiService = new OpenAiService(_openAiConfig, _searchApiConfig);

        var iaResponse = await azureOpenaiService.GetResponseFromQuestion(textMessage);
        var chatHistory = new ChatHistory();
        chatHistory.Interactions.Add(new ChatInteraction
        {
            UserMessage = textMessage,
            BotResponse = false
        });
        chatHistory.Interactions.Add(new ChatInteraction
        {
            UserMessage = iaResponse,
            BotResponse = true
        });

        return View(chatHistory);
    }
}
