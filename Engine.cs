namespace Trek.Net;

using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;

public sealed class Engine
{
  private const string DataFileRootName = "StarTrekData";
  private const string DataFileExtension = ".xml";

  private StarTrekData _data;
  private readonly IUserInterface _uiMain;

  public Engine(IUserInterface thisUiMain)
  {
    _uiMain = thisUiMain;

    _uiMain.AddCommands(MainCommands);

    InitializeGame();
  }

  public async Task OnCommandSelected(CommandItemInfo cmd)
  {
    PrintGameStatus();
    if (IsGameFinished())
    {
      if (await NewGame())
      {
        InitializeGame();
        Start();
      }
      else
      {
        _uiMain.Quit();
      }

      return;
    }

    // filter out computer commands first
    if (cmd.CommandId == "rec" ||
        cmd.CommandId == "sta" ||
        cmd.CommandId == "toc" ||
        cmd.CommandId == "bas" ||
        cmd.CommandId == "nvc")
    {
      if (_data.ComputerDamage > 0)
      {
        _uiMain.DisplayLine("The main computer is damaged. Repairs are underway.");
        _uiMain.DisplayLine("");
        return;
      }
    }


    switch (cmd.CommandId)
    {
      #region Main commands

      case "nav":
        await Navigation();
        break;

      case "srs":
        ShortRangeScan();
        break;

      case "lrs":
        LongRangeScan();
        break;

      case "pha":
        await PhaserControls();
        break;

      case "tor":
        await TorpedoControl();
        break;

      case "dam":
        await DamageControl();
        break;

      case "sav":
        await SaveUserGame();
        break;

      case "ldg":
        await LoadUserGame();
        break;

      case "hlp":
        ShowHelp();
        break;

      case "xxx":
        ResignCommission();
        if (await NewGame())
        {
          InitializeGame();
          Start();
        }
        else
        {
          _uiMain.Quit();
        }

        break;

      #endregion

      #region Shield commands

      case "add":
        await ShieldControls(true, _data.Energy);
        break;

      case "sub":
        await ShieldControls(false, _data.ShieldLevel);
        break;

      #endregion

      #region Computer commands

      case "rec":
        DisplayGalacticRecord();
        break;

      case "sta":
        DisplayStatus();
        break;

      case "toc":
        PhotonTorpedoCalculator();
        break;

      case "bas":
        StarBaseCalculator();
        break;

      case "nvc":
        await NavigationCalculator();
        break;

      #endregion

      default:
        Debug.Fail("Failed to handle: " + cmd.CommandId);
        break;
    }
  }

  #region String Constants

  #region Title

  private static readonly string[] TitleStrings =
  {
    @"                         ______ __   __ ______ ______ ______ ",
    @"                        / __  // /  / // __  // ____// __  /",
    @"                       / / /_// /  / // /_/ // /__  / /_/ /",
    @"                       _\ \  / /  / // ___ // __ / /   __/",
    @"                     / /_/ // /__/ // /    / /___ / /\ \",
    @"                    /_____/ \____ //_/    /_____//_/  \_\",
    @"",
    @"                        _______ ______  ______ __  __ ",
    @"                       /__  __// __  / / ____// / / /",
    @"                         / /  / /_/ / / /__  / // /",
    @"                        / /  /   __/ / __ / /   / ",
    @"                       / /  / /\ \  / /___ / /\ \",
    @"                      /_/  /_/  \_\/_____//_/  \_\",
    @"",
    @"                     ________________        _",
    @"                     \__(=======/_=_/____.--'-`--.___",
    @"                                \ \   `,--,-.___.----'",
    @"                              .--`\\--'../",
    @"                             '---._____.|]",
  };

  #endregion

  #region QuadrantNames

  private static readonly string[] QuadrantNames =
  {
    "Aaamazzara",
    "Altair IV",
    "Aurelia",
    "Bajor",
    "Benthos",
    "Borg Prime",
    "Cait",
    "Cardassia Prime",
    "Cygnia Minor",
    "Daran V",
    "Duronom",
    "Dytallix B",
    "Efros",
    "El-Adrel IV",
    "Epsilon Caneris III",
    "Ferenginar",
    "Finnea Prime",
    "Freehaven",
    "Gagarin IV",
    "Gamma Trianguli VI",
    "Genesis",
    "H'atoria",
    "Holberg 917-G",
    "Hurkos III",
    "Iconia",
    "Ivor Prime",
    "Iyaar",
    "Janus VI",
    "Jouret IV",
    "Juhraya",
    "Kabrel I",
    "Kelva",
    "Ktaris",
    "Ligillium",
    "Loval",
    "Lyshan",
    "Magus III",
    "Matalas",
    "Mudd",
    "Nausicaa",
    "New Bajor",
    "Nova Kron",
    "Ogat",
    "Orion III",
    "Oshionion Prime",
    "Pegos Minor",
    "P'Jem",
    "Praxillus",
    "Qo'noS",
    "Quadra Sigma III",
    "Quazulu VIII",
    "Rakosa V",
    "Rigel VII",
    "Risa",
    "Romulus",
    "Rura Penthe",
    "Sauria",
    "Sigma Draconis",
    "Spica",
    "Talos IV",
    "Tau Alpha C",
    "Ti'Acor",
    "Udala Prime",
    "Ultima Thule",
    "Uxal",
    "Vacca VI",
    "Volan II",
    "Vulcan",
    "Wadi",
    "Wolf 359",
    "Wysanti",
    "Xanthras III",
    "Xendi Sabu",
    "Xindus",
    "Yadalla Prime",
    "Yadera II",
    "Yridian",
    "Zalkon",
    "Zeta Alpha II",
    "Zytchin III",
  };

  #endregion

  #region Commands

  private const string MainCommandPrompt = "Enter command: ";
  private const string MainCommandTitle = "--- Commands -----------------";

