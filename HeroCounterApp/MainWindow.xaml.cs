using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.IO;

namespace HeroCounterApp
{
    public partial class MainWindow : Window
    {
        private List<Champion> champions = new List<Champion>();
        public CounterRecommendationsTab counterRecommendationsTab;
        public ObservableCollection<Champion> SelectedCounters { get; set; } = new ObservableCollection<Champion>();

        public ObservableCollection<Champion> SelectedWeaknesses { get; set; } = new ObservableCollection<Champion>();

        public MainWindow()
        {
            InitializeComponent();
            LoadChampions();
            CreateTabClasses();
            InitializeTabs();


            this.DataContext = this;
        }

        private void CreateTabClasses()
        {
            ComboBox[] enemyComboBoxes = new ComboBox[] {
                EnemyChampion1ComboBox,
                EnemyChampion2ComboBox,
                EnemyChampion3ComboBox,
                EnemyChampion4ComboBox,
                EnemyChampion5ComboBox
            };
            ListBox[] counterListBoxes = new ListBox[] {
            EXPCounterslistbox,
            GoldCounterslistbox,
            JungleCounterslistbox,
            MidCounterslistbox,
            RoamCounterslistbox,
            SPEEXPCounterslistbox,
            SPEGoldCounterslistbox,
            SPEJungleCounterslistbox,
            SPEMidCounterslistbox,
            SPERoamCounterslistbox,
            NOREXPlistbox,
            NORGoldlistbox,
            NORJunglelistbox,
            NORMidlistbox,
            NORRoamlistbox
        };
            counterRecommendationsTab = new CounterRecommendationsTab(enemyComboBoxes, counterListBoxes);
        }
        private void InitializeTabs()
        {
            counterRecommendationsTab.Initialize();
        }
        private void LoadChampions()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\heroes.db");
            string imageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\Image");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

                bool isNewDatabase = !File.Exists(dbPath);
                if (isNewDatabase)
                {
                    SQLiteConnection.CreateFile(dbPath);
                }

                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;ReadWrite=True;"))
                {
                    connection.Open();

                    string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Champion (
                    ChampionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Lanes TEXT,
                    Counters TEXT,
                    Weaknesses TEXT,
                    ImagePath TEXT
                )";
                    using (var createTableCommand = new SQLiteCommand(createTableQuery, connection))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }

                    var command = new SQLiteCommand("SELECT ChampionID, Name, ImagePath FROM Champion", connection);
                    var reader = command.ExecuteReader();
                    champions.Clear();

                    var updateCommand = new SQLiteCommand("UPDATE Champion SET ImagePath = @ImagePath WHERE ChampionID = @ChampionID", connection);

                    while (reader.Read())
                    {
                        int championId = reader.GetInt32(reader.GetOrdinal("ChampionID"));
                        string name = reader.GetString(reader.GetOrdinal("Name"));
                        string imagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"));

                        // Construct expected image path
                        string expectedImagePath = Path.Combine(imageDirectory, $"{name}.png");

                        // Check if the image file exists
                        if (File.Exists(expectedImagePath))
                        {
                            // If ImagePath in DB is different, update it
                            if (imagePath != expectedImagePath)
                            {
                                updateCommand.Parameters.Clear();
                                updateCommand.Parameters.AddWithValue("@ImagePath", expectedImagePath);
                                updateCommand.Parameters.AddWithValue("@ChampionID", championId);
                                updateCommand.ExecuteNonQuery();
                                imagePath = expectedImagePath; // Update the local variable
                            }
                        }

                        champions.Add(new Champion
                        {
                            ChampionID = championId,
                            Name = name,
                            ImagePath = imagePath
                        });
                    }

                    // Add a default option for selection
                    champions.Insert(0, new Champion { ChampionID = -1, Name = "Select a Champion" });

