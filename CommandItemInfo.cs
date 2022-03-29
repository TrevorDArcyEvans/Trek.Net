namespace Trek.Net;

public sealed class CommandItemInfo
{
  /// <summary>
  /// globally unique string to identify this command
  /// </summary>
  public string CommandId { get; private set; }

  public string Description { get; private set; }

  public CommandItemInfo(string thisCommandId, string thisDescription)
  {
    CommandId = thisCommandId;
    Description = thisDescription;
  }
}
