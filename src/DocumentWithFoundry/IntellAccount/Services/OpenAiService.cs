using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.Identity;
using IntellAccount.Constants;
using IntellAccount.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace IntellAccount.Services;

public class OpenAiService
{
    private readonly OpenAiConfig _openAiCredentials;
    private readonly SearchApiConfig _searchApiCredentials;
    private bool _useSearch;
    public OpenAiService(OpenAiConfig config, SearchApiConfig searchApiCredentials)
    {
        _openAiCredentials = config;
        _searchApiCredentials = searchApiCredentials;
        _useSearch = true;
    }

    public OpenAiService(OpenAiConfig config)
    {
        _openAiCredentials = config;
        _useSearch = false;
    }

    public OpenAiService(IOptions<OpenAiConfig> config)
    {
        _openAiCredentials = config.Value;
        _useSearch = false;
    }
    private IList<ChatMessage> messages = new List<ChatMessage>();
    public OpenAiService()
    {
        messages.Add(new SystemChatMessage(PromptConstants.DefaultPrompt));
    }

    public async Task<string> GetResponseFromQuestion(string question)
    {
        AzureOpenAIClient azureClient = new(new Uri(_openAiCredentials.Endpoint),
            //new AzureKeyCredential(_openAiCredentials.Key));
            new DefaultAzureCredential());

        ChatClient chatClient = azureClient.GetChatClient(_openAiCredentials.DeploymentName);

        if (question != string.Empty)
        {
            messages.Add(new UserChatMessage(question));

            #pragma warning disable AOAI001

            ChatCompletionOptions options = new();
         
            if (_useSearch)
            {
                options.AddDataSource(new AzureSearchChatDataSource()
                {
                    Endpoint = new Uri(_searchApiCredentials.Endpoint),
                    IndexName = _searchApiCredentials.IndexName,
                    Authentication = DataSourceAuthentication.FromApiKey(_searchApiCredentials.Key),
                });
            }

            ChatCompletion completion = chatClient.CompleteChat(
                [
                    new UserChatMessage(question),
                ],
                options);

            ChatMessageContext onYourDataContext = completion.GetMessageContext();

            foreach (ChatMessageContentPart contentPart in completion.Content)
            {
                return contentPart.Text;
            }
        }
        return string.Empty;
    }

}
