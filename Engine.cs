namespace Trek.Net;

using System.Diagnostics;
using System.Reflection;
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

    _uiMain.CommandSelected += UI_Main_CommandSelected;

    InitializeGame();
  }

  private void UI_Main_CommandSelected(object sender, CommandItemInfo cmd)
  {
    PrintGameStatus();
    if (IsGameFinished())
    {
      if (NewGame())
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
        Navigation();
        break;

      case "srs":
        ShortRangeScan();
        break;

      case "lrs":
        LongRangeScan();
        break;

      case "pha":
        PhaserControls();
        break;

      case "tor":
        TorpedoControl();
        break;

      case "dam":
        DamageControl();
        break;

      case "sav":
        SaveUserGame();
        break;

      case "ldg":
        LoadUserGame();
        break;

      case "hlp":
        ShowHelp();
        break;

      case "xxx":
        ResignCommission();
        if (NewGame())
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
        ShieldControls(true, _data.Energy);
        break;

      case "sub":
        ShieldControls(false, _data.ShieldLevel);
        break;

      #endregion

      #region Computer commands

      case "rec":
        DisplayGalaticRecord();
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
        NavigationCalculator();
        break;

      #endregion

      default:
        Debug.Fail("Failed to handle: " + cmd.CommandId);
        break;
    }
  }

  #region String Constants

  #region Title

  public static readonly string[] TitleStrings =
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

  public static readonly string[] QuadrantNames =
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

  private static readonly string MainCommandPrompt = "Enter command: ";
  private static readonly string MainCommandTitle = "--- Commands -----------------";

  private static readonly List<CommandItemInfo> MainCommandItems = new List<CommandItemInfo>
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

  private static readonly CommandInfo MainCommands = new CommandInfo(MainCommandPrompt, MainCommandTitle, MainCommandItems);

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

  private bool NewGame()
  {
    _uiMain.DisplayLine("The Federation is in need of a new starship commander");
    _uiMain.DisplayLine(" for a similar mission.");
    _uiMain.DisplayLine("");

    string command = _uiMain.InputString("If there is a volunteer, let him step forward and enter 'aye': ");
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
    // load from resource
    Assembly assy = Assembly.GetExecutingAssembly();
    StreamReader sr = new StreamReader(assy.GetManifestResourceStream("SuperStarTrek.StarTrek.txt"));

    _uiMain.Clear();
    _uiMain.DisplayLine(sr.ReadToEnd());
  }

  private void DamageControl()
  {
    var TotalDamage = _data.NavigationDamage +
                      _data.ShortRangeScanDamage +
                      _data.LongRangeScanDamage +
                      _data.ShieldControlDamage +
                      _data.ComputerDamage +
                      _data.PhotonDamage +
                      _data.PhaserDamage;
    var TimeToRepair = 1 + (int)TotalDamage / 7; // repairs always take a minimum of 1 day

    _uiMain.DisplayLine("Technicians standing by to effect repairs to your ship");
    _uiMain.DisplayLine(string.Format("Estimated time to repair: {0} stardates.", TimeToRepair));
    var Choice = _uiMain.InputString("Will you authorize the repair order (Y/N)? ");

    if (Choice == "y")
    {
      _data.ResetDamage();
      _data.TimeRemaining -= TimeToRepair;
      _data.StarDate += TimeToRepair;
    }
  }

  #region Load/Save Games

  private string GetDataFilePath(string SlotName)
  {
    var DataFileName = DataFileRootName + SlotName + DataFileExtension;
    var DataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DataFileName);

    return DataFilePath;
  }

  /// <summary>
  /// used for system default save/load file
  /// </summary>
  /// <returns></returns>
  private string GetDataFilePath()
  {
    return GetDataFilePath("");
  }

  private string GetDataFilePath(int Slot)
  {
    Debug.Assert(Slot >= 0 && Slot <= 9, "Only slots 0-9 are supported");

    return GetDataFilePath(Slot.ToString());
  }

  /// <summary>
  /// load game from specified file
  /// </summary>
  /// <param name="DataFilePath">full path to file to load game from</param>
  private void LoadGame(string DataFilePath)
  {
    if (!File.Exists(DataFilePath))
    {
      // never saved a game, so save this one
      SaveGame();
    }

    Debug.Assert(File.Exists(DataFilePath));

    XmlSerializer DataSerializer = new XmlSerializer(typeof(StarTrekData));
    using (FileStream fs = new FileStream(DataFilePath, FileMode.Open))
    {
      _data = (StarTrekData)DataSerializer.Deserialize(fs);
    }

    PrintMission();
  }

  /// <summary>
  /// used by system for default load game
  /// </summary>
  public void LoadGame()
  {
    var DataFilePath = GetDataFilePath();

    LoadGame(DataFilePath);
  }

  private void LoadUserGame()
  {
    const string Separator = ",";
    var prompt = "Enter save slot (";

    // work out which slots have a corresponding file
    for (var i = 0; i <= 9; i++)
    {
      var ThisDataFilePath = GetDataFilePath(i);
      if (File.Exists(ThisDataFilePath))
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

    var DataFilePath = string.Empty;
    if (GetUserGameDataFilePath(prompt, ref DataFilePath))
    {
      if (File.Exists(DataFilePath))
      {
        LoadGame(DataFilePath);
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
  /// <param name="DataFilePath">full path to file to save game into</param>
  private void SaveGame(string DataFilePath)
  {
    XmlSerializer DataSerializer = new XmlSerializer(typeof(StarTrekData));
    using (var sw = new StreamWriter(DataFilePath))
    {
      DataSerializer.Serialize(sw, _data);
    }
  }

  /// <summary>
  /// used by system for default save game
  /// </summary>
  public void SaveGame()
  {
    var DataFilePath = GetDataFilePath();

    SaveGame(DataFilePath);
  }

  private void SaveUserGame()
  {
    var DataFilePath = string.Empty;

    if (GetUserGameDataFilePath("Enter save slot (0-9) ", ref DataFilePath))
    {
      SaveGame(DataFilePath);
    }
  }

  private bool GetUserGameDataFilePath(string prompt, ref string DataFilePath)
  {
    var Slot = 0;
    if (InputInt(_uiMain, prompt, out Slot))
    {
      if (Slot < 0 || Slot > 9)
      {
        _uiMain.DisplayLine("Invalid save slot ");
        return false;
      }

      DataFilePath = GetDataFilePath(Slot);
      return true;
    }

    return false;
  }

  #endregion

  private void ResignCommission()
  {
    _data.Resigned = true;

    _uiMain.DisplayLine(string.Format("There were {0} Klingon Battlecruisers left at the", _data.Klingons));
    _uiMain.DisplayLine(" end of your mission.");
    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("");
  }

  private static double ComputeDirection(int x1, int y1, int x2, int y2)
  {
    double direction = 0;
    if (x1 == x2)
    {
      if (y1 < y2)
      {
        direction = 7;
      }
      else
      {
        direction = 3;
      }
    }
    else if (y1 == y2)
    {
      if (x1 < x2)
      {
        direction = 1;
      }
      else
      {
        direction = 5;
      }
    }
    else
    {
      double dy = Math.Abs(y2 - y1);
      double dx = Math.Abs(x2 - x1);
      double angle = Math.Atan2(dy, dx);
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

  private void NavigationCalculator()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine(string.Format("Enterprise located in quadrant [{0},{1}].", (_data.QuadrantX + 1), (_data.QuadrantY + 1)));
    _uiMain.DisplayLine("");

    double quadX;
    double quadY;

    if (!InputDouble(_uiMain, "Enter destination quadrant X (1--8): ", out quadX) || quadX < 1 || quadX > 8)
    {
      _uiMain.DisplayLine("Invalid X coordinate.");
      _uiMain.DisplayLine("");
      return;
    }

    if (!InputDouble(_uiMain, "Enter destination quadrant Y (1--8): ", out quadY) || quadY < 1 || quadY > 8)
    {
      _uiMain.DisplayLine("Invalid Y coordinate.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");
    int qx = ((int)(quadX)) - 1;
    int qy = ((int)(quadY)) - 1;
    if (qx == _data.QuadrantX && qy == _data.QuadrantY)
    {
      _uiMain.DisplayLine("That is the current location of the Enterprise.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine(string.Format("Direction: {0:#.##}", ComputeDirection(_data.QuadrantX, _data.QuadrantY, qx, qy)));
    _uiMain.DisplayLine(string.Format("Distance:  {0:##.##}", Distance(_data.QuadrantX, _data.QuadrantY, qx, qy)));
    _uiMain.DisplayLine("");
  }

  private void StarBaseCalculator()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    if (_data.Quadrants[_data.QuadrantY][_data.QuadrantX].StarBase)
    {
      _uiMain.DisplayLine(string.Format("Starbase in sector [{0},{1}].", (_data.StarBaseX + 1), (_data.StarBaseY + 1)));
      _uiMain.DisplayLine(string.Format("Direction: {0:#.##}", ComputeDirection(_data.SectorX, _data.SectorY, _data.StarBaseX, _data.StarBaseY)));
      _uiMain.DisplayLine(string.Format("Distance:  {0:##.##}", Distance(_data.SectorX, _data.SectorY, _data.StarBaseX, _data.StarBaseY) / 8));
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

    foreach (KlingonShip ship in _data.KlingonShips)
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
    _uiMain.DisplayLine(string.Format("               Time Remaining: {0}", _data.TimeRemaining));
    _uiMain.DisplayLine(string.Format("      Klingon Ships Remaining: {0}", _data.Klingons));
    _uiMain.DisplayLine(string.Format("                    Starbases: {0}", _data.StarBases));
    _uiMain.DisplayLine(string.Format("           Warp Engine Damage: {0}", _data.NavigationDamage));
    _uiMain.DisplayLine(string.Format("   Short Range Scanner Damage: {0}", _data.ShortRangeScanDamage));
    _uiMain.DisplayLine(string.Format("    Long Range Scanner Damage: {0}", _data.LongRangeScanDamage));
    _uiMain.DisplayLine(string.Format("       Shield Controls Damage: {0}", _data.ShieldControlDamage));
    _uiMain.DisplayLine(string.Format("         Main Computer Damage: {0}", _data.ComputerDamage));
    _uiMain.DisplayLine(string.Format("Photon Torpedo Control Damage: {0}", _data.PhotonDamage));
    _uiMain.DisplayLine(string.Format("                Phaser Damage: {0}", _data.PhaserDamage));
    _uiMain.DisplayLine("");
  }

  private void DisplayGalaticRecord()
  {
    _uiMain.Clear();

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("-------------------------------------------------");
    for (int i = 0; i < 8; i++)
    {
      StringBuilder sb = new StringBuilder();
      for (int j = 0; j < 8; j++)
      {
        sb.Append("| ");
        int klingonCount = 0;
        int starbaseCount = 0;
        int starCount = 0;
        Quadrant quadrant = _data.Quadrants[i][j];
        if (quadrant.Scanned)
        {
          klingonCount = quadrant.Klingons;
          starbaseCount = quadrant.StarBase ? 1 : 0;
          starCount = quadrant.Stars;
        }

        sb.Append(string.Format("{0}{1}{2} ", klingonCount, starbaseCount, starCount));
      }

      sb.Append("|");
      _uiMain.DisplayLine(sb.ToString());
      _uiMain.DisplayLine("-------------------------------------------------");
    }

    _uiMain.DisplayLine("");
  }

  private void PhaserControls()
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

    double phaserEnergy;
    _uiMain.DisplayLine("Phasers locked on target.");
    if (!InputDouble(_uiMain, string.Format("Enter phaser energy (1--{0}): ", _data.Energy), out phaserEnergy) || phaserEnergy < 1 || phaserEnergy > _data.Energy)
    {
      _uiMain.DisplayLine("Invalid energy level.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");

    _uiMain.DisplayLine("Firing phasers...");
    List<KlingonShip> destroyedShips = new List<KlingonShip>();
    foreach (KlingonShip ship in _data.KlingonShips)
    {
      _data.Energy -= (int)phaserEnergy;
      if (_data.Energy < 0)
      {
        _data.Energy = 0;
        break;
      }

      double distance = Distance(_data.SectorX, _data.SectorY, ship.SectorX, ship.SectorY);
      double deliveredEnergy = phaserEnergy * (1.0 - distance / 11.3);
      ship.ShieldLevel -= (int)deliveredEnergy;
      if (ship.ShieldLevel <= 0)
      {
        _uiMain.DisplayLine(string.Format("Klingon ship destroyed at sector [{0},{1}].",
          (ship.SectorX + 1), (ship.SectorY + 1)));
        destroyedShips.Add(ship);
      }
      else
      {
        _uiMain.DisplayLine(string.Format("Hit ship at sector [{0},{1}]. Klingon shield strength dropped to {2}.",
          (ship.SectorX + 1), (ship.SectorY + 1), ship.ShieldLevel));
      }
    }

    foreach (KlingonShip ship in destroyedShips)
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

  private void ShieldControls(bool adding, int maxTransfer)
  {
    double transfer;
    if (!InputDouble(_uiMain, string.Format("Enter amount of energy (1--{0}): ", maxTransfer), out transfer)
        || transfer < 1 || transfer > maxTransfer)
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

    _uiMain.DisplayLine(string.Format("Shield strength is now {0}. Energy level is now {1}.", _data.ShieldLevel, _data.Energy));
    _uiMain.DisplayLine("");
  }

  private bool KlingonsAttack()
  {
    if (_data.KlingonShips.Count > 0)
    {
      foreach (KlingonShip ship in _data.KlingonShips)
      {
        if (_data.Docked)
        {
          _uiMain.DisplayLine(string.Format("Enterprise hit by ship at sector [{0},{1}]. No damage due to starbase shields.",
            (ship.SectorX + 1), (ship.SectorY + 1)));
        }
        else
        {
          double distance = Distance(_data.SectorX, _data.SectorY, ship.SectorX, ship.SectorY);
          double deliveredEnergy = 300 * _data.random.NextDouble() * (1.0 - distance / 11.3);
          _data.ShieldLevel -= (int)deliveredEnergy;
          if (_data.ShieldLevel < 0)
          {
            _data.ShieldLevel = 0;
            _data.Destroyed = true;
          }

          _uiMain.DisplayLine(string.Format("Enterprise hit by ship at sector [{0},{1}]. Shields dropped to {2}.",
            (ship.SectorX + 1), (ship.SectorY + 1), _data.ShieldLevel));
          if (_data.ShieldLevel == 0)
          {
            return true;
          }
        }
      }

      return true;
    }

    return false;
  }

  private static double Distance(double x1, double y1, double x2, double y2)
  {
    double x = x2 - x1;
    double y = y2 - y1;

    return Math.Sqrt(x * x + y * y);
  }

  private void InduceDamage(int item)
  {
    if (_data.random.Next(7) > 0)
    {
      return;
    }

    int damage = 1 + _data.random.Next(5);
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
        Debug.Fail("Failed to handle item: " + item.ToString());
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
    for (int i = _data.QuadrantY - 1; i <= _data.QuadrantY + 1; i++)
    {
      StringBuilder sb = new StringBuilder();
      for (int j = _data.QuadrantX - 1; j <= _data.QuadrantX + 1; j++)
      {
        sb.Append("| ");
        int klingonCount = 0;
        int starbaseCount = 0;
        int starCount = 0;
        if (i >= 0 && j >= 0 && i < 8 && j < 8)
        {
          Quadrant quadrant = _data.Quadrants[i][j];
          quadrant.Scanned = true;
          klingonCount = quadrant.Klingons;
          starbaseCount = quadrant.StarBase ? 1 : 0;
          starCount = quadrant.Stars;
        }

        sb.Append(string.Format("{0}{1}{2} ", klingonCount, starbaseCount, starCount));
      }

      sb.Append("|");
      _uiMain.DisplayLine(sb.ToString());
      _uiMain.DisplayLine("-------------------");
    }

    _uiMain.DisplayLine("");
  }

  private void TorpedoControl()
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

    double direction;
    if (!InputDouble(_uiMain, "Enter firing direction (1.0--9.0): ", out direction) || direction < 1.0 || direction > 9.0)
    {
      _uiMain.DisplayLine("Invalid direction.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");
    _uiMain.DisplayLine("Photon torpedo fired...");
    _data.PhotonTorpedoes--;

    double angle = -(Math.PI * (direction - 1.0) / 4.0);
    if (_data.random.Next(3) == 0)
    {
      angle += ((1.0 - 2.0 * _data.random.NextDouble()) * Math.PI * 2.0) * 0.03;
    }

    double x = _data.SectorX;
    double y = _data.SectorY;
    double vx = Math.Cos(angle) / 20;
    double vy = Math.Sin(angle) / 20;
    int lastX = -1, lastY = -1;
    int newX = _data.SectorX;
    int newY = _data.SectorY;

    while (x >= 0 && y >= 0 && Math.Round(x) < 8 && Math.Round(y) < 8)
    {
      newX = (int)Math.Round(x);
      newY = (int)Math.Round(y);
      if (lastX != newX || lastY != newY)
      {
        _uiMain.DisplayLine(string.Format("  [{0},{1}]", newX + 1, newY + 1));
        lastX = newX;
        lastY = newY;
      }

      foreach (KlingonShip ship in _data.KlingonShips)
      {
        if (ship.SectorX == newX && ship.SectorY == newY)
        {
          _uiMain.DisplayLine(string.Format("Klingon ship destroyed at sector [{0},{1}].",
            (ship.SectorX + 1), (ship.SectorY + 1)));
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
          _uiMain.DisplayLine(string.Format("The Enterprise destroyed a Federation starbase at sector [{0},{1}]!",
            newX + 1, newY + 1));
          goto label;

        case SectorType.Star:
          _uiMain.DisplayLine(string.Format("The torpedo was captured by a star's gravitational field at sector [{0},{1}].",
            newX + 1, newY + 1));
          goto label;

        case SectorType.Empty:
        case SectorType.Enterprise:
        case SectorType.Klingon:
          break;

        default:
          Debug.Fail("Failed to handle: " + _data.Sector[newY][newX].ToString());
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

  private void Navigation()
  {
    double maxWarpFactor = 8.0;
    if (_data.NavigationDamage > 0)
    {
      maxWarpFactor = 0.2 + _data.random.Next(9) / 10.0;
      _uiMain.DisplayLine(string.Format("Warp engines damaged. Maximum warp factor: {0}", maxWarpFactor));
      _uiMain.DisplayLine("");
    }

    double direction, distance;
    if (!InputDouble(_uiMain, "Enter course (1.0--9.0): ", out direction)
        || direction < 1.0 || direction > 9.0)
    {
      _uiMain.DisplayLine("Invalid course.");
      _uiMain.DisplayLine("");
      return;
    }

    if (!InputDouble(_uiMain, string.Format("Enter warp factor (0.1--{0}): ", maxWarpFactor), out distance)
        || distance < 0.1 || distance > maxWarpFactor)
    {
      _uiMain.DisplayLine("Invalid warp factor.");
      _uiMain.DisplayLine("");
      return;
    }

    _uiMain.DisplayLine("");

    distance *= 8;
    int energyRequired = (int)distance;
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
    double angle = -(Math.PI * (direction - 1.0) / 4.0);
    double x = _data.QuadrantX * 8 + _data.SectorX;
    double y = _data.QuadrantY * 8 + _data.SectorY;
    double dx = distance * Math.Cos(angle);
    double dy = distance * Math.Sin(angle);
    double vx = dx / 1000;
    double vy = dy / 1000;
    int quadX, quadY, sectX, sectY, lastSectX = _data.SectorX, lastSectY = _data.SectorY;
    _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Empty;
    for (int i = 0; i < 1000; i++)
    {
      x += vx;
      y += vy;
      quadX = ((int)Math.Round(x)) / 8;
      quadY = ((int)Math.Round(y)) / 8;
      if (quadX == _data.QuadrantX && quadY == _data.QuadrantY)
      {
        sectX = ((int)Math.Round(x)) % 8;
        sectY = ((int)Math.Round(y)) % 8;
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

  private bool InputDouble(IUserInterface UI, string prompt, out double value)
  {
    try
    {
      string NumStr = UI.InputString(prompt);
      value = Double.Parse(NumStr);
      return true;
    }
    catch
    {
      value = 0;
    }

    return false;
  }

  private bool InputInt(IUserInterface UI, string prompt, out int value)
  {
    try
    {
      string NumStr = UI.InputString(prompt);
      value = Int32.Parse(NumStr);
      return true;
    }
    catch
    {
      value = 0;
    }

    return false;
  }

  private void GenerateSector()
  {
    Quadrant quadrant = _data.Quadrants[_data.QuadrantY][_data.QuadrantX];
    bool starbase = quadrant.StarBase;
    int stars = quadrant.Stars;
    int klingons = quadrant.Klingons;
    _data.KlingonShips.Clear();
    for (int i = 0; i < 8; i++)
    {
      for (int j = 0; j < 8; j++)
      {
        _data.Sector[i][j] = SectorType.Empty;
      }
    }

    _data.Sector[_data.SectorY][_data.SectorX] = SectorType.Enterprise;
    while (starbase || stars > 0 || klingons > 0)
    {
      int i = _data.random.Next(8);
      int j = _data.random.Next(8);
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
          KlingonShip klingonShip = new KlingonShip();
          klingonShip.ShieldLevel = 300 + _data.random.Next(200);
          klingonShip.SectorY = i;
          klingonShip.SectorX = j;
          _data.KlingonShips.Add(klingonShip);
          klingons--;
        }
      }
    }
  }

  private bool IsDockingLocation(int i, int j)
  {
    for (int y = i - 1; y <= i + 1; y++)
    {
      for (int x = j - 1; x <= j + 1; x++)
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
    for (int y = i - 1; y <= i + 1; y++)
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
      Quadrant quadrant = _data.Quadrants[_data.QuadrantY][_data.QuadrantX];
      quadrant.Scanned = true;
      PrintSector(quadrant);
    }

    _uiMain.DisplayLine("");
  }

  private void PrintSector(Quadrant quadrant)
  {
    string condition = "GREEN";
    if (quadrant.Klingons > 0)
    {
      condition = "RED";
    }
    else if (_data.Energy < 300)
    {
      condition = "YELLOW";
    }

    _uiMain.DisplayLine(string.Format("-=--=--=--=--=--=--=--=-             Region: {0}", quadrant.Name));
    PrintSectorRow(0, string.Format("           Quadrant: [{0},{1}]", _data.QuadrantX + 1, _data.QuadrantY + 1));
    PrintSectorRow(1, string.Format("             Sector: [{0},{1}]", _data.SectorX + 1, _data.SectorY + 1));
    PrintSectorRow(2, string.Format("           Stardate: {0}", _data.StarDate));
    PrintSectorRow(3, string.Format("     Time remaining: {0}", _data.TimeRemaining));
    PrintSectorRow(4, string.Format("          Condition: {0}", condition));
    PrintSectorRow(5, string.Format("             Energy: {0}", _data.Energy));
    PrintSectorRow(6, string.Format("            Shields: {0}", _data.ShieldLevel));
    PrintSectorRow(7, string.Format("   Photon Torpedoes: {0}", _data.PhotonTorpedoes));
    _uiMain.DisplayLine(string.Format("-=--=--=--=--=--=--=--=-             Docked: {0}", _data.Docked));

    if (quadrant.Klingons > 0)
    {
      _uiMain.DisplayLine("");
      _uiMain.DisplayLine(string.Format("Condition RED: Klingon ship{0} detected.", (quadrant.Klingons == 1 ? "" : "s")));
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
    StringBuilder sb = new StringBuilder();
    for (int column = 0; column < 8; column++)
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
          Debug.Fail("Failed to handle: " + _data.Sector[row][column].ToString());
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
    _uiMain.DisplayLine(string.Format("Mission: Destroy {0} Klingon ships in {1} stardates with {2} starbases.",
      _data.Klingons, _data.TimeRemaining, _data.StarBases));
    _uiMain.DisplayLine("");
  }

  private void InitializeGame()
  {
    _data = new StarTrekData();

    List<string> names = new List<string>();
    foreach (string name in QuadrantNames)
    {
      names.Add(name);
    }

    for (int i = 0; i < 8; i++)
    {
      for (int j = 0; j < 8; j++)
      {
        int index = _data.random.Next(names.Count);
        Quadrant quadrant = new Quadrant();
        _data.Quadrants[i][j] = quadrant;
        quadrant.Name = names[index];
        quadrant.Stars = 1 + _data.random.Next(8);
        names.RemoveAt(index);
      }
    }

    int klingonCount = _data.Klingons;
    int starbaseCount = _data.StarBases;
    while (klingonCount > 0 || starbaseCount > 0)
    {
      int i = _data.random.Next(8);
      int j = _data.random.Next(8);
      Quadrant quadrant = _data.Quadrants[i][j];
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

  private void PrintStrings(string[] strings)
  {
    foreach (string str in strings)
    {
      _uiMain.DisplayLine(str);
    }

    _uiMain.DisplayLine("");
  }
}
