using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HeroCounterApp
{
    public partial class MainWindow : Window
    {
        private List<Champion> champions = new List<Champion>();
        public ObservableCollection<Champion> SelectedCounters { get; set; } = new ObservableCollection<Champion>();

        public ObservableCollection<Champion> SelectedWeaknesses { get; set; } = new ObservableCollection<Champion>();


        public MainWindow()
        {
            InitializeComponent();
            LoadChampions();
            this.DataContext = this;
        }
        private void LoadChampions()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    var command = new SQLiteCommand("SELECT ChampionID, Name, ImagePath FROM Champion", connection);
                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        champions.Add(new Champion
                        {
                            ChampionID = reader.GetInt32(reader.GetOrdinal("ChampionID")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"))
                        });
                    }

                    EnemyChampion1ComboBox.ItemsSource = champions;
                    EnemyChampion2ComboBox.ItemsSource = champions;
                    EnemyChampion3ComboBox.ItemsSource = champions;
                    EnemyChampion4ComboBox.ItemsSource = champions;
                    EnemyChampion5ComboBox.ItemsSource = champions;
                    HeroSelectionComboBox.ItemsSource = champions;
                    EditHeroCountersComboBox.ItemsSource = champions;
                    EditHeroWeaknessesComboBox.ItemsSource = champions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading champions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EnemyChampionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateCounterRecommendations();
        }
        private void UpdateCounterRecommendations()
        {
            var selectedEnemyHeroes = new List<int>();

            if (EnemyChampion1ComboBox.SelectedValue != null)
                selectedEnemyHeroes.Add((int)EnemyChampion1ComboBox.SelectedValue);
            if (EnemyChampion2ComboBox.SelectedValue != null)
                selectedEnemyHeroes.Add((int)EnemyChampion2ComboBox.SelectedValue);
            if (EnemyChampion3ComboBox.SelectedValue != null)
                selectedEnemyHeroes.Add((int)EnemyChampion3ComboBox.SelectedValue);
            if (EnemyChampion4ComboBox.SelectedValue != null)
                selectedEnemyHeroes.Add((int)EnemyChampion4ComboBox.SelectedValue);
            if (EnemyChampion5ComboBox.SelectedValue != null)
                selectedEnemyHeroes.Add((int)EnemyChampion5ComboBox.SelectedValue);

            var laneCounters = GetLaneCounterRecommendations(selectedEnemyHeroes);

            CountersDataGrid.ItemsSource = laneCounters;
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
        private List<Champion> GetLaneCounterRecommendations(List<int> enemyHeroIds)
        {
            var counters = new List<Champion>();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    var inClause = string.Join(",", enemyHeroIds.Select((_, index) => $"@EnemyId{index}"));
                    var command = new SQLiteCommand($"SELECT * FROM Champion WHERE ChampionID NOT IN ({inClause})", connection);

                    for (int i = 0; i < enemyHeroIds.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@EnemyId{i}", enemyHeroIds[i]);
                    }

                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        counters.Add(new Champion
                        {
                            ChampionID = reader.GetInt32(reader.GetOrdinal("ChampionID")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Lanes = reader.GetString(reader.GetOrdinal("Roles")),
                            Counters = reader.GetString(reader.GetOrdinal("Counters")),
                            Weaknesses = reader.GetString(reader.GetOrdinal("Weaknesses")),
                            ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? "" : reader.GetString(reader.GetOrdinal("ImagePath"))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading counters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return counters;
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

            // Extract counters
            var counterIds = EditHeroCountersListBox.Items
                .Cast<Champion>()
                .Select(champion => champion.ChampionID.ToString())
                .ToList();
            string counters = string.Join(",", counterIds);

            // Extract weaknesses
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
                LoadChampions(); // Reload the updated data
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
                EditHeroCountersListBox.ItemsSource = SelectedWeaknesses;
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
                                // Populate fields with data
                                EditHeroNameTextBox.Text = SafeGetString(reader, "Name");
                                EditHeroLanesTextBox.Text = SafeGetString(reader, "Lanes");

                                // Process counters
                                string counters = SafeGetString(reader, "Counters");
                                EditHeroCountersListBox.ItemsSource = GetChampionsByIds(counters);
                                SelectedCounters = new ObservableCollection<Champion>(GetChampionsByIds(counters));

                                // Process weaknesses
                                string weaknesses = SafeGetString(reader, "Weaknesses");
                                EditHeroWeaknessesListBox.ItemsSource = GetChampionsByIds(weaknesses);
                                SelectedWeaknesses = new ObservableCollection<Champion>(GetChampionsByIds(weaknesses));


                                // Hero image path
                                EditHeroImagePathTextBox.Text = SafeGetString(reader, "ImagePath");
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

        // Helper method to safely retrieve values from the reader
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

                    // Split IDs and use parameters for query
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
            string filterText = textBox.Text;

            if (string.IsNullOrEmpty(filterText))
            {

                HeroSelectionComboBox.ItemsSource = champions;
            }
            else
            {
                var filteredChampions = champions.Where(heroe =>
                    heroe.Name.StartsWith(filterText)
                ).ToList();

                HeroSelectionComboBox.ItemsSource = filteredChampions;
                HeroSelectionComboBox.IsDropDownOpen = true;
            }
        }

        
    }
}
