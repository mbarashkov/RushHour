using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RushHour.StatisticsFix
{
    class StatisticsFixManager
    {
        private StatisticsFixManager()
        {
            SaveData = new DataModel();
        }

        public DataModel SaveData;
        
        private static StatisticsFixManager m_instance = null;
        public static StatisticsFixManager instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new StatisticsFixManager();
                }

                return m_instance;
            }
        }
    }
}
