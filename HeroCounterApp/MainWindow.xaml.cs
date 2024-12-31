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
        public ObservableCollection<Champion> SelectedCounters { get; set; }

        public ObservableCollection<Champion> SelectedWeaknesses { get; set; } = new ObservableCollection<Champion>();


        public MainWindow()
        {
            InitializeComponent();
            LoadChampions();
            SelectedCounters = new ObservableCollection<Champion>();
            DataContext = this;
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
            if (HeroSelectionComboBox.SelectedValue == null) return;

            int heroId = (int)HeroSelectionComboBox.SelectedValue;
            string name = EditHeroNameTextBox.Text;
            string lanes = EditHeroLanesTextBox.Text;
            string counters = EditHeroCountersComboBox.Text;
            string weaknesses = EditHeroWeaknessesComboBox.Text;
            string imagePath = EditHeroImagePathTextBox.Text;

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Champion SET Name = @Name, Lanes = @Lanes, Counters = @Counters, Weaknesses = @Weaknesses, ImagePath = @ImagePath WHERE ChampionID = @ChampionID",
                        connection);
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
            }
        }

        private void EditHeroWeaknessesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Champion selectedChampion)
            {
                if (!SelectedWeaknesses.Contains(selectedChampion))
                    SelectedWeaknesses.Add(selectedChampion);
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
                        var command = new SQLiteCommand("SELECT Name, Lanes, Counters, Weaknesses, ImagePath FROM Champion WHERE ChampionID = @ChampionID", connection);
                        command.Parameters.AddWithValue("@ChampionID", heroId);

                        var reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            EditHeroNameTextBox.Text = reader.GetString(0);
                            EditHeroLanesTextBox.Text = reader.GetString(1);
                            EditHeroCountersComboBox.Text = reader.GetString(2);
                            EditHeroWeaknessesComboBox.Text = reader.GetString(3);
                            EditHeroImagePathTextBox.Text = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
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
        private void HeroSelectionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)e.OriginalSource; // Obține TextBox-ul din ComboBox
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