  private static readonly List<CommandItemInfo> MainCommandItems = new()
  {
    new CommandItemInfo("nav", "Warp Engine Control"),
    new CommandItemInfo("srs", "Short Range Scan"),
    new CommandItemInfo("lrs", "Long Range Scan"),

    new CommandItemInfo("pha", "Phaser Control"),
    new CommandItemInfo("tor", "Photon Torpedo Control"),
    new CommandItemInfo("add", "Add Energy To Shields"),
    new CommandItemInfo("sub", "Subtract Energy From Shields"),
    new CommandItemInfo("dam", "Damage Control"),

    new CommandItemInfo("rec", "Cumulative Galatic Record"),
    new CommandItemInfo("sta", "Status Report"),

    new CommandItemInfo("toc", "Photon Torpedo Calculator"),
    new CommandItemInfo("bas", "Starbase Calculator"),
    new CommandItemInfo("nvc", "Navigation Calculator"),

    new CommandItemInfo("sav", "Save Game"),
    new CommandItemInfo("ldg", "Load Saved Game"),
    new CommandItemInfo("hlp", "Help"),
    new CommandItemInfo("xxx", "Resign Commission"),
  };

  private static readonly CommandInfo MainCommands = new(MainCommandPrompt, MainCommandTitle, MainCommandItems);

  #endregion

  #region Help

  private const string HelpText =
    @"
The galaxy is divided into an 8 X 8 quadrant grid, and each quadrant
is further divided into an 8 x 8 sector grid.

You will be assigned a starting point somewhere in the galaxy to begin
a tour of duty as commander of the starship _Enterprise_
Your mission:
  to seek out and destroy the fleet of Klingon warships which are
  menacing the United Federation of Planets.

You have the following commands available to you as Captain of the Starship
Enterprise:

Warp Engine Control
  Course is in a circular numerical vector            4  3  2
  arrangement as shown. Integer and real               . . .
  values may be used. (Thus course 1.5 is               ...
  half-way between 1 and 2.                         5 ---*--- 1
                                                        ...
  Values may approach 9.0, which itself is             . . .
  equivalent to 1.0.                                  6  7  8

  One warp factor is the size of one quadrant.        COURSE
  Therefore, to get from quadrant 6,5 to 5,5
  you would use course 3, warp factor 1.

Short Range Sensor Scan
  Shows you a scan of your present quadrant.

  Symbology on your sensor screen is as follows:
    <*> = Your starship's position
    +K+ = Klingon battlecruiser
    >!< = Federation starbase (Refuel/Repair/Re-Arm here)
     *  = Star

  A condensed 'Status Report' will also be presented.

Long Range Sensor Scan
  Shows conditions in space for one quadrant on each side of the Enterprise
  (which is in the middle of the scan). The scan is coded in the form \###\
  where the units digit is the number of stars, the tens digit is the number
  of starbases, and the hundreds digit is the number of Klingons.

  Example - 207 = 2 Klingons, No Starbases, & 7 stars.

Phaser Control
  Allows you to destroy the Klingon Battle Cruisers by zapping them with
  suitably large units of energy to deplete their shield power. (Remember,
  Klingons have phasers, too!)

Photon Torpedo Control
  Torpedo course is the same  as used in warp engine control. If you hit
  the Klingon vessel, he is destroyed and cannot fire back at you. If you
  miss, you are subject to the phaser fire of all other Klingons in the
  quadrant.

Shield Control
  Defines the number of energy units to be assigned to the shields. Energy
  is taken from total ship's energy. Note that the status display total
  energy includes shield energy.

Damage Control
  Gives the state of repair of all devices. Where a negative
  'State of Repair' shows that the device is temporarily damaged.

Cumulative Galactic Record
    This option shows computer memory of the results of all previous
    short and long range sensor scans.

Status Report
    This option shows the number of Klingons, stardates, and starbases
    remaining in the game.

Photon Torpedo Calculator
    Which gives directions and distance from Enterprise to all Klingons
    in your quadrant.

Starbase Calculator
    This option gives direction and distance to any starbase in your
    quadrant.

Navigation Calculator
    This option allows you to enter coordinates for direction/distance
    calculations.

Save Game
    Save current game to a slot [0-9]

Load Saved Game
    Load a previously saved game from a slot [0-9]

Help
    This screen of instructions and explanations.

Resign Commission
    Quit current game and possibly start a new one.
";

  #endregion

  #endregion

  public void Start()
  {
    _uiMain.Clear();
    PrintStrings(TitleStrings);
    PrintMission();
    GenerateSector();
    PrintGameStatus();
  }

  private async Task<bool> NewGame()
  {
    _uiMain.DisplayLine("The Federation is in need of a new starship commander");
    _uiMain.DisplayLine(" for a similar mission.");
    _uiMain.DisplayLine("");

    var command = await _uiMain.InputString("If there is a volunteer, let him step forward and enter 'aye': ");
    _uiMain.DisplayLine("");

    return command == "aye";
  }

  private bool IsGameFinished()
  {
    return _data.Destroyed || (_data.Energy == 0) || (_data.Klingons == 0) || (_data.TimeRemaining == 0) || _data.Resigned;
  }

  private void PrintGameStatus()
  {
    if (_data.Destroyed)
    {
      _uiMain.DisplayLine("MISSION FAILED: ENTERPRISE DESTROYED!!!");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
    }
    else if (_data.Energy == 0)
    {
      _uiMain.DisplayLine("MISSION FAILED: ENTERPRISE RAN OUT OF ENERGY.");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
    }
    else if (_data.Klingons == 0)
    {
      _uiMain.DisplayLine("MISSION ACCOMPLISHED: ALL KLINGON SHIPS DESTROYED. WELL DONE!!!");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
    }
    else if (_data.TimeRemaining == 0)
    {
      _uiMain.DisplayLine("MISSION FAILED: ENTERPRISE RAN OUT OF TIME.");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
    }
    else if (_data.Resigned)
    {
      _uiMain.DisplayLine("MISSION FAILED: COMMANDER RESIGNED.");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("");
    }
  }

  private void ShowHelp()
  {
    _uiMain.Clear();
    _uiMain.DisplayLine(HelpText);
  }

  private async Task DamageControl()
  {
    var totalDamage = _data.NavigationDamage +
                      _data.ShortRangeScanDamage +
                      _data.LongRangeScanDamage +
                      _data.ShieldControlDamage +
                      _data.ComputerDamage +
                      _data.PhotonDamage +
                      _data.PhaserDamage;
    var timeToRepair = 1 + totalDamage / 7; // repairs always take a minimum of 1 day

    _uiMain.DisplayLine("Technicians standing by to effect repairs to your ship");
    _uiMain.DisplayLine($"Estimated time to repair: {timeToRepair} stardates.");
    var choice = await _uiMain.InputString("Will you authorize the repair order (Y/N)? ");

    if (choice == "y")
    {
      _data.ResetDamage();
      _data.TimeRemaining -= timeToRepair;
      _data.StarDate += timeToRepair;
    }
  }

  #region Load/Save Games

  private static string GetDataFilePath(string slotName = "")
  {
    // TODO   use local storage
    var dataFileName = DataFileRootName + slotName + DataFileExtension;
    var dataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dataFileName);

    return dataFilePath;
  }

