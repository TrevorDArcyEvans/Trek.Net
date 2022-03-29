namespace Trek.Net.Pages;

using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

public sealed partial class Index : IUserInterface
{
  [CascadingParameter]
  public IModalService Modal { get; set; } = default!;

  private Engine _engine;

  // map of 'command description' --> CommandItemInfo
  private readonly Dictionary<string, CommandItemInfo> _cmds = new Dictionary<string, CommandItemInfo>();

  private string _display { get; set; }

  #region button text
  private string Btn01Title { get; set; }
  private string Btn02Title { get; set; }
  private string Btn03Title { get; set; }
  private string Btn04Title { get; set; }
  private string Btn05Title { get; set; }
  private string Btn06Title { get; set; }
  private string Btn07Title { get; set; }
  private string Btn08Title { get; set; }
  private string Btn09Title { get; set; }
  private string Btn10Title { get; set; }
  private string Btn11Title { get; set; }
  private string Btn12Title { get; set; }
  private string Btn13Title { get; set; }
  private string Btn14Title { get; set; }
  private string Btn15Title { get; set; }
  private string Btn16Title { get; set; }
  private string Btn17Title { get; set; }
  #endregion

  protected override void OnInitialized()
  {
    _engine = new Engine(this);
    _engine.Start();

    base.OnInitialized();
  }

  #region IUserInterface

  public async Task<string> InputString(string prompt)
  {
    return await ShowModal(prompt);
  }

  public void AddCommands(CommandInfo commands)
  {
    if (commands.Commands.Count != 17)
    {
      throw new ArgumentOutOfRangeException("Commands.Commands", "Must be exactly 17 commands to match number of buttons in UI");
    }

    Btn01Title = commands.Commands[00].Description;
    Btn02Title = commands.Commands[01].Description;
    Btn03Title = commands.Commands[02].Description;
    Btn04Title = commands.Commands[03].Description;
    Btn05Title = commands.Commands[04].Description;
    Btn06Title = commands.Commands[05].Description;
    Btn07Title = commands.Commands[06].Description;
    Btn08Title = commands.Commands[07].Description;
    Btn09Title = commands.Commands[08].Description;
    Btn10Title = commands.Commands[09].Description;
    Btn11Title = commands.Commands[10].Description;
    Btn12Title = commands.Commands[11].Description;
    Btn13Title = commands.Commands[12].Description;
    Btn14Title = commands.Commands[13].Description;
    Btn15Title = commands.Commands[14].Description;
    Btn16Title = commands.Commands[15].Description;
    Btn17Title = commands.Commands[16].Description;

    foreach (CommandItemInfo cii in commands.Commands)
    {
      _cmds.Add(cii.Description, cii);
    }
  }

  public void Display(string info)
  {
    _display += info;
  }

  public void DisplayLine(string info)
  {
    Display(info + Environment.NewLine);
  }

  public void Clear()
  {
    _display = string.Empty;
  }

  public void Quit()
  {
    OnInitialized();
  }

  #endregion

  private async Task OnButtonClick(string cmdDescription)
  {
    await _engine.OnCommandSelected(_cmds[cmdDescription]);
  }

  private async Task<string> ShowModal(string title)
  {
    var msgFrm = Modal.Show<MessageForm>(title);
    var result = await msgFrm.Result;

    return !result.Cancelled ? result.Data?.ToString() ?? string.Empty : string.Empty;
  }
}
