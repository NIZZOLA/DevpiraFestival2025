namespace IntellAccount.Models;

public class ChatHistory
{
    public IList<ChatInteraction> Interactions { get; set; } = new List<ChatInteraction>();
}

public class ChatInteraction
{
    public string UserMessage { get; set; }
    public bool BotResponse { get; set; }
}