  private static string GetDataFilePath(int slot)
  {
    Debug.Assert(slot is >= 0 and <= 9, "Only slots 0-9 are supported");

    return GetDataFilePath(slot.ToString());
  }

  /// <summary>
  /// load game from specified file
  /// </summary>
  /// <param name="dataFilePath">full path to file to load game from</param>
  private void LoadGame(string dataFilePath)
  {
    // TODO   use local storage
    if (!File.Exists(dataFilePath))
    {
      // never saved a game, so save this one
      SaveGame();
    }

    Debug.Assert(File.Exists(dataFilePath));

    // TODO   use local storage
    var dataSerializer = new XmlSerializer(typeof(StarTrekData));
    using (var fs = new FileStream(dataFilePath, FileMode.Open))
    {
      _data = (StarTrekData)dataSerializer.Deserialize(fs);
    }

    PrintMission();
  }

  /// <summary>
  /// used by system for default load game
  /// </summary>
  private void LoadGame()
  {
    var dataFilePath = GetDataFilePath();

    LoadGame(dataFilePath);
  }

  private async Task LoadUserGame()
  {
    const string Separator = ",";
    var prompt = "Enter save slot (";

    // work out which slots have a corresponding file
    for (var i = 0; i <= 9; i++)
    {
      var thisDataFilePath = GetDataFilePath(i);
      // TODO   use local storage
      if (File.Exists(thisDataFilePath))
      {
        prompt += i.ToString();
        prompt += Separator;
      }
    }

    // remove trailing separator
    if (prompt.EndsWith(Separator))
    {
      prompt = prompt.Remove(prompt.Length - Separator.Length, Separator.Length);
    }

    prompt += ") ";

    var res = await GetUserGameDataFilePath(prompt);
    if (res.IsConfirmed)
    {
      // TODO   use local storage
      var dataFilePath = res.Value;
      if (File.Exists(dataFilePath))
      {
        LoadGame(dataFilePath);
      }
      else
      {
        _uiMain.DisplayLine("Selected slot does not exist ");
      }
    }
  }

  /// <summary>
  /// save current game to specified file
  /// </summary>
  /// <param name="dataFilePath">full path to file to save game into</param>
  private void SaveGame(string dataFilePath)
  {
    // TODO   use local storage
    var dataSerializer = new XmlSerializer(typeof(StarTrekData));
    using var sw = new StreamWriter(dataFilePath);
    dataSerializer.Serialize(sw, _data);
  }

  /// <summary>
  /// used by system for default save game
  /// </summary>
  private void SaveGame()
  {
    var dataFilePath = GetDataFilePath();

    SaveGame(dataFilePath);
  }

  private async Task SaveUserGame()
  {
    var res = await GetUserGameDataFilePath("Enter save slot (0-9) ");
    if (res.IsConfirmed)
    {
      SaveGame(res.Value);
    }
  }

  private async Task<ConfirmedText> GetUserGameDataFilePath(string prompt)
  {
    var dataFilePath = string.Empty;
    var res = await InputInt(_uiMain, prompt);
    var slot = res.Value;
    if (!res.IsConfirmed)
    {
      return new ConfirmedText { IsConfirmed = false, Value = dataFilePath };
    }

    if (slot < 0 || slot > 9)
    {
      _uiMain.DisplayLine("Invalid save slot ");
      return new ConfirmedText { IsConfirmed = false, Value = dataFilePath };
    }

    dataFilePath = GetDataFilePath(slot);
    return new ConfirmedText { IsConfirmed = false, Value = dataFilePath };
  }

  #endregion

  private void ResignCommission()
  {
    _data.Resigned = true;

    _uiMain.DisplayLine($"There were {_data.Klingons} Klingon Battlecruisers left at the");
    _uiMain.DisplayLine(" end of your mission.");
    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("");
  }

  private static double ComputeDirection(int x1, int y1, int x2, int y2)
  {
    double direction;
    if (x1 == x2)
    {
      direction = y1 < y2 ? 7 : 3;
    }
    else if (y1 == y2)
    {
      direction = x1 < x2 ? 1 : 5;
    }
    else
    {
      double dy = Math.Abs(y2 - y1);
      double dx = Math.Abs(x2 - x1);
      var angle = Math.Atan2(dy, dx);
      if (x1 < x2)
      {
        if (y1 < y2)
        {
          direction = 9.0 - 4.0 * angle / Math.PI;
        }
        else
        {
          direction = 1.0 + 4.0 * angle / Math.PI;
        }
      }
      else
      {
        if (y1 < y2)
        {
          direction = 5.0 + 4.0 * angle / Math.PI;
        }
        else
        {
          direction = 5.0 - 4.0 * angle / Math.PI;
        }
      }
    }

    return direction;
  }

