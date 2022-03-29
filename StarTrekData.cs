namespace Trek.Net;

public class StarTrekData
{
  #region Public member variables

  public Random random;

  public int StarDate;
  public int TimeRemaining;
  public int Energy;
  public int Klingons;
  public int StarBases;
  public int QuadrantX, QuadrantY;
  public int SectorX, SectorY;
  public int ShieldLevel;

  public int NavigationDamage;
  public int ShortRangeScanDamage;
  public int LongRangeScanDamage;
  public int ShieldControlDamage;
  public int ComputerDamage;
  public int PhotonDamage;
  public int PhaserDamage;

  public int PhotonTorpedoes;
  public bool Docked;
  public bool Destroyed;
  public bool Resigned;
  public int StarBaseX, StarBaseY;

  public Quadrant[][] Quadrants = new Quadrant[][]
  {
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
    new Quadrant[8],
  };

  public SectorType[][] Sector = new SectorType[][]
  {
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
    new SectorType[8],
  };

  public List<KlingonShip> KlingonShips = new List<KlingonShip>();

  #endregion

  public StarTrekData()
  {
    random = new Random();

    QuadrantX = random.Next(8);
    QuadrantY = random.Next(8);
    SectorX = random.Next(8);
    SectorY = random.Next(8);
    StarDate = random.Next(50) + 2250;
    TimeRemaining = 40 + random.Next(10);
    Klingons = 15 + random.Next(6);
    StarBases = 2 + random.Next(3);
    Destroyed = false;

    ResetSupplies();
    ResetDamage();

    ShieldLevel = 0;
    Docked = false;
    Resigned = false;
  }

  public void ResetDamage()
  {
    NavigationDamage = 0;
    ShortRangeScanDamage = 0;
    LongRangeScanDamage = 0;
    ShieldControlDamage = 0;
    ComputerDamage = 0;
    PhotonDamage = 0;
    PhaserDamage = 0;
  }

  public void ResetSupplies()
  {
    Energy = 3000;
    PhotonTorpedoes = 10;
  }
}
