namespace Trek.Net
{
  public sealed class CommandInfo
  {
    public string Prompt { get; private set; }
    public string Title { get; private set; }
    public IList<CommandItemInfo> Commands { get; private set; }

    public CommandInfo(string thisPrompt, string thisTitle, IList<CommandItemInfo> thisCommands)
    {
      Prompt = thisPrompt;
      Title = thisTitle;
      Commands = thisCommands;
    }
  }
}