  private async Task NavigationCalculator()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine($"Enterprise located in quadrant [{(_data.QuadrantX + 1)},{(_data.QuadrantY + 1)}].");
    _uiMain.DisplayLine("");

    var resX = await InputDouble(_uiMain, "Enter destination quadrant X (1--8): ");
    var quadX = resX.Value;
    if (!resX.IsConfirmed || quadX < 1 || quadX > 8)
    {
      _uiMain.DisplayLine("Invalid X coordinate.");
      _uiMain.DisplayLine("");
      return;
    }

    var resY = await InputDouble(_uiMain, "Enter destination quadrant Y (1--8): ");
    var quadY = resY.Value;
    if (!resY.IsConfirmed || quadY < 1 || quadY > 8)
    {
      _uiMain.DisplayLine("Invalid Y coordinate.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");
    var qx = ((int)(quadX)) - 1;
    var qy = ((int)(quadY)) - 1;
    if (qx == _data.QuadrantX && qy == _data.QuadrantY)
    {
      _uiMain.DisplayLine("That is the current location of the Enterprise.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine($"Direction: {ComputeDirection(_data.QuadrantX, _data.QuadrantY, qx, qy):#.##}");
    _uiMain.DisplayLine($"Distance:  {Distance(_data.QuadrantX, _data.QuadrantY, qx, qy):##.##}");
    _uiMain.DisplayLine("");
  }

  private void StarBaseCalculator()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    if (_data.Quadrants[_data.QuadrantY][_data.QuadrantX].StarBase)
    {
      _uiMain.DisplayLine($"Starbase in sector [{(_data.StarBaseX + 1)},{(_data.StarBaseY + 1)}].");
      _uiMain.DisplayLine($"Direction: {ComputeDirection(_data.SectorX, _data.SectorY, _data.StarBaseX, _data.StarBaseY):#.##}");
      _uiMain.DisplayLine($"Distance:  {Distance(_data.SectorX, _data.SectorY, _data.StarBaseX, _data.StarBaseY) / 8:##.##}");
    }
    else
    {
      _uiMain.DisplayLine("There are no starbases in this quadrant.");
    }

    _uiMain.DisplayLine("");
  }

  private void PhotonTorpedoCalculator()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    if (_data.KlingonShips.Count == 0)
    {
      _uiMain.DisplayLine("There are no Klingon ships in this quadrant.");
      _uiMain.DisplayLine("");
      return;
    }

    foreach (var ship in _data.KlingonShips)
    {
      _uiMain.DisplayLine(string.Format("Direction {2:#.##}: Klingon ship in sector [{0},{1}].",
        (ship.SectorX + 1), (ship.SectorY + 1),
        ComputeDirection(_data.SectorX, _data.SectorY, ship.SectorX, ship.SectorY)));
    }

    _uiMain.DisplayLine("");
  }

  private void DisplayStatus()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine($"               Time Remaining: {_data.TimeRemaining}");
    _uiMain.DisplayLine($"      Klingon Ships Remaining: {_data.Klingons}");
    _uiMain.DisplayLine($"                    Starbases: {_data.StarBases}");
    _uiMain.DisplayLine($"           Warp Engine Damage: {_data.NavigationDamage}");
    _uiMain.DisplayLine($"   Short Range Scanner Damage: {_data.ShortRangeScanDamage}");
    _uiMain.DisplayLine($"    Long Range Scanner Damage: {_data.LongRangeScanDamage}");
    _uiMain.DisplayLine($"       Shield Controls Damage: {_data.ShieldControlDamage}");
    _uiMain.DisplayLine($"         Main Computer Damage: {_data.ComputerDamage}");
    _uiMain.DisplayLine($"Photon Torpedo Control Damage: {_data.PhotonDamage}");
    _uiMain.DisplayLine($"                Phaser Damage: {_data.PhaserDamage}");
    _uiMain.DisplayLine("");
  }

  private void DisplayGalacticRecord()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("-------------------------------------------------");
    for (var i = 0; i < 8; i++)
    {
      var sb = new StringBuilder();
      for (var j = 0; j < 8; j++)
      {
        sb.Append("| ");
        var klingonCount = 0;
        var starbaseCount = 0;
        var starCount = 0;
        var quadrant = _data.Quadrants[i][j];
        if (quadrant.Scanned)
        {
          klingonCount = quadrant.Klingons;
          starbaseCount = quadrant.StarBase ? 1 : 0;
          starCount = quadrant.Stars;
        }

        sb.Append($"{klingonCount}{starbaseCount}{starCount} ");
      }

      sb.Append("|");
      _uiMain.DisplayLine(sb.ToString());
      _uiMain.DisplayLine("-------------------------------------------------");
    }

