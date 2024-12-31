using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SQLite;

namespace HeroCounterApp
{
    public static class DatabaseHelper
    {
        private const string ConnectionString = "Data Source=C:\\Users\\barbu\\source\\repos\\HeroCounterApp\\HeroCounterApp\\Data\\heroes.db;Version=3;";

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
