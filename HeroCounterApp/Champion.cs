using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroCounterApp
{
    public class Champion
    {
        public int ChampionID { get; set; }
        public string Name { get; set; }
        public string Lanes { get; set; }
        public string Counters { get; set; }
        public string Weaknesses { get; set; }
        public string ImagePath { get; set; }
    }

}