    _uiMain.DisplayLine("");
  }

  private async Task PhaserControls()
  {
    if (_data.PhaserDamage > 0)
    {
      _uiMain.DisplayLine("Phasers are damaged. Repairs are underway.");
      _uiMain.DisplayLine("");
      return;
    }

    if (_data.KlingonShips.Count == 0)
    {
      _uiMain.DisplayLine("There are no Klingon ships in this quadrant.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("Phasers locked on target.");
    var res = await InputDouble(_uiMain, $"Enter phaser energy (1--{_data.Energy}): ");
    var phaserEnergy = res.Value;
    if (!res.IsConfirmed || phaserEnergy < 1 || phaserEnergy > _data.Energy)
    {
      _uiMain.DisplayLine("Invalid energy level.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");

    _uiMain.DisplayLine("Firing phasers...");
    var destroyedShips = new List<KlingonShip>();
    foreach (var ship in _data.KlingonShips)
    {
      _data.Energy -= (int)phaserEnergy;
      if (_data.Energy < 0)
      {
        _data.Energy = 0;
        break;
      }

      var distance = Distance(_data.SectorX, _data.SectorY, ship.SectorX, ship.SectorY);
      var deliveredEnergy = phaserEnergy * (1.0 - distance / 11.3);
      ship.ShieldLevel -= (int)deliveredEnergy;
      if (ship.ShieldLevel <= 0)
      {
        _uiMain.DisplayLine($"Klingon ship destroyed at sector [{(ship.SectorX + 1)},{(ship.SectorY + 1)}].");
        destroyedShips.Add(ship);
      }
      else
      {
        _uiMain.DisplayLine($"Hit ship at sector [{(ship.SectorX + 1)},{(ship.SectorY + 1)}]. Klingon shield strength dropped to {ship.ShieldLevel}.");
      }
    }

    foreach (var ship in destroyedShips)
    {
      _data.Quadrants[_data.QuadrantY][_data.QuadrantX].Klingons--;
      _data.Klingons--;
      _data.Sector[ship.SectorY][ship.SectorX] = SectorType.Empty;
      _data.KlingonShips.Remove(ship);
    }

    if (_data.KlingonShips.Count > 0)
    {
      _uiMain.DisplayLine("");
      KlingonsAttack();
    }

    _uiMain.DisplayLine("");
  }

  private async Task ShieldControls(bool adding, int maxTransfer)
  {
    var res = await InputDouble(_uiMain, $"Enter amount of energy (1--{maxTransfer}): ");
    var transfer = res.Value;
    if (!res.IsConfirmed || transfer < 1 || transfer > maxTransfer)
    {
      _uiMain.DisplayLine("Invalid amount of energy.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");

    if (adding)
    {
      _data.Energy -= (int)transfer;
      _data.ShieldLevel += (int)transfer;
    }
    else
    {
      _data.Energy += (int)transfer;
      _data.ShieldLevel -= (int)transfer;
    }

    _uiMain.DisplayLine($"Shield strength is now {_data.ShieldLevel}. Energy level is now {_data.Energy}.");
    _uiMain.DisplayLine("");
  }

  private void KlingonsAttack()
  {
    if (_data.KlingonShips.Count <= 0)
    {
      return;
    }

    foreach (var ship in _data.KlingonShips)
    {
      if (_data.Docked)
      {
        _uiMain.DisplayLine($"Enterprise hit by ship at sector [{(ship.SectorX + 1)},{(ship.SectorY + 1)}]. No damage due to starbase shields.");
      }
      else
      {
        var distance = Distance(_data.SectorX, _data.SectorY, ship.SectorX, ship.SectorY);
        var deliveredEnergy = 300 * _data.random.NextDouble() * (1.0 - distance / 11.3);
        _data.ShieldLevel -= (int)deliveredEnergy;
        if (_data.ShieldLevel < 0)
        {
          _data.ShieldLevel = 0;
          _data.Destroyed = true;
        }

        _uiMain.DisplayLine($"Enterprise hit by ship at sector [{(ship.SectorX + 1)},{(ship.SectorY + 1)}]. Shields dropped to {_data.ShieldLevel}.");
        if (_data.ShieldLevel == 0)
        {
          return;
        }
      }
    }
  }

  private static double Distance(double x1, double y1, double x2, double y2)
  {
    var x = x2 - x1;
    var y = y2 - y1;

    return Math.Sqrt(x * x + y * y);
  }

  private void InduceDamage(int item)
  {
    if (_data.random.Next(7) > 0)
    {
      return;
    }

    var damage = 1 + _data.random.Next(5);
    if (item < 0)
    {
      item = _data.random.Next(7);
    }

    switch (item)
    {
      case 0:
        _data.NavigationDamage = damage;
        _uiMain.DisplayLine("Warp engines are malfunctioning.");
        break;

      case 1:
        _data.ShortRangeScanDamage = damage;
        _uiMain.DisplayLine("Short range scanner is malfunctioning.");
        break;

      case 2:
        _data.LongRangeScanDamage = damage;
        _uiMain.DisplayLine("Long range scanner is malfunctioning.");
        break;

      case 3:
        _data.ShieldControlDamage = damage;
        _uiMain.DisplayLine("Shield controls are malfunctioning.");
        break;

      case 4:
        _data.ComputerDamage = damage;
        _uiMain.DisplayLine("The main computer is malfunctioning.");
        break;

      case 5:
        _data.PhotonDamage = damage;
        _uiMain.DisplayLine("Photon torpedo controls are malfunctioning.");
        break;

      case 6:
        _data.PhaserDamage = damage;
        _uiMain.DisplayLine("Phasers are malfunctioning.");
        break;

      default:
        Debug.Fail("Failed to handle item: " + item);
        break;
    }

    _uiMain.DisplayLine("");
  }

  private bool RepairDamage()
  {
    if (_data.NavigationDamage > 0)
    {
      _data.NavigationDamage--;
      if (_data.NavigationDamage == 0)
      {
        _uiMain.DisplayLine("Warp engines have been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.ShortRangeScanDamage > 0)
    {
      _data.ShortRangeScanDamage--;
      if (_data.ShortRangeScanDamage == 0)
      {
        _uiMain.DisplayLine("Short range scanner has been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.LongRangeScanDamage > 0)
    {
      _data.LongRangeScanDamage--;
      if (_data.LongRangeScanDamage == 0)
      {
        _uiMain.DisplayLine("Long range scanner has been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.ShieldControlDamage > 0)
    {
      _data.ShieldControlDamage--;
      if (_data.ShieldControlDamage == 0)
      {
        _uiMain.DisplayLine("Shield controls have been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.ComputerDamage > 0)
    {
      _data.ComputerDamage--;
      if (_data.ComputerDamage == 0)
      {
        _uiMain.DisplayLine("The main computer has been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.PhotonDamage > 0)
    {
      _data.PhotonDamage--;
      if (_data.PhotonDamage == 0)
      {
        _uiMain.DisplayLine("Photon torpedo controls have been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    if (_data.PhaserDamage > 0)
    {
      _data.PhaserDamage--;
      if (_data.PhaserDamage == 0)
      {
        _uiMain.DisplayLine("Phasers have been repaired.");
      }

      _uiMain.DisplayLine("");
      return true;
    }

    return false;
  }

  private void LongRangeScan()
  {
    _uiMain.Clear();

    if (_data.LongRangeScanDamage > 0)
    {
      _uiMain.DisplayLine("Long range scanner is damaged. Repairs are underway.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("-------------------");
    for (var i = _data.QuadrantY - 1; i <= _data.QuadrantY + 1; i++)
    {
      var sb = new StringBuilder();
      for (var j = _data.QuadrantX - 1; j <= _data.QuadrantX + 1; j++)
      {
        sb.Append("| ");
        var klingonCount = 0;
        var starbaseCount = 0;
        var starCount = 0;
        if (i >= 0 && j >= 0 && i < 8 && j < 8)
        {
          var quadrant = _data.Quadrants[i][j];
          quadrant.Scanned = true;
          klingonCount = quadrant.Klingons;
          starbaseCount = quadrant.StarBase ? 1 : 0;
          starCount = quadrant.Stars;
        }

        sb.Append($"{klingonCount}{starbaseCount}{starCount} ");
      }

      sb.Append("|");
      _uiMain.DisplayLine(sb.ToString());
      _uiMain.DisplayLine("-------------------");
    }

    _uiMain.DisplayLine("");
  }

  private async Task TorpedoControl()
  {
    if (_data.PhotonDamage > 0)
    {
      _uiMain.DisplayLine("Photon torpedo control is damaged. Repairs are underway.");
      _uiMain.DisplayLine("");
      return;
    }

    if (_data.PhotonTorpedoes == 0)
    {
      _uiMain.DisplayLine("Photon torpedoes exhausted.");
      _uiMain.DisplayLine("");
      return;
    }

    if (_data.KlingonShips.Count == 0)
    {
      _uiMain.DisplayLine("There are no Klingon ships in this quadrant.");
      _uiMain.DisplayLine("");
      return;
    }

    var res = await InputDouble(_uiMain, "Enter firing direction (1.0--9.0): ");
    var direction = res.Value;
    if (!res.IsConfirmed || direction < 1.0 || direction > 9.0)
    {
      _uiMain.DisplayLine("Invalid direction.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("Photon torpedo fired...");
    _data.PhotonTorpedoes--;

    var angle = -(Math.PI * (direction - 1.0) / 4.0);
    if (_data.random.Next(3) == 0)
    {
      angle += ((1.0 - 2.0 * _data.random.NextDouble()) * Math.PI * 2.0) * 0.03;
    }

    double x = _data.SectorX;
    double y = _data.SectorY;
    var vx = Math.Cos(angle) / 20;
    var vy = Math.Sin(angle) / 20;
    int lastX = -1, lastY = -1;
    var newX = _data.SectorX;
    var newY = _data.SectorY;

    while (x >= 0 && y >= 0 && Math.Round(x) < 8 && Math.Round(y) < 8)
    {
      newX = (int)Math.Round(x);
      newY = (int)Math.Round(y);
      if (lastX != newX || lastY != newY)
      {
        _uiMain.DisplayLine($"  [{newX + 1},{newY + 1}]");
        lastX = newX;
        lastY = newY;
      }

      foreach (var ship in _data.KlingonShips)
      {
        if (ship.SectorX == newX && ship.SectorY == newY)
        {
          _uiMain.DisplayLine($"Klingon ship destroyed at sector [{(ship.SectorX + 1)},{(ship.SectorY + 1)}].");
          _data.Sector[ship.SectorY][ship.SectorX] = SectorType.Empty;
          _data.Klingons--;
          _data.KlingonShips.Remove(ship);
          _data.Quadrants[_data.QuadrantY][_data.QuadrantX].Klingons--;
          goto label;
        }
      }

      switch (_data.Sector[newY][newX])
      {
        case SectorType.Starbase:
          _data.StarBases--;
          _data.Quadrants[_data.QuadrantY][_data.QuadrantX].StarBase = false;
          _data.Sector[newY][newX] = SectorType.Empty;
          _uiMain.DisplayLine($"The Enterprise destroyed a Federation starbase at sector [{newX + 1},{newY + 1}]!");
          goto label;

        case SectorType.Star:
          _uiMain.DisplayLine($"The torpedo was captured by a star's gravitational field at sector [{newX + 1},{newY + 1}].");
          goto label;

        case SectorType.Empty:
        case SectorType.Enterprise:
        case SectorType.Klingon:
          break;

        default:
          Debug.Fail("Failed to handle: " + _data.Sector[newY][newX]);
          break;
      }

      x += vx;
      y += vy;
    }

    _uiMain.DisplayLine("Photon torpedo failed to hit anything.");

    label:

    if (_data.KlingonShips.Count > 0)
    {
      _uiMain.DisplayLine("");
      KlingonsAttack();
    }

    _uiMain.DisplayLine("");
  }

  private async Task Navigation()
  {
    var maxWarpFactor = 8.0;
    if (_data.NavigationDamage > 0)
    {
      maxWarpFactor = 0.2 + _data.random.Next(9) / 10.0;
      _uiMain.DisplayLine($"Warp engines damaged. Maximum warp factor: {maxWarpFactor}");
      _uiMain.DisplayLine("");
    }

    var resCourse = await InputDouble(_uiMain, "Enter course (1.0--9.0): ");
    var direction = resCourse.Value;
    if (!resCourse.IsConfirmed || direction < 1.0 || direction > 9.0)
    {
      _uiMain.DisplayLine("Invalid course.");
      _uiMain.DisplayLine("");
      return;
    }

    var resWarp = await InputDouble(_uiMain, $"Enter warp factor (0.1--{maxWarpFactor}): ");
    var distance = resWarp.Value;
    if (!resWarp.IsConfirmed || distance < 0.1 || distance > maxWarpFactor)
    {
      _uiMain.DisplayLine("Invalid warp factor.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");

    distance *= 8;
    var energyRequired = (int)distance;
    if (energyRequired >= _data.Energy)
    {
      _uiMain.DisplayLine("Unable to comply. Insufficient energy to travel that speed.");
      _uiMain.DisplayLine("");
      return;
    }
    else
    {
      _uiMain.DisplayLine("Warp engines engaged.");
      _uiMain.DisplayLine("");
      _data.Energy -= energyRequired;
    }

    int lastQuadX = _data.QuadrantX, lastQuadY = _data.QuadrantY;
    var angle = -(Math.PI * (direction - 1.0) / 4.0);
    double x = _data.QuadrantX * 8 + _data.SectorX;
    double y = _data.QuadrantY * 8 + _data.SectorY;
    var dx = distance * Math.Cos(angle);
    var dy = distance * Math.Sin(angle);
    var vx = dx / 1000;
    var vy = dy / 1000;
    int quadX, quadY;
    int lastSectX = _data.SectorX, lastSectY = _data.SectorY;
    _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Empty;
    for (var i = 0; i < 1000; i++)
    {
      x += vx;
      y += vy;
      quadX = ((int)Math.Round(x)) / 8;
      quadY = ((int)Math.Round(y)) / 8;
      if (quadX == _data.QuadrantX && quadY == _data.QuadrantY)
      {
        var sectX = ((int)Math.Round(x)) % 8;
        var sectY = ((int)Math.Round(y)) % 8;
        if (sectX < 0 || sectY < 0)
        {
          _data.SectorX = lastSectX;
          _data.SectorY = lastSectY;
          _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Enterprise;
          _uiMain.DisplayLine("Course would go outside the universe.");
          _uiMain.DisplayLine("");
          goto label;
        }

        if (_data.Sector[sectY][sectX] != SectorType.Empty)
        {
          _data.SectorX = lastSectX;
          _data.SectorY = lastSectY;
          _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Enterprise;
          _uiMain.DisplayLine("Encountered obstacle within quadrant.");
          _uiMain.DisplayLine("");
          goto label;
        }

        lastSectX = sectX;
        lastSectY = sectY;
      }
    }

    if (x < 0)
    {
      x = 0;
    }
    else if (x > 63)
    {
      x = 63;
    }

    if (y < 0)
    {
      y = 0;
    }
    else if (y > 63)
    {
      y = 63;
    }

    quadX = ((int)Math.Round(x)) / 8;
    quadY = ((int)Math.Round(y)) / 8;
    _data.SectorX = ((int)Math.Round(x)) % 8;
    _data.SectorY = ((int)Math.Round(y)) % 8;
    if (quadX != _data.QuadrantX || quadY != _data.QuadrantY)
    {
      _data.QuadrantX = quadX;
      _data.QuadrantY = quadY;
      GenerateSector();
    }
    else
    {
      _data.QuadrantX = quadX;
      _data.QuadrantY = quadY;
      _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Enterprise;
    }

    label:

    if (IsDockingLocation(_data.SectorY, _data.SectorX))
    {
      _data.ResetSupplies();
      _data.ResetDamage();

      _data.ShieldLevel = 0;

      _data.Docked = true;
    }
    else
    {
      _data.Docked = false;
    }

    if (lastQuadX != _data.QuadrantX || lastQuadY != _data.QuadrantY)
    {
      _data.TimeRemaining--;
      _data.StarDate++;
    }

    ShortRangeScan();

    if (_data.Docked)
    {
      _uiMain.DisplayLine("Lowering shields as part of docking sequence...");
      _uiMain.DisplayLine("Enterprise successfully docked with starbase.");
      _uiMain.DisplayLine("");
    }
    else
    {
      if (_data.Quadrants[_data.QuadrantY][_data.QuadrantX].Klingons > 0
          && lastQuadX == _data.QuadrantX && lastQuadY == _data.QuadrantY)
      {
        KlingonsAttack();
        _uiMain.DisplayLine("");
      }
      else if (!RepairDamage())
      {
        InduceDamage(-1);
      }
    }
  }

  private static async Task<ConfirmedDouble> InputDouble(IUserInterface ui, string prompt)
  {
    try
    {
      var numStr = await ui.InputString(prompt);
      var value = double.Parse(numStr);
      return new ConfirmedDouble { IsConfirmed = true, Value = value };
    }
    catch
    {
      // ignored
    }

    return new ConfirmedDouble { IsConfirmed = false };
  }

  private static async Task<ConfirmedInteger> InputInt(IUserInterface ui, string prompt)
  {
    try
    {
      var numStr = await ui.InputString(prompt);
      var value = int.Parse(numStr);
      return new ConfirmedInteger { IsConfirmed = true, Value = value };
    }
    catch
    {
      // ignored
    }

    return new ConfirmedInteger { IsConfirmed = false };
  }

  private void GenerateSector()
  {
    var quadrant = _data.Quadrants[_data.QuadrantY][_data.QuadrantX];
    var starbase = quadrant.StarBase;
    var stars = quadrant.Stars;
    var klingons = quadrant.Klingons;
    _data.KlingonShips.Clear();
    for (var i = 0; i < 8; i++)
    {
      for (var j = 0; j < 8; j++)
      {
        _data.Sector[i][j] = SectorType.Empty;
      }
    }

    _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Enterprise;
    while (starbase || stars > 0 || klingons > 0)
    {
      var i = _data.random.Next(8);
      var j = _data.random.Next(8);
      if (IsSectorRegionEmpty(i, j))
      {
        if (starbase)
        {
          starbase = false;
          _data.Sector[i][j] = SectorType.Starbase;
          _data.StarBaseY = i;
          _data.StarBaseX = j;
        }
        else if (stars > 0)
        {
          _data.Sector[i][j] = SectorType.Star;
          stars--;
        }
        else if (klingons > 0)
        {
          _data.Sector[i][j] = SectorType.Klingon;
          var klingonShip = new KlingonShip
          {
            ShieldLevel = 300 + _data.random.Next(200),
            SectorY = i,
            SectorX = j
          };
          _data.KlingonShips.Add(klingonShip);
          klingons--;
        }
      }
    }
  }

  private bool IsDockingLocation(int i, int j)
  {
    for (var y = i - 1; y <= i + 1; y++)
    {
      for (var x = j - 1; x <= j + 1; x++)
      {
        if (ReadSector(y, x) == SectorType.Starbase)
        {
          return true;
        }
      }
    }

    return false;
  }

  private bool IsSectorRegionEmpty(int i, int j)
  {
    for (var y = i - 1; y <= i + 1; y++)
    {
      if (ReadSector(y, j - 1) != SectorType.Empty
          && ReadSector(y, j + 1) != SectorType.Empty)
      {
        return false;
      }
    }

    return ReadSector(i, j) == SectorType.Empty;
  }

  private SectorType ReadSector(int i, int j)
  {
    if (i < 0 || j < 0 || i > 7 || j > 7)
    {
      return SectorType.Empty;
    }

    return _data.Sector[i][j];
  }

  private void ShortRangeScan()
  {
    _uiMain.Clear();

    if (_data.ShortRangeScanDamage > 0)
    {
      _uiMain.DisplayLine("Short range scanner is damaged. Repairs are underway.");
      _uiMain.DisplayLine("");
    }
    else
    {
      var quadrant = _data.Quadrants[_data.QuadrantY][_data.QuadrantX];
      quadrant.Scanned = true;
      PrintSector(quadrant);
    }

    _uiMain.DisplayLine("");
  }

  private void PrintSector(Quadrant quadrant)
  {
    var condition = "GREEN";
    if (quadrant.Klingons > 0)
    {
      condition = "RED";
    }
    else if (_data.Energy < 300)
    {
      condition = "YELLOW";
    }

    _uiMain.DisplayLine($"-=--=--=--=--=--=--=--=-             Region: {quadrant.Name}");
    PrintSectorRow(0, $"           Quadrant: [{_data.QuadrantX + 1},{_data.QuadrantY + 1}]");
    PrintSectorRow(1, $"             Sector: [{_data.SectorX + 1},{_data.SectorY + 1}]");
    PrintSectorRow(2, $"           Stardate: {_data.StarDate}");
    PrintSectorRow(3, $"     Time remaining: {_data.TimeRemaining}");
    PrintSectorRow(4, $"          Condition: {condition}");
    PrintSectorRow(5, $"             Energy: {_data.Energy}");
    PrintSectorRow(6, $"            Shields: {_data.ShieldLevel}");
    PrintSectorRow(7, $"   Photon Torpedoes: {_data.PhotonTorpedoes}");
    _uiMain.DisplayLine($"-=--=--=--=--=--=--=--=-             Docked: {_data.Docked}");

    if (quadrant.Klingons > 0)
    {
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine($"Condition RED: Klingon ship{(quadrant.Klingons == 1 ? "" : "s")} detected.");
      if (_data.ShieldLevel == 0 && !_data.Docked)
      {
        _uiMain.DisplayLine("Warning: Shields are down.");
      }
    }
    else if (_data.Energy < 300)
    {
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine("Condition YELLOW: Low energy level.");
      condition = "YELLOW";
    }
  }

  private void PrintSectorRow(int row, string suffix)
  {
    var sb = new StringBuilder();
    for (var column = 0; column < 8; column++)
    {
      switch (_data.Sector[row][column])
      {
        case SectorType.Empty:
          sb.Append("   ");
          break;

        case SectorType.Enterprise:
          sb.Append("<*>");
          break;

        case SectorType.Klingon:
          sb.Append("+K+");
          break;

        case SectorType.Star:
          sb.Append(" * ");
          break;

        case SectorType.Starbase:
          sb.Append(">!<");
          break;

        default:
          Debug.Fail("Failed to handle: " + _data.Sector[row][column]);
          break;
      }
    }

    if (suffix != null)
    {
      sb.Append(suffix);
    }

    _uiMain.DisplayLine(sb.ToString());
  }

  private void PrintMission()
  {
    _uiMain.DisplayLine($"Mission: Destroy {_data.Klingons} Klingon ships in {_data.TimeRemaining} stardates with {_data.StarBases} starbases.");
    _uiMain.DisplayLine("");
  }

  private void InitializeGame()
  {
    _data = new StarTrekData();

    var names = QuadrantNames.ToList();

    for (var i = 0; i < 8; i++)
    {
      for (var j = 0; j < 8; j++)
      {
        var index = _data.random.Next(names.Count);
        var quadrant = new Quadrant();
        _data.Quadrants[i][j] = quadrant;
        quadrant.Name = names[index];
        quadrant.Stars = 1 + _data.random.Next(8);
        names.RemoveAt(index);
      }
    }

    var klingonCount = _data.Klingons;
    var starbaseCount = _data.StarBases;
    while (klingonCount > 0 || starbaseCount > 0)
    {
      var i = _data.random.Next(8);
      var j = _data.random.Next(8);
      var quadrant = _data.Quadrants[i][j];
      if (!quadrant.StarBase)
      {
        quadrant.StarBase = true;
        starbaseCount--;
      }

      if (quadrant.Klingons < 3)
      {
        quadrant.Klingons++;
        klingonCount--;
      }
    }
  }

  private void PrintStrings(IEnumerable<string> strings)
  {
    foreach (var str in strings)
    {
      _uiMain.DisplayLine(str);
    }

    _uiMain.DisplayLine("");
  }

  #region ConfirmedValues

  private abstract class ConfirmedValue<T>
  {
    public bool IsConfirmed { get; init; }
    public T Value { get; init; }
  }

  private sealed class ConfirmedText : ConfirmedValue<string>
  {
  }

  private sealed class ConfirmedDouble : ConfirmedValue<double>
  {
  }

  private sealed class ConfirmedInteger : ConfirmedValue<int>
  {
  }

  #endregion
}
