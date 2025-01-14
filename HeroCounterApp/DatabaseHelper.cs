using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace HeroCounterApp
{
    public static class DatabaseHelper
    {
        static string dbFileName = "heroes.db";
        static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", dbFileName);
        static string ConnectionString = $"Data Source={dbPath};Version=3;ReadWrite=True;";

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        public static List<Champion> GetAllChampions()
        {
            var champions = new List<Champion>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Champion", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        champions.Add(new Champion
                        {
                            ChampionID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Lanes = reader.GetString(2),
                            Counters = reader.GetString(3),
                            Weaknesses = reader.GetString(4)
                        });
                    }
                }
            }
            return champions;
        }

        public static List<Champion> GetCounters(int enemyID, string lane)
        {
            var counters = new List<Champion>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "SELECT * FROM Champion WHERE ChampionID IN (SELECT CounterChampionID FROM Counter WHERE EnemyID = @EnemyID AND Lane LIKE @Lane)",
                    connection);
                command.Parameters.AddWithValue("@EnemyID", enemyID);
                command.Parameters.AddWithValue("@Lane", $"%{lane}%");
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        counters.Add(new Champion
                        {
                            ChampionID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Lanes = reader.GetString(2),
                            Counters = reader.GetString(3),
                            Weaknesses = reader.GetString(4)
                        });
                    }
                }
            }
            return counters;
        }
    }
}