                    // Set data sources for comboboxes
                    EnemyChampion1ComboBox.ItemsSource = champions;
                    EnemyChampion2ComboBox.ItemsSource = champions;
                    EnemyChampion3ComboBox.ItemsSource = champions;
                    EnemyChampion4ComboBox.ItemsSource = champions;
                    EnemyChampion5ComboBox.ItemsSource = champions;
                    HeroSelectionComboBox.ItemsSource = champions;
                    ChampionSelectionComboBox.ItemsSource = champions;
                    EditHeroCountersComboBox.ItemsSource = champions;
                    EditHeroWeaknessesComboBox.ItemsSource = champions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading champions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                HeroImagePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void AddHeroButton_Click(object sender, RoutedEventArgs e)
        {
            string name = HeroNameTextBox.Text;
            string lanes = HeroLanesTextBox.Text;
            string counters = HeroCountersTextBox.Text;
            string weaknesses = HeroWeaknessesTextBox.Text;
            string imagePath = HeroImagePathTextBox.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(lanes))
            {
                MessageBox.Show("Please provide a name and at least one role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "INSERT INTO Champion (Name, Lanes, Counters, Weaknesses, ImagePath) VALUES (@Name, @Lanes, @Counters, @Weaknesses, @ImagePath)",
                        connection);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Lanes", lanes);
                    command.Parameters.AddWithValue("@Counters", counters);
                    command.Parameters.AddWithValue("@Weaknesses", weaknesses);
                    command.Parameters.AddWithValue("@ImagePath", imagePath);
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Hero added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadChampions();
                // Clear the input fields
                HeroNameTextBox.Clear();
                HeroLanesTextBox.Clear();
                HeroCountersTextBox.Clear();
                HeroWeaknessesTextBox.Clear();
                HeroImagePathTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding hero: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void EditBrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                EditHeroImagePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (HeroSelectionComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a hero to update.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int heroId = (int)HeroSelectionComboBox.SelectedValue;
            string name = EditHeroNameTextBox.Text.Trim();
            string lanes = EditHeroLanesTextBox.Text.Trim();

            var counterIds = EditHeroCountersListBox.Items
                .Cast<Champion>()
                .Select(champion => champion.ChampionID.ToString())
                .ToList();
            string counters = string.Join(",", counterIds);

            var weaknessIds = EditHeroWeaknessesListBox.Items
                .Cast<Champion>()
                .Select(champion => champion.ChampionID.ToString())
                .ToList();
            string weaknesses = string.Join(",", weaknessIds);

            string imagePath = EditHeroImagePathTextBox.Text.Trim();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    var command = new SQLiteCommand(
                        "UPDATE Champion SET Name = @Name, Lanes = @Lanes, Counters = @Counters, Weaknesses = @Weaknesses, ImagePath = @ImagePath WHERE ChampionID = @ChampionID",
                        connection);

                    // Add parameters
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Lanes", lanes);
                    command.Parameters.AddWithValue("@Counters", counters);
                    command.Parameters.AddWithValue("@Weaknesses", weaknesses);
                    command.Parameters.AddWithValue("@ImagePath", imagePath);
                    command.Parameters.AddWithValue("@ChampionID", heroId);

                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Hero updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadChampions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void EditHeroCountersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditHeroCountersComboBox.SelectedItem is Champion selectedChampion)
            {
                if (!SelectedCounters.Contains(selectedChampion))
                {
                    SelectedCounters.Add(selectedChampion);
                }
                EditHeroCountersListBox.ItemsSource = null;
                EditHeroCountersListBox.ItemsSource = SelectedCounters;
            }
        }


