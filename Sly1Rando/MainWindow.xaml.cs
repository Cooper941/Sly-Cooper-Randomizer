using Memory;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace Sly1Rando
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        Mem mem = new();

        bool OpenProc;
        bool GoodBase, ScanningBase;
        UInt64 EE_BaseAddress = 0;
        string BaseString;
        int bgProcessID;

        public static byte?[] CheckBytesEE { get; private set; } = new byte?[]
        { 0x01, 0x80, 0x1A, 0x3C, null, null, 0x59, 0xFF, 0x00, 0x68, 0x19, 0x40,
        0x01, 0x80, 0x1A, 0x3C, 0x7C, 0x00, 0x39, 0x33, 0x21, 0xD0, 0x59, 0x03, null,
        null, null, 0x8F, 0x01, 0x80, 0x19, 0x3C, 0x08, 0x00, 0x40, 0x03, null, null, 0x39, 0xDF };


        public bool ReadyForSceneInit = false;

        public bool IsRunning = false;
        public static int SEED;

        public bool Opts_RandomLevelOrder = true;
        public bool Opts_Logging = false;

        public class JT
        {
            public const ulong EntityStruct = 0x262E10;
            public const ulong Gravity = 0x358;
            public const ulong JumpHeight = 0x6a8;
        }

        #region Bindings and shit
        //ui bindings
        string _MainReport = "Hooking...";
        public string MainReporter { get { return _MainReport; } set { _MainReport = value; NotifyPropertyChanged("MainReporter"); } }

        string _RunButtonText = "...";
        public string RunButtonText { get { return _RunButtonText; } set { _RunButtonText = value; NotifyPropertyChanged("RunButtonText"); } }

        string _seedstring;
        public string SEEDString { get { return _seedstring; } set { _seedstring = value; NotifyPropertyChanged("SEEDString"); } }

        bool _ReadyToRun = false;
        public bool ReadyToRun { get { return _ReadyToRun; } set { _ReadyToRun = value; NotifyPropertyChanged("ReadyToRun"); } }

        bool _AllowSeedGeneration = false;
        public bool AllowSeedGeneration { get { return _AllowSeedGeneration; } set { _AllowSeedGeneration = value; NotifyPropertyChanged("AllowSeedGeneration"); } }

        string _ConsoleLog = "Initiating...\n";
        public string ConsoleLog { get { return _ConsoleLog; } set { _ConsoleLog = value; NotifyPropertyChanged("ConsoleLog"); } }
        public int logCount = 1;

        string _LoadingStatus = "";
        public string LoadingStatus { get { return _LoadingStatus; } set { _LoadingStatus = value; NotifyPropertyChanged("LoadingStatus"); } }
        bool IsLoading = false;

        #endregion


        public ulong MapAddress = 0x275F94;
        public int MapChangedTimes = 0;
        List<ulong> MapValues = new List<ulong>
        {


            0x247B74,//stealthy app
            0x247BA0,//prowling
            0x247BCC,//highclass heist
            0x247BF8,//in to the machine
            0x247C50,//firedown
            0x247C24,//cunning
            0x247C7C,//crab
            0x247CA8,//gunboat<3sex
            0x247CD4,//raleigh      BOSS

            0x247D00,//rocky start
            0x247D2C,//turf
            0x247D58,//casino
            0x247D84,//murray gamble    //bugged??
            0x247DB0,//at dog
            0x247DDC,//22tango
            0x247E08,//str8 to the top
            0x247E34,//back alley
            0x247E60,//mug dog      BOSS

            0x247E8C,//dread path
            0x247EB8,//sw dark center
            0x247EE4,//beast
            0x247F10,//grave
            0x247F3C,//fish
            0x247F68,//descent
            0x247F94,//ghastly
            0x247FC0,//cooking
            0x247FEC,//mzruby         BOSS

            0x248044,//stronghold
            0x248070,//temple
            0x24809C,//foe
            0x248018,//per ascent
            0x2480C8,//king
            0x2480F4,//rapidfireass
            0x248120,//carm duel
            0x24814C,//race
            0x248178,//pandaboss        BOSS

            0x2481A4,//haz path
            0x2481D0,//rubber
            0x2481FC,//rescue
            0x248228,//ben
            0x248254,//truce
            0x248280,//sinking peril
            0x2482AC//clockwerk   BOSS
        };
        public List<string> MapNamesWithID = new List<string>
        {
            "A Stealthy Approach",
            "Prowling The Grounds",
            "High Class Heist",
            "Into the Machine",
            "The Fire Down Below",
            "A Cunning Disguise",
            "Treasure in the Depths",
            "The Gunboat Graveyard",
            "The Eye of the Storm",
            "A Rocky Start",
            "Muggshot's Turf",
            "Boneyard Casino",
            "Murray's Big Gamble",
            "At the Dog Track",
            "Two to Tango",
            "Straight to the Top",
            "Back Alley Heist",
            "Last Call",
            "The Dread Swamp Path",
            "The Swamp's Dark Centre",
            "The Lair of the Beast",
            "A Grave Undertaking",
            "Piranha Lake",
            "Descent into Danger",
            "A Ghastly Voyage",
            "Down Home Cooking",
            "A Deadly Dance",
            "A Perilous Ascent",
            "Inside the Stronghold",
            "Flaming Temple of Flame",
            "The Unseen Foe",
            "The King of the Hill",
            "Rapid Fire Assault",
            "Duel by the Dragon",
            "A Desperate Race",
            "Flame Fu!",
            "A Hazardous Path",
            "Burning Rubber",
            "A Daring Rescue",
            "Bentley Comes Through",
            "A Temporary Truce",
            "Sinking Peril",
            "A Strange Reunion"
        };

        List<ulong> ScrambledLevelList;

        public static List<ulong> ShuffleList(List<ulong> inputList)
        {
            List<ulong> templist = new List<ulong>(inputList);
            for (int i = 0; i < templist.Count - 1; i++)
            {
                int j = RandomFuncs.GenerateRandomValue(i, templist.Count - 1, SEED + i);
                (templist[i], templist[j]) = (templist[j], templist[i]);
            }

            return templist;
        }

        
        public class Cheat
        {
            public string CheatName;                // for debug logs
            public ulong Address;                   // target address (this should never change)
            public string ValueType;                // value type of this cheat
            public List<string> RandomValueTable;   // list of integers the value at this address can be set to
            public int TimesUsed = 0;               // to offset the seed on this specific cheat so no same random value every time forever
            int LastRandomIndex;
            bool First = true;
            public Cheat(string _name, ulong _address, string _valueType, List<string> _valueTable)
            {
                CheatName = _name;
                Address = _address;
                ValueType = _valueType;
                RandomValueTable = _valueTable;
            }
            public string GetValueFromTable()
            {
                string result = "";

                int rand = LastRandomIndex;
                while (true)
                {
                    rand = RandomFuncs.GenerateRandomValue(0, RandomValueTable.Count - 1, SEED + TimesUsed);
                    if (First)
                    {
                        First = false;
                        break;
                    }
                    if (rand != LastRandomIndex)
                    {
                        LastRandomIndex = rand;
                        break;
                    }
                }
                result = RandomValueTable[rand];
                return result;
            }
        }

        public Cheat ExampleCheat = new Cheat
        (
            "Random Coin Count",
            0x0286208,
            "int",
            new List<string>()
            {
                "1","2","3",
                "4","5","6"
            }
        );

        public class RandomFuncs
        {
            public static int GenerateRandomValue(int min, int max, int seed)
            {
                Random r = new Random(seed);
                return r.Next(min, max + 1);  //+1 to make it maxinclusive
            }
            public static float GenerateRandomFloat(float min, float max, int seed)
            {
                Random r = new Random(seed);
                double res = (r.NextDouble() * (max - min) + min);
                return (float)res;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            this.DataContext = this;

            ThreadPool.QueueUserWorkItem(MainLoop_ThreadF);
            ThreadPool.QueueUserWorkItem(ThreadLoop_Loading);

        }

        public void StartRunning()
        {
            if (!IsSeedViable())
            {
                Log(string.Format("Invalid seed '{0}', use a number between {1} and {2} or generate a random one.", SEEDString, int.MinValue, int.MaxValue), true);
                //MainReporter = string.Format("Invalid seed, please use a number between {0} and {1}.", int.MinValue, int.MaxValue);
                return;
            }
            SEED = int.Parse(SEEDString);
            IsRunning = true;
            ReadyToRun = false;
            AllowSeedGeneration = false;
            RunButtonText = "Running";
            MainReporter = "Running";
            Log("Running", true);
            ScrambledLevelList = ShuffleList(MapValues);
            foreach (ulong u in ScrambledLevelList)
            {
                Debug.WriteLine(u.ToString("X"));
            }
            //all cheat loops here -v
            //ThreadPool.QueueUserWorkItem(CheatLoop_Example);
        }


        public bool GameLoading() // loading = 2
        {
            return mem.ReadInt((EE_BaseAddress + 0x269BA4).ToString("X")) == 2;
        }


        public int[] GetCurrentLevel()
        {
            ulong g_pgsCur = EE_BaseAddress + 0x2623C0;     // global game state data pointer
            ulong curWID = EE_BaseAddress + 0x19d8;         // gs pointer to current world id
            ulong curLID = EE_BaseAddress + 0x19dc;         // gs pointer to current level id
            string widScan = string.Format("{0},{1}", g_pgsCur.ToString("X"), curWID.ToString("X"));
            string lidScan = string.Format("{0},{1}", g_pgsCur.ToString("X"), curLID.ToString("X"));
            int curW = mem.ReadInt(widScan);
            int curL = mem.ReadInt(lidScan);
            return new int[2] { curW, curL };
        }
        public void GenerateRandomSeed()
        {
            SEED = new Random().Next(int.MinValue, int.MaxValue);
            SEEDString = SEED.ToString("0000000000");
            Log("Generated new seed: " + SEEDString, true);
        }
        public bool IsSeedViable()
        {
            if (int.TryParse(SEEDString, out int res)) return true;
            return false;
        }

        void MainLoop_ThreadF(object stateInfo)
        {
            Log("Welcome to the Sly 1 Randomizer!", true);
            Log("By Sly Cooper Modding Server", true);
            Log("\n", true);
            Thread.Sleep(1000 * 3);
            IsLoading = true;
            GenerateRandomSeed();
            string tempPhrog = GetProcessName();

            Log("Found PCSX2.", true);

            Thread.Sleep(500);

            Log("Hooking Memory.dll to PCSX2.", true);


            while (true)
            {

                int pID = mem.GetProcIdFromName(tempPhrog);       //  get process ID
                OpenProc = false;

                if (pID > 0)                                    //  is process running?
                {
                    OpenProc = mem.OpenProcess(pID);
                }
                if (OpenProc)
                {
                    bgProcessID = pID;
                    break;
                }

                Thread.Sleep(1000);
            }
            IsLoading = false;
            Log("Hooked to PCSX2.", true);
            Thread.Sleep(500);
            IsLoading = true;
            Log("Finding EE Base.", true);
            while (true)     //  finding the base (luminar's method)
            {
                if (!ScanningBase)
                {
                    CurrentProcessBaseFinder();
                    if (EE_BaseAddress > 0)
                    {
                        Console.WriteLine(EE_BaseAddress.ToString("X"));
                        break;
                    }
                }
                Thread.Sleep(50);
            }
            IsLoading = false;
            Log("EE Base found at: " + BaseString, true);
            Log("The randomizer is now ready to run.", true);
            MainReporter = "Ready to run";
            RunButtonText = "Start Randomizing";
            AllowSeedGeneration = true;
            ReadyToRun = true;

            while (!IsRunning)
            {
                Thread.Sleep(100);
            }
            OnSceneLoad();
            while (true)
            {
                //this runs continuously
                if (GameLoading())
                {
                    if(!ReadyForSceneInit)
                    {
                        OnLoadStart();
                    }
                    ReadyForSceneInit = true;
                }
                else if (ReadyForSceneInit)
                {
                    OnSceneLoad();
                }
                Thread.Sleep(100);
            }
        }

        void CheatLoop_Example(object stateInfo)    // example cheat func, does something every .5 seconds
        {
            Cheat cheat = ExampleCheat;
            while (true)
            {
                string writeAddr = (EE_BaseAddress + cheat.Address).ToString("X");
                mem.WriteMemory(writeAddr, cheat.ValueType, cheat.GetValueFromTable());
                cheat.TimesUsed++;
                Thread.Sleep(500);
            }
        }

        public void OnLoadStart()
        {
            /*foreach(long l in FrozenTimescaleBytes)
            {
                mem.UnfreezeValue("l");
            }*/
        }

        public void OnSceneLoad()      // called upon entering a new scene
        {
            ReadyForSceneInit = false;
            int[] curIDs = GetCurrentLevel();
            Log(string.Format("Loaded to {0} ({1},{2}).", GetMapName(curIDs[0], curIDs[1]), curIDs[0], curIDs[1]));

            #region Random Gravity
            if (AllowGravityChange())
            {
                if (RandomFuncs.GenerateRandomFloat(0, 1, SEED + curIDs[0] - curIDs[1]) < .5f)
                {
                    string gravmemwrite = string.Format("{0},{1}", (EE_BaseAddress + JT.EntityStruct).ToString("X"), (EE_BaseAddress + JT.Gravity).ToString("X"));
                    string gravityvalue = RandomFuncs.GenerateRandomFloat(-2300f, -300f, SEED + curIDs[0] - curIDs[1]).ToString();

                    Log(string.Format("Gravity ({0},{1}) = {2}", curIDs[0], curIDs[1], gravityvalue));
                    mem.WriteMemory(gravmemwrite, "float", gravityvalue);
                }
                else
                {
                    Log(string.Format("Gravity ({0},{1}) = {2}", curIDs[0], curIDs[1], "default gravity"));
                }
            }
            else
            {
                Log(string.Format("Gravity at {0},{1} isn't altered due to possible softlocks.", curIDs[0], curIDs[1]));
            }
            #endregion


            SetRandomMap();
            //SetRandomTimescales();

            if (curIDs[0] == 3 && curIDs[1] == 8) MzRubyRandomButtons();


        }

        void FindEEBase()
        {

        }

        public string GetProcessName()
        {
            Log("Finding PCSX2 process.");
            while (true)
            {

                Process[] processes = Process.GetProcesses();
                foreach (Process p in processes)
                {
                    if (p.ProcessName.StartsWith("pcsx2"))
                    {
                        return p.ProcessName;
                    }
                }
                Thread.Sleep(1000);
            }
        }
        public void CurrentProcessBaseFinder()      //luminar's method
        {
            if (!GoodBase)
            {
                ScanningBase = true;

                List<byte?> checkBytes = new List<byte?>();

                for (int i = 0; i < CheckBytesEE.Length; i++)
                {
                    checkBytes.Add(CheckBytesEE[i]);
                }

                UInt64 start = 0;

                while (start < 0x800000000000)
                {
                    bool good = true;

                    for (int i = 0; i < checkBytes.Count; i += 4)
                    {
                        if (checkBytes[i] == null) continue;

                        byte[] helpByteArr = new byte[]
                        {
                            (byte)checkBytes[i],
                            (byte)checkBytes[i+1],
                            (byte)checkBytes[i+2],
                            (byte)checkBytes[i+3]
                        };

                        int val = BitConverter.ToInt32(helpByteArr, 0);

                        if (mem.ReadUIntPtr((UIntPtr)start + i) != val)
                        {
                            good = false;
                            break;
                        }
                    }

                    if (!OpenProc)      //  fallback if scan successful but process is lost
                    {
                        ScanningBase = false;
                        GoodBase = false;
                        return;
                    }

                    if (!good)
                    {
                        start += 0x10000000;
                        continue;
                    }

                    if (start == EE_BaseAddress)
                    {
                        ScanningBase = false;
                        GoodBase = true;
                        return;
                    }

                    EE_BaseAddress = (UInt64)start;
                    BaseString = "0x" + EE_BaseAddress.ToString("X");
                    Debug.WriteLine(BaseString);

                    ScanningBase = false;
                    GoodBase = true;
                    return;
                }
                Thread.Sleep(1000);
                ScanningBase = false;
                GoodBase = false;
                return;
            }
        }

        public bool AllowGravityChange() //exceptions for levels that break with altered gravity
        {
            int[] curlvl = GetCurrentLevel();
            string string_curLvl = string.Format("{0},{1}", curlvl[0], curlvl[1]);
            List<string> exceptions = new List<string>
            {

                //raleigh boss (water softlock)
                "1,8",

                //mur big gamble (void)
                "2,3",

                //mzruby boss (softlock if sly fly too far at intro)
                "3,8",
                
                //temp truce (if sly ledge grabs something, stuck forever)
                "5,5"

            };
            foreach (string s in exceptions)
            {
                if (string_curLvl == s) return false;
            }
            return true;
        }
        public void MzRubyRandomButtons()
        {
            Log("Generating Mz. Ruby fight...");
            List<ulong> MzRubyButtons = new List<ulong>
            {
                0x177CEDC,
                0x177CEEC,
                0x177CEFC,
                0x177CF6C,
                0x177CF7C,
                0x177CF8C,
                0x177CFFC,
                0x177D00C,
                0x177D01C,
                0x177D08C,
                0x177D09C,
                0x177D0AC,
                0x177D11C,
                0x177D12C,
                0x177D13C,
                0x177D1AC,
                0x177D1BC,
                0x177D1CC,
                0x177D23C,
                0x177D24C,
                0x177D25C,
                0x177D2DC,
                0x177D34C,
                0x177D3BC,
                0x177C28C,
                0x177C29C,
                0x177C33C,
                0x177C34C,
                0x177C3FC,
                0x177C40C,
                0x177C41C,
                0x177C42C,
                0x177C43C,
                0x177C44C,
                0x177C4FC,
                0x177C50C,
                0x177C51C,
                0x177C52C,
                0x177C53C,
                0x177C54C,
                0x177C5FC,
                0x177C60C,
                0x177C61C,
                0x177C68C,
                0x177C72C,
                0x177C73C,
                0x177C74C,
                0x177C7BC,
                0x177C86C,
                0x177C87C,
                0x177C88C,
                0x177C90C,
                0x177C9BC,
                0x177C9CC,
                0x177C9DC,
                0x177CA5C,
                0x177B63C,
                0x177B64C,
                0x177B65C,
                0x177B6CC,
                0x177B6DC,
                0x177B6EC,
                0x177B76C,
                0x177B77C,
                0x177B78C,
                0x177B79C,
                0x177B7AC,
                0x177B7BC,
                0x177B7CC,
                0x177B83C,
                0x177B84C,
                0x177B85C,
                0x177B86C,
                0x177B87C,
                0x177B88C,
                0x177B89C,
                0x177B91C,
                0x177B92C,
                0x177B93C,
                0x177B9AC,
                0x177B9BC,
                0x177B9CC,
                0x177BA3C,
                0x177BA4C,
                0x177BA5C,
                0x177BACC,
                0x177BADC,
                0x177BAEC,
                0x177BB5C,
                0x177BB6C,
                0x177BB7C,
                0x177BBEC,
                0x177BBFC,
                0x177BC0C,
                0x177BC7C,
                0x177BC8C,
                0x177BC9C,
                0x177BD0C,
                0x177BD1C

            };
            List<ulong> MzRubyTimes = new List<ulong>
            {
                0x177CEE8,
                0x177CEF8,
                0x177CF78,
                0x177CF88,
                0x177D008,
                0x177D018,
                0x177D098,
                0x177D0A8,
                0x177D128,
                0x177D138,
                0x177D1B8,
                0x177D1C8,
                0x177D248,
                0x177D258,
                0x177C298,
                0x177C2A8,
                0x177C348,
                0x177C358,
                0x177C408,
                0x177C418,
                0x177C428,
                0x177C448,
                0x177C458,
                0x177C508,
                0x177C518,
                0x177C528,
                0x177C548,
                0x177C558,
                0x177C608,
                0x177C618,
                0x177C698,
                0x177C738,
                0x177C748,
                0x177C7C8,
                0x177C878,
                0x177C888,
                0x177C918,
                0x177C9C8,
                0x177C9D8,
                0x177CA68,
                0x177B648,
                0x177B658,
                0x177B6D8,
                0x177B6E8,
                0x177B778,
                0x177B788,
                0x177B798,
                0x177B7B8,
                0x177B7C8,
                0x177B848,
                0x177B858,
                0x177B868,
                0x177B888,
                0x177B898,
                0x177B928,
                0x177B938,
                0x177B9B8,
                0x177B9C8,
                0x177BA48,
                0x177BA58,
                0x177BAD8,
                0x177BAE8,
                0x177BB68,
                0x177BB78,
                0x177BBF8,
                0x177BC08,
                0x177BC88,
                0x177BC98,
                0x177BD18,
                0x177BD28
            };
            int id = 0;
            foreach (ulong addr in MzRubyButtons)
            {
                string _addr = (EE_BaseAddress + addr).ToString("X");
                string _val = RandomFuncs.GenerateRandomValue(0, 2, SEED + id).ToString();
                mem.WriteMemory(_addr, "int", _val);
                Log(string.Format("Button - ({1}) <- ({0})", _val + string.Format(" ({0})", GetButtonId(_val)), _addr));
                id++;
            }
            id = 0;
            foreach (ulong addr in MzRubyTimes)
            {
                string _addr = (EE_BaseAddress + addr).ToString("X");
                bool doTime = (RandomFuncs.GenerateRandomFloat(0f, 1f, SEED + id) < .5f);   //50% to not change timing at all

                if (doTime)
                {
                    bool do05 = (RandomFuncs.GenerateRandomFloat(0f, 1f, SEED + id + id) < .8f);//80% to use .5 offs, else .75
                    float timeoffs = do05 ? 0.5f : 0.75f;

                    float valToUse = mem.ReadFloat(_addr) - timeoffs;
                    mem.WriteMemory(_addr, "float", valToUse.ToString());
                    Log(string.Format("Time - ({1}) <- ({0}f)", valToUse, _addr));
                }
                id++;
            }
        }
        string GetButtonId(string inp)
        {
            return inp == "0" ? "Square" : inp == "1" ? "Triangle" : inp == "2" ? "Circle" : "?";
        }
        string[,] MapNames = new string[,]//world,level
        {
            {"n/a", "n/a", "n/a", "Splash", "Paris", "Hideout", "n/a", "n/a", "n/a"},//0
            {"A Stealthy Approach", "Prowling The Grounds", "High Class Heist", "Into The Machine", "A Cunning Disguise", "The Fire Down Below", "Treasure in the Depths", "The Gunboat Graveyard", "The Eye of the Storm (Raleigh)"},//1
            {"A Rocky Start", "Muggshot's Turf", "Boneyard Casino", "Murray's Big Gamble", "At the Dog Track", "Two to Tango", "Straight to the Top", "Back Alley Heist", "Last Call (Muggshot)"},//2
            {"The Dread Swamp Path", "The Swamp's Dark Centre", "The Lair of the Beast", "A Grave Undertaking", "Piranha Lake", "Descent into Danger", "A Ghastly Voyage", "Down Home Cooking", "A Deadly Dance (Mz. Ruby)"},//3
            {"A Perilous Ascent", "Inside the Stronghold", "Flaming Temple of Flame", "The Unseen Foe", "The King of the Hill", "Rapid Fire Assault", "Duel by the Dragon", "A Desperate Race", "Flame Fu! (Panda King)"},//4
            {"A Hazardous Path", "n/a", "Burning Rubber", "A Daring Rescue", "Bentley Comes Through", "A Temporary Truce", "Sinking Peril", "n/a", "A Strange Reunion (Clockwerk)"}//5

        };
        string GetMapName(int w, int l)
        {
            try
            {
                return MapNames[w, l];
            }
            catch
            {
                return "???";
            }

        }

        public void SetRandomMap()
        {
            if (!Opts_RandomLevelOrder) return;
            ulong nextmapval = ScrambledLevelList[MapChangedTimes];
            string nextMap = nextmapval.ToString();
            string nextmapname = "n/a";


            /*int listid = 0;
            foreach (ulong mapval in MapValues)
            {
                if (mapval==nextmapval)
                {
                    nextmapname = MapNamesWithID[listid];
                    break;
                }
                listid++;
            }*/
            Thread.Sleep(1);
            for (int i = 0; i < MapValues.Count; i++)
            {
                if (MapValues[i] == nextmapval)
                {
                    Debug.WriteLine("MAPVALUE i = " + MapValues[i].ToString("X") + " NEXTMAPVAL = " + nextmapval.ToString("X"));
                    nextmapname = MapNamesWithID[i];
                    break;
                }
            }
            Thread.Sleep(1);

            Log(string.Format("Next map will be {0} ({1}).", nextmapname, nextmapval.ToString("X8")));
            
            mem.FreezeValue((EE_BaseAddress + MapAddress).ToString("X"), "int", nextMap);
            MapChangedTimes++;
            if (MapChangedTimes >= ScrambledLevelList.Count)
            {
                MapChangedTimes = 0;
            }
        }

        string AnimationBytes = "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 B8 97 21 00 00 00 00";
        List<long> FrozenTimescaleBytes;
        public async void SetRandomTimescales()
        {
            FrozenTimescaleBytes = new();
            FrozenTimescaleBytes.Clear();
            int[] wl = GetCurrentLevel();
            IEnumerable<long> AoBScanResults = await mem.AoBScan((long)EE_BaseAddress, (long)(EE_BaseAddress+0x02000000), AnimationBytes, true, true);

            Log(string.Format("Animation AOB Scan returned {0} results.", AoBScanResults.Count()));
            int offs = 0;
            foreach (long res in AoBScanResults)
            {
                if (mem.ReadFloat((res+0x38).ToString("X")) == 1f)
                {
                    FrozenTimescaleBytes.Add(res);
                    float toWrite = RandomFuncs.GenerateRandomFloat(.8f, 2.5f, SEED + offs * wl[0] + wl[1]);
                    Log(string.Format("Animation tiemscale at {0} set to {1}.", (res+0x38).ToString("X"), toWrite));
                    mem.FreezeValue((res + 0x38).ToString("X"), "float", toWrite.ToString());
                    mem.FreezeValue((res + 0x38 + 0x4).ToString("X"), "float", toWrite.ToString());
                    offs++;
                }
            }
        }
        string[] LoadingString = new string[]
        {
            ".","..","...","....","....."
        };
        public void ThreadLoop_Loading(object stateInfo)
        {
            
            int stringid = 0;
            while (true)
            {
                while(!IsLoading)
                {
                    LoadingStatus = "";
                    Thread.Sleep(1000);
                }
                LoadingStatus = LoadingString[stringid];
                stringid++;
                if (stringid >= LoadingString.Length) stringid = 0;
                Thread.Sleep(250);
            }
        }
        public void Log(string str, bool forced = false)
        {
            if (!forced && !Opts_Logging)
            {
                return;
            }
            logCount++;
            /*if(logCount>500)
            {
                //ConsoleLog = "";
                //logCount = 1;
            }*/
            ConsoleLog += str + "\n";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SeedButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateRandomSeed();
        }

        private void LOGBOX_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LOGBOX.Text = ConsoleLog;
            LOGBOX.ScrollToEnd();
        }

        private void TOGGLE_RandLevels_Click(object sender, RoutedEventArgs e)
        {
            Opts_RandomLevelOrder = !Opts_RandomLevelOrder;
        }

        private void TOGGLE_Logs_Click(object sender, RoutedEventArgs e)
        {
            Opts_Logging = !Opts_Logging;
        }

        private void Runbutton_Click(object sender, RoutedEventArgs e)
        {
            StartRunning();
        }


    }
}
