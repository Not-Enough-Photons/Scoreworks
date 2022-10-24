using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace NEP.ScoreLab.Data
{
    public static class DataManager
    {
        public static class Bundle
        {
            public static List<AssetBundle> Bundles { get; private set; }

            public static void Init()
            {
                Bundles = new List<AssetBundle>();

                InitializeDirectories();
                LoadBundles(Path_CustomUIs);
            }

            public static void LoadBundles(string path)
            {
                string[] files = LoadAllFiles(path);

                foreach (var file in files)
                {
                    if (!file.EndsWith(".hud"))
                    {
                        return;
                    }

                    AssetBundle bundle = AssetBundle.LoadFromFile(file);
                    Bundles.Add(bundle);
                }
            }
        }

        public static class PackedValues
        {
            public static Dictionary<string, PackedValue> ValueTable { get; private set; }
            public static JSONScore[] Scores { get; private set; }
            public static JSONMult[] Multipliers { get; private set; }

            static string[] _scoreFiles;
            static string[] _multiplierFiles;

            public static void Init()
            {
                Scores = GetScores();
                Multipliers = GetMultipliers();
                GetValues();
            }

            public static PackedValue Get(string eventType)
            {
                return ValueTable[eventType];
            }

            private static JSONScore[] GetScores()
            {
                _scoreFiles = LoadAllFiles(Path_ScoreData, ".json");
                List<JSONScore> scores = new List<JSONScore>();

                foreach (var file in _scoreFiles)
                {
                    var data = ReadScoreData(file);
                    scores.Add(data);
                }

                return scores.ToArray();
            }

            private static JSONMult[] GetMultipliers()
            {
                _multiplierFiles = LoadAllFiles(Path_MultiplierData, ".json");
                List<JSONMult> multipliers = new List<JSONMult>();

                foreach (var file in _multiplierFiles)
                {
                    var data = ReadMultiplierData(file);
                    multipliers.Add(data);
                }

                return multipliers.ToArray();
            }

            private static void GetValues()
            {
                ValueTable = new Dictionary<string, PackedValue>();

                foreach (var score in Scores)
                {
                    var data = new PackedScore(score.EventType, score.Name, score.Score, score.DecayTime);
                    ValueTable.Add(score.EventType, data);
                }

                foreach (var multiplier in Multipliers)
                {
                    var data = new PackedMultiplier(multiplier.EventType, multiplier.DecayTime, multiplier.Name, multiplier.Multiplier, multiplier.Condition);
                    ValueTable.Add(multiplier.EventType, data);
                }
            }

            private static JSONScore ReadScoreData(string file)
            {
                var data = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<JSONScore>(data);
            }

            private static JSONMult ReadMultiplierData(string file)
            {
                var data = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<JSONMult>(data);
            }
        }

        public static class HighScore
        {
            public static Dictionary<string, int> BestTable;

            public static void Init()
            {
                BestTable = new Dictionary<string, int>();
                BestTable = ReadFromFile();
            }

            public static void WriteBest(PackedHighScore highScore)
            {
                string sceneName = highScore.Name;
                int bestScore = highScore.bestScore;

                BestTable.Add(sceneName, bestScore);
            }

            public static Dictionary<string, int> ReadFromFile()
            {
                string directory = File_HighScores;

                if (!File.Exists(Path_HighScoreData))
                {
                    Debug.LogWarning("High score file doesn't exist! Creating one.");
                    File.Create(Path_HighScoreData);
                    return null;
                }

                return JsonConvert.DeserializeObject(directory) as Dictionary<string, int>;
            }

            public static void WriteToFile()
            {
                string directory = File_HighScores;

                if (!File.Exists(directory))
                {
                    Debug.LogWarning("High score file doesn't exist! Creating one.");
                    File.Create(directory);
                    return;
                }

                var data = JsonConvert.SerializeObject(BestTable);
                File.WriteAllText(directory, data);
            }
        }

        public static class UI
        {
            public static void Init()
            {
                LoadedUIObjects = new List<GameObject>();
                UINames = new List<string>();

                LoadCustomUIs(Bundle.Bundles);
                LoadUINames();

                //SpawnDefaultUI();
            }

            public static List<GameObject> LoadedUIObjects { get; private set; }
            public static List<string> UINames { get; private set; }

            private static readonly string MainUIName = "Coda";
            private static readonly string Prefix_Hud = "[SLHUD] - ";

            public static GameObject GetObjectFromList(GameObject[] list, string query)
            {
                foreach (var obj in list)
                {
                    if (obj.name == query)
                    {
                        return obj;
                    }
                }

                return null;
            }

            public static void LoadCustomUIs(List<AssetBundle> bundles)
            {
                if (bundles == null)
                {
                    return;
                }

                foreach (var bundle in bundles)
                {
                    var loadedObjects = bundle.LoadAllAssets();

                    foreach (var bundleObject in loadedObjects)
                    {
                        if (bundleObject.TryCast<GameObject>())
                        {
                            var go = bundleObject.Cast<GameObject>();
                            go.hideFlags = HideFlags.DontUnloadUnusedAsset;
                            LoadedUIObjects.Add(go);
                        }
                    }
                }
            }

            public static void LoadUINames()
            {
                foreach (var uiObject in LoadedUIObjects)
                {
                    if (uiObject.name.StartsWith("[SLHUD]"))
                    {
                        UINames.Add(uiObject.name.Substring(Prefix_Hud.Length));
                    }
                }
            }

            public static void SpawnDefaultUI()
            {
                SpawnUI(MainUIName);
            }

            public static void SpawnUI(string name)
            {
                foreach (var obj in LoadedUIObjects)
                {
                    if (GetHUDName(obj) == name)
                    {
                        SpawnUI(obj);
                    }
                }
            }

            public static void SpawnUI(GameObject uiObject)
            {
                GameObject createdUI = GameObject.Instantiate(uiObject);
                createdUI.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            private static string GetHUDName(GameObject obj)
            {
                return obj.name.Substring(Prefix_Hud.Length);
            }
        }

        static readonly string Path_UserData = MelonLoader.MelonUtils.UserDataDirectory + "/";
        static readonly string Path_Developer = Path_UserData + "Not Enough Photons/";
        static readonly string Path_Mod = Path_Developer + "ScoreLab/";
        static readonly string Path_CustomUIs = Path_Mod + "Custom UIs/";

        static readonly string Path_ScoreData = Path_Mod + "Data/Score/";
        static readonly string Path_MultiplierData = Path_Mod + "Data/Multiplier/";
        static readonly string Path_HighScoreData = Path_Mod + "Data/High Score/";

        static readonly string File_HighScores = Path_HighScoreData + "high_score_table.json";
        static readonly string File_HUDSettings = Path_Mod + "sl_hud_settings.json";
        static readonly string File_CurrentHUD = Path_Mod + "sl_current_hud.txt";

        public static void Init()
        {
            InitializeDirectories();

            Bundle.Init();
            UI.Init();
            PackedValues.Init();
            //HighScore.Init();
        }

        public static string[] LoadAllFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public static string[] LoadAllFiles(string path, string extensionFilter)
        {
            string[] files = LoadAllFiles(path);
            List<string> filteredFiles = new List<string>();

            foreach (string file in files)
            {
                if (file.EndsWith(extensionFilter))
                {
                    filteredFiles.Add(file);
                }
            }

            return filteredFiles.ToArray();
        }

        private static void InitializeDirectories()
        {
            Directory.CreateDirectory(Path_Mod);
            Directory.CreateDirectory(Path_CustomUIs);

            Directory.CreateDirectory(Path_ScoreData);
            Directory.CreateDirectory(Path_MultiplierData);
        }
    }
}