        private void EditHeroWeaknessesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditHeroWeaknessesComboBox.SelectedItem is Champion selectedChampion)
            {
                if (!SelectedWeaknesses.Contains(selectedChampion))
                {
                    SelectedWeaknesses.Add(selectedChampion);
                }
                EditHeroWeaknessesListBox.ItemsSource = null;
                EditHeroWeaknessesListBox.ItemsSource = SelectedWeaknesses;
            }

        }
        private void RemoveCounterButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditHeroCountersListBox.SelectedItem is Champion selectedChampion)
            {
                SelectedCounters.Remove(selectedChampion);
                EditHeroCountersListBox.ItemsSource = null;
                EditHeroCountersListBox.ItemsSource = SelectedCounters;
            }
            else
            {
                MessageBox.Show("Please select a champion to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void RemoveCounterContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (EditHeroCountersListBox.SelectedItem is Champion selectedChampion)
            {
                SelectedCounters.Remove(selectedChampion);
                EditHeroCountersListBox.ItemsSource = null;
                EditHeroCountersListBox.ItemsSource = SelectedCounters;
            }
            else
            {
                MessageBox.Show("No champion selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveWeaknessesButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditHeroWeaknessesComboBox.SelectedItem is Champion selectedChampion)
            {
                SelectedWeaknesses.Remove(selectedChampion);
                EditHeroWeaknessesListBox.ItemsSource = null;
                EditHeroWeaknessesListBox.ItemsSource = SelectedWeaknesses;
            }
            else
            {
                MessageBox.Show("No champion selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveWeaknessesContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (EditHeroWeaknessesListBox.SelectedItem is Champion selectedChampion)
            {
                SelectedWeaknesses.Remove(selectedChampion);
                EditHeroWeaknessesListBox.ItemsSource = null;
                EditHeroWeaknessesListBox.ItemsSource = SelectedWeaknesses;
            }
            else
            {
                MessageBox.Show("No champion selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void HeroSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeroSelectionComboBox.SelectedValue == null)
                return;

            if (HeroSelectionComboBox.SelectedValue is int heroId)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        var command = new SQLiteCommand(
                            "SELECT Name, Lanes, Counters, Weaknesses, ImagePath FROM Champion WHERE ChampionID = @ChampionID",
                            connection);
                        command.Parameters.AddWithValue("@ChampionID", heroId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                EditHeroNameTextBox.Text = SafeGetString(reader, "Name");
                                EditHeroLanesTextBox.Text = SafeGetString(reader, "Lanes");

                                string counters = SafeGetString(reader, "Counters");
                                EditHeroCountersListBox.ItemsSource = GetChampionsByIds(counters);
                                SelectedCounters = new ObservableCollection<Champion>(GetChampionsByIds(counters));

                                string weaknesses = SafeGetString(reader, "Weaknesses");
                                EditHeroWeaknessesListBox.ItemsSource = GetChampionsByIds(weaknesses);
                                SelectedWeaknesses = new ObservableCollection<Champion>(GetChampionsByIds(weaknesses));

                                string imagePath = SafeGetString(reader, "ImagePath");
                                EditHeroImagePathTextBox.Text = imagePath;

                                // Check if the image exists and update the status message
                                if (File.Exists(imagePath))
                                {
                                    ImageStatusTextBlock.Text = "Icon found.";
                                    ImageStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                                }
                                else
                                {
                                    ImageStatusTextBlock.Text = "Icon not found.";
                                    ImageStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading hero data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Selected value is not a valid Hero ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string SafeGetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private List<Champion> GetChampionsByIds(string idString)
        {
            List<Champion> champions = new List<Champion>();

            if (string.IsNullOrWhiteSpace(idString))
                return champions;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string[] ids = idString.Split(',');
                    var command = new SQLiteCommand(
                        $"SELECT ChampionID, Name, Lanes, Counters, Weaknesses, ImagePath FROM Champion WHERE ChampionID IN ({string.Join(", ", ids.Select((_, i) => $"@id{i}"))})",
                        connection);

                    for (int i = 0; i < ids.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@id{i}", ids[i]);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            champions.Add(new Champion
                            {
                                ChampionID = reader.GetInt32(reader.GetOrdinal("ChampionID")),
                                Name = SafeGetString(reader, "Name"),
                                Lanes = SafeGetString(reader, "Lanes"),
                                Counters = SafeGetString(reader, "Counters"),
                                Weaknesses = SafeGetString(reader, "Weaknesses"),
                                ImagePath = SafeGetString(reader, "ImagePath")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching champions by IDs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return champions;
        }

        private void HeroSelectionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)e.OriginalSource;
            string filterText = textBox.Text.ToLower();

            if (string.IsNullOrEmpty(filterText))
            {
                HeroSelectionComboBox.ItemsSource = champions;
            }
            else
            {
                var filteredChampions = champions.Where(heroe =>
                  heroe.Name.ToLower().Contains(filterText)
                ).ToList();
                HeroSelectionComboBox.ItemsSource = null;
                HeroSelectionComboBox.ItemsSource = filteredChampions;
                HeroSelectionComboBox.IsDropDownOpen = true;
            }
        }

        private void CounterSelectionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)e.OriginalSource;
            string filterText = textBox.Text.ToLower();

            if (string.IsNullOrEmpty(filterText))
            {

                EditHeroCountersComboBox.ItemsSource = champions;
            }
            else
            {
                var filteredChampions = champions.Where(heroe =>
                    heroe.Name.ToLower().Contains(filterText)
                ).ToList();
                EditHeroCountersComboBox.ItemsSource = null;
                EditHeroCountersComboBox.ItemsSource = filteredChampions;
                EditHeroCountersComboBox.IsDropDownOpen = true;
            }
        }

        private void WeaknessesSelectionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)e.OriginalSource;
            string filterText = textBox.Text.ToLower();

            if (string.IsNullOrEmpty(filterText))
            {

                EditHeroWeaknessesComboBox.ItemsSource = champions;
            }
            else
            {
                var filteredChampions = champions.Where(heroe =>
                    heroe.Name.ToLower().Contains(filterText)
                ).ToList();
                EditHeroWeaknessesComboBox.ItemsSource = null;
                EditHeroWeaknessesComboBox.ItemsSource = filteredChampions;
                EditHeroWeaknessesComboBox.IsDropDownOpen = true;
            }
        }
        private void FilterChampions(TextBox searchTextBox, ComboBox targetComboBox)
        {
            string filterText;
            if (searchTextBox == null)
                return;
            if (searchTextBox.Text.ToLower() == "search..")
                return;
            filterText = searchTextBox.Text.ToLower();

            if (string.IsNullOrEmpty(filterText))
            {
                targetComboBox.ItemsSource = champions;
            }
            else
            {
                var filteredChampions = champions.Where(heroe =>
                    heroe.Name.ToLower().Contains(filterText)
                ).ToList();
                targetComboBox.ItemsSource = null;
                targetComboBox.ItemsSource = filteredChampions;
                targetComboBox.IsDropDownOpen = true;
            }
        }
        private void Enemy1SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(Enemy1Search, EnemyChampion1ComboBox);
        }

        private void Enemy2SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(Enemy2Search, EnemyChampion2ComboBox);
        }

        private void Enemy3SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(Enemy3Search, EnemyChampion3ComboBox);
        }

        private void Enemy4SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(Enemy4Search, EnemyChampion4ComboBox);
        }

        private void Enemy5SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(Enemy5Search, EnemyChampion5ComboBox);
        }

        private void EnemySearch_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Search..")
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
            else
            {
                textBox.SelectAll();
            }
        }

        private void EnemySearch_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "Search..";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            EnemyChampion1ComboBox.ItemsSource = null;  
            EnemyChampion2ComboBox.ItemsSource = null;
            EnemyChampion3ComboBox.ItemsSource = null;
            EnemyChampion4ComboBox.ItemsSource = null;
            EnemyChampion5ComboBox.ItemsSource = null;
            Enemy1Search.Text = null;
            Enemy2Search.Text = null;
            Enemy3Search.Text = null;
            Enemy4Search.Text = null;
            Enemy5Search.Text = null;
            LoadChampions();
        }
        private void ChampionCountersTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChampionCountersTextBox.Text))
            {
                CountersPlaceholder.Opacity = 1;
            }
            }

        private void ChampionCountersTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CountersPlaceholder.Opacity = 0;
        }

        private void ChampionWeaknessTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChampionWeaknessTextBox.Text))
            {
                WeaknessPlaceholder.Opacity = 1;
            }
        }
        private void ChampionWeaknessTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

            WeaknessPlaceholder.Opacity = 0;
        }

        private void ChampionCounterSelectionSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Search..")
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
            else
            {
                textBox.SelectAll();
            }
        }
        private void ChampionCounterSelectionSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "Search..";
                textBox.Foreground = Brushes.Gray;
            }
        }
        private void ChampionCounterSelectionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChampions(ChampionCounterSelectionSearchTextBox, ChampionSelectionComboBox);
        }
        private void DeleteCountersButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedChampionId = (int)ChampionSelectionComboBox.SelectedValue;

            // Șterge contraconturile din câmpul Counters al campionului selectat
            ClearCountersFromChampion(selectedChampionId);

            // Obține lista de ID-uri din ChampionWeaknessTextBox
            var weaknessesInput = ChampionWeaknessTextBox.Text;
            var weaknessIds = new List<int>();

            foreach (var weaknessName in weaknessesInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var championId = GetChampionIdByName(weaknessName.Trim());
                if (championId.HasValue)
                {
                    weaknessIds.Add(championId.Value);
                }
            }

            // Elimină ID-ul campionului selectat din contraconturile fiecărui campion din lista de slăbiciuni
            foreach (var weaknessId in weaknessIds)
            {
                RemoveCounterFromChampion(weaknessId, selectedChampionId);
            }

            MessageBox.Show("Counters deleted successfully!");
        }

        private void ClearCountersFromChampion(int championId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand("UPDATE Champion SET Counters = '' WHERE ChampionID = @ChampionID", connection);
                command.Parameters.AddWithValue("@ChampionID", championId);
                command.ExecuteNonQuery();
            }
        }

        private void RemoveCounterFromChampion(int championId, int counterId)
        {
            var existingCounters = GetExistingCounters(championId);
            var updatedCounters = existingCounters.Where(id => id != counterId).ToHashSet();
            SaveCountersToChampion(championId, updatedCounters);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedChampionId = (int)ChampionSelectionComboBox.SelectedValue;

            var countersInput = ChampionCountersTextBox.Text;
            var countersIds = new List<int>();

            bool championNotFound = false;
            string notFoundChampions = "";

            foreach (var counterName in countersInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var championId = GetChampionIdByName(counterName.Trim());
                if (championId.HasValue)
                {
                    countersIds.Add(championId.Value);
                }
                else
                {
                    championNotFound = true;
                    notFoundChampions += counterName.Trim() + ", ";
                }
            }
            if (championNotFound)

            {
                ErrorMessageTextBlock.Text = "Champions not found: " + notFoundChampions.TrimEnd(',', ' ');
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                return; 
            }
            else
                ErrorMessageTextBlock.Visibility = Visibility.Hidden;
            var existingCounters = GetExistingCounters(selectedChampionId);
            var allCounters = new HashSet<int>(existingCounters); 

            foreach (var counterId in countersIds)
            {
                allCounters.Add(counterId);
            }

            SaveCountersToChampion(selectedChampionId, allCounters);

            var weaknessesInput = ChampionWeaknessTextBox.Text;
            var weaknessIds = new List<int>();

            foreach (var weaknessName in weaknessesInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var championId = GetChampionIdByName(weaknessName.Trim());
                if (championId.HasValue)
                {
                    weaknessIds.Add(championId.Value);
                }
                else
                {
                    championNotFound = true;
                    notFoundChampions += weaknessName.Trim() + ", ";
                }
            }
            if (championNotFound)

            {
                ErrorMessageTextBlock.Text = "Champions not found: " + notFoundChampions.TrimEnd(',', ' ');
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                return;
            }
            else
                ErrorMessageTextBlock.Visibility = Visibility.Hidden;
            foreach (var weaknessId in weaknessIds)
            {
                var weaknessCounters = GetExistingCounters(weaknessId);
                var weaknessCounterSet = new HashSet<int>(weaknessCounters);
                weaknessCounterSet.Add(selectedChampionId); 

                SaveCountersToChampion(weaknessId, weaknessCounterSet);
            }

            MessageBox.Show("Data saved successfully!");
        }

        private int? GetChampionIdByName(string name)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT ChampionID FROM Champion WHERE Name = @Name", connection);
                command.Parameters.AddWithValue("@Name", name);
                var result = command.ExecuteScalar();
                return result != null ? (int?)Convert.ToInt32(result) : null;
            }
        }

        private List<int> GetExistingCounters(int championId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT Counters FROM Champion WHERE ChampionID = @ChampionID", connection);
                command.Parameters.AddWithValue("@ChampionID", championId);
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    var countersString = result.ToString();
                    return countersString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(id => int.Parse(id.Trim()))
                                         .ToList();
                }
                return new List<int>();
            }
        }

        private void SaveCountersToChampion(int championId, HashSet<int> counters)
        {
            var countersString = string.Join(",", counters);
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand("UPDATE Champion SET Counters = @Counters WHERE ChampionID = @ChampionID", connection);
                command.Parameters.AddWithValue("@Counters", countersString);
                command.Parameters.AddWithValue("@ChampionID", championId);
                command.ExecuteNonQuery();
            }
        }

        private void OverwriteCountersButton_Click(object sender, RoutedEventArgs e)
        {
            bool championNotFound = false;
            string notFoundChampions = "";

            var selectedChampionId = (int)ChampionSelectionComboBox.SelectedValue;

            var countersInput = ChampionCountersTextBox.Text;
            var countersIds = new List<int>();

            foreach (var counterName in countersInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var championId = GetChampionIdByName(counterName.Trim());
                if (championId.HasValue)
                {
                    countersIds.Add(championId.Value);
                }
                else
                {
                    championNotFound = true;
                    notFoundChampions += counterName.Trim() + ", ";
                }

            }
            if (championNotFound)

            {
                ErrorMessageTextBlock.Text = "Champions not found: " + notFoundChampions.TrimEnd(',', ' ');
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                return;
            }
            SaveCountersToChampion(selectedChampionId, new HashSet<int>(countersIds));

            MessageBox.Show("Counters overwritten successfully!");
        }

        private void ChampionSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChampionCountersTextBox.Text= string.Empty;
            ChampionWeaknessTextBox.Text= string.Empty;
            ErrorMessageTextBlock.Visibility = Visibility.Hidden;
        }
        private void ChampionWeaknessTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveButton_Click(sender, e);
        }
    }

    private void ChampionSelectionComboBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveButton_Click(sender, e);
        }
    }
    }
    
}
