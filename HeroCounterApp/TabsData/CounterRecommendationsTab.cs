using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace HeroCounterApp
{
    public class CounterRecommendationsTab
    {
        private readonly Dictionary<int, List<string>> counterImagePaths = new Dictionary<int, List<string>>();
        private readonly ComboBox[] enemyComboBoxes;
        private readonly ListBox[] counterListBoxes;

        public Dictionary<string, ObservableCollection<Champion>> Counters { get; set; } = new Dictionary<string, ObservableCollection<Champion>>
        {
            { "EXPCounters", new ObservableCollection<Champion>() },
            { "MidCounters", new ObservableCollection<Champion>() },
            { "GoldCounters", new ObservableCollection<Champion>() },
            { "JungleCounters", new ObservableCollection<Champion>() },
            { "RoamCounters", new ObservableCollection<Champion>() },
            { "SPEEXPCounters", new ObservableCollection<Champion>() },
            { "SPEMidCounters", new ObservableCollection<Champion>() },
            { "SPEGoldCounters", new ObservableCollection<Champion>() },
            { "SPEJungleCounters", new ObservableCollection<Champion>() },
            { "SPERoamCounters", new ObservableCollection<Champion>() },
            { "NOREXP", new ObservableCollection<Champion>() },
            { "NORMid", new ObservableCollection<Champion>() },
            { "NORGold", new ObservableCollection<Champion>() },
            { "NORJungle", new ObservableCollection<Champion>() },
            { "NORRoam", new ObservableCollection<Champion>() }
        };

        private static readonly Dictionary<string, string> LaneMappings = new Dictionary<string, string>
        {
            { "EXP", "EXPCounters" },
            { "Gold", "GoldCounters" },
            { "Mid", "MidCounters" },
            { "Roam", "RoamCounters" },
            { "Jungle", "JungleCounters" },
            { "SPEEXP", "SPEEXPCounters" },
            { "SPEGold", "SPEGoldCounters" },
            { "SPEMid", "SPEMidCounters" },
            { "SPERoam", "SPERoamCounters" },
            { "SPEJungle", "SPEJungleCounters" },
            { "NOREXP", "NOREXP" },
            { "NORGold", "NORGold" },
            { "NORMid", "NORMid" },
            { "NORRoam", "NORRoam" },
            { "NORJungle", "NORJungle" }
        };

        public CounterRecommendationsTab(ComboBox[] enemyComboBoxes, ListBox[] counterListBoxes)
        {
            this.enemyComboBoxes = enemyComboBoxes;
            this.counterListBoxes = counterListBoxes;
        }

        public void Initialize()
        {
            foreach (var comboBox in enemyComboBoxes)
            {
                comboBox.SelectionChanged += EnemyChampionComboBox_SelectionChanged;
            }
        }

        private ObservableCollection<Champion> GetLaneCollection(string lane, bool isSPE = false, bool isNOR = false)
        {
            string key = (isSPE ? $"SPE{lane}" : lane);
            key = (isNOR ? $"NOR{lane}" : key);

            if (LaneMappings.TryGetValue(key, out var counterKey))
            {
                return Counters[counterKey];
            }

            throw new InvalidOperationException($"Unexpected lane: {lane}");
        }

        private void EnemyChampionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearCounterLists();
            UpdateCounterRecommendations();
        }

        private void ClearCounterLists()
        {
            foreach (var listBox in counterListBoxes)
            {
                listBox.ItemsSource = null;
            }
            foreach (var key in Counters.Keys.ToList())
            {
                Counters[key].Clear();
            }
            foreach (var key in counterImagePaths.Keys.ToList())
            {
                counterImagePaths[key].Clear();
            }
        }

        private void UpdateCounterRecommendations()
        {
            var selectedEnemyHeroes = enemyComboBoxes
                .Where(comboBox => comboBox.SelectedValue != null)
                .Select(comboBox => (int)comboBox.SelectedValue)
                .ToList();
            if(selectedEnemyHeroes.Count!=0)
            GetLaneCounterRecommendations(selectedEnemyHeroes);
        }

        private void GetLaneCounterRecommendations(List<int> enemyHeroIds)
        {
            var championCounts = new Dictionary<int, int>();
            var allCountersID = new HashSet<int>();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var enemyChampionData = FetchChampionData(enemyHeroIds, connection);

                    foreach (var (championId, counters, lane, imagepath) in enemyChampionData)
                    {
                        if (!string.IsNullOrEmpty(counters))
                        {
                            var counterIds = counters.Split(',').Select(int.Parse).ToHashSet();
                            allCountersID.UnionWith(counterIds);

                            foreach (var counterId in counterIds)
                            {
                                if (!counterImagePaths.ContainsKey(counterId))
                                {
                                    counterImagePaths[counterId] = new List<string>();
                                }
                                if (!counterImagePaths[counterId].Contains(imagepath))
                                {
                                    counterImagePaths[counterId].Add(imagepath);
                                }
                                championCounts[counterId] = championCounts.ContainsKey(counterId) ? championCounts[counterId] + 1 : 1;
                            }

                            AddToLaneLists(enemyHeroIds, counterIds, lane, championCounts, connection, true);
                        }
                    }

                    if (allCountersID.Count > 0)
                    {
                        AddToLaneLists(enemyHeroIds, allCountersID, null, championCounts, connection, false);
                    }

                    GetNonRecommendedChampions(enemyHeroIds, connection);
                    SortChampionListsByOccurrences();
                    BindChampionListsToUI();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading counters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<(int ChampionId, string Counters, string Lane, string ImagePath)> FetchChampionData(List<int> enemyHeroIds, SQLiteConnection connection)
        {
            var data = new List<(int, string, string, string)>();

            foreach (var enemyId in enemyHeroIds)
            {
                string query = "SELECT ChampionID, Counters, Lanes, ImagePath FROM Champion WHERE ChampionID = @ChampionID";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChampionID", enemyId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int championId = reader.GetInt32(reader.GetOrdinal("ChampionID"));
                            string counters = reader.IsDBNull(reader.GetOrdinal("Counters")) ? "" : reader.GetString(reader.GetOrdinal("Counters"));
                            string lane = reader.IsDBNull(reader.GetOrdinal("Lanes")) ? "" : reader.GetString(reader.GetOrdinal("Lanes"));
                            string imagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"));
                            data.Add((championId, counters, lane, imagePath));
                        }
                    }
                }
            }

            return data;
        }

        private void AddToLaneLists(List<int> enemyHeroIds, IEnumerable<int> championIds, string lane, Dictionary<int, int> championCounts, SQLiteConnection connection, bool isSpecific)
        {
            var inClause = string.Join(",", championIds.Select((_, index) => $"@CounterId{index}"));
            var command = new SQLiteCommand($"SELECT * FROM Champion WHERE ChampionID IN ({inClause})", connection);

            for (int i = 0; i < championIds.Count(); i++)
            {
                command.Parameters.AddWithValue($"@CounterId{i}", championIds.ElementAt(i));
            }

            var addedChampions = new HashSet<int>();

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var champion = new Champion
                    {
                        ChampionID = reader.GetInt32(reader.GetOrdinal("ChampionID")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Lanes = reader.GetString(reader.GetOrdinal("Lanes")),
                        Counters = reader.GetString(reader.GetOrdinal("Counters")),
                        ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"))
                    };

                    if (enemyHeroIds.Contains(champion.ChampionID)) continue;

                    if (!string.IsNullOrEmpty(champion.Counters))
                    {
                        var countChampionCounters = champion.Counters.Split(',').Select(int.Parse);
                        if (countChampionCounters.Any(counterId => enemyHeroIds.Contains(counterId)))
                        {
                            continue;
                        }
                    }

                    if (!championCounts.TryGetValue(champion.ChampionID, out var occurrences)) continue;

                    if (isSpecific && lane != null)
                    {
                        var specificLanes = lane.Split(',').Select(l => l.Trim()).ToList();

                        foreach (var specificLane in specificLanes)
                        {
                            if (Regex.IsMatch(champion.Lanes, $@"\b{Regex.Escape(specificLane)}\b") && !addedChampions.Contains(champion.ChampionID))
                            {
                                SPEAddChampionToLaneList(champion, specificLane, occurrences);
                                addedChampions.Add(champion.ChampionID);
                            }
                        }
                    }
                    else
                    {
                        var lanesToMatch = Regex.Matches(champion.Lanes, @"\b(EXP|Gold|Mid|Roam|Jungle)\b");
                        foreach (Match match in lanesToMatch)
                        {
                            if (!addedChampions.Contains(champion.ChampionID))
                            {
                                AddChampionToLaneList(champion, match.Value, occurrences);
                                addedChampions.Add(champion.ChampionID);
                            }
                        }
                    }
                }
            }
        }

        private void GetNonRecommendedChampions(List<int> enemyHeroIds, SQLiteConnection connection)
        {
            var likeConditions = string.Join(" OR ", enemyHeroIds.Select((_, index) => $"CONCAT(',', Counters, ',') LIKE @EnemyId{index}"));
            var commandText = $"SELECT * FROM Champion WHERE {likeConditions}";
            var addedChampions = new HashSet<int>();

            using (var command = new SQLiteCommand(commandText, connection))
            {
                for (int i = 0; i < enemyHeroIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@EnemyId{i}", $"%,{enemyHeroIds[i]},%");
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var champion = new Champion
                        {
                            ChampionID = reader.GetInt32(reader.GetOrdinal("ChampionID")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Lanes = reader.GetString(reader.GetOrdinal("Lanes")),
                            Counters = reader.GetString(reader.GetOrdinal("Counters")),
                            ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"))
                        };

                        var lanesToMatch = Regex.Matches(champion.Lanes, @"\b(EXP|Gold|Mid|Roam|Jungle)\b");
                        foreach (Match match in lanesToMatch)
                        {
                            if (!addedChampions.Contains(champion.ChampionID)&& !enemyHeroIds.Contains(champion.ChampionID))
                            {
                                addedChampions.Add(champion.ChampionID);
                                NORAddChampionToLaneList(champion, match.Value);
                            }
                        }
                    }
                }
            }
        }

        private void SPEAddChampionToLaneList(Champion champion, string lane, int occurrences)
        {
            champion.Name += $" (Discovered {occurrences} times)";
            AddChampionToObservableCollection(GetLaneCollection(lane, true), champion);
        }

        private void NORAddChampionToLaneList(Champion champion, string lane)
        {
            AddChampionToObservableCollection(GetLaneCollection(lane, false, true), champion);
        }

        private void AddChampionToLaneList(Champion champion, string lane, int occurrences)
        {
            champion.Name += $" (Discovered {occurrences} times)";
            AddChampionToObservableCollection(GetLaneCollection(lane), champion);
        }

        private void AddChampionToObservableCollection(ObservableCollection<Champion> collection, Champion champion)
        {
            if (!collection.Any(c => c.ChampionID == champion.ChampionID))
            {
                collection.Add(champion);
            }
        }

        private void SortChampionListsByOccurrences()
        {
            foreach (var key in Counters.Keys)
            {
                var sortedList = Counters[key]
                    .OrderByDescending(c => ExtractDiscoveryCount(c.Name))
                    .ToList();

                UpdateObservableCollection(Counters[key], sortedList);
            }
        }

        private void BindChampionListsToUI()
        {
            foreach (var listBox in counterListBoxes)
            {
                string key = listBox.Name.Replace("listbox", ""); // Extrage cheia pentru lista corespunzătoare

                if (Counters.TryGetValue(key, out var championList))
                {
                    // Transformă lista pentru a include imaginile asociate
                    var itemsSource = championList.Select(champion => new
                    {
                        ImagePath = champion.ImagePath,
                        Name = champion.Name,
                        AssociatedImages = counterImagePaths.ContainsKey(champion.ChampionID)
                            ? counterImagePaths[champion.ChampionID]
                            : new List<string>() // Fără imagini asociate
                    }).ToList();

                    // Asociază direct noua listă la ListBox
                    listBox.ItemsSource = itemsSource;
                }
            }
        }


        private void UpdateObservableCollection(ObservableCollection<Champion> collection, List<Champion> sortedList)
        {
            collection.Clear();
            foreach (var item in sortedList)
            {
                collection.Add(item);
            }
        }

        private int ExtractDiscoveryCount(string name)
        {
            var match = Regex.Match(name, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }
}