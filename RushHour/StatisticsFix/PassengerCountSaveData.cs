using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.IO;
using RushHour.Utils;

namespace RushHour.StatisticsFix
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff).Date;
        }
    }

    public class CounterResetDate : IDataContainer
    {
        public ushort nodeID;
        public DateTime CountResetDateTime;

        public void Serialize(DataSerializer s)
        {
            s.WriteInt16(nodeID);
            s.WriteLong64(CountResetDateTime.Ticks);
        }

        public void Deserialize(DataSerializer s)
        {
            try
            {
                nodeID = (ushort)s.ReadInt16();
                var ticks = s.ReadLong64();
                CountResetDateTime = new DateTime(ticks);
            }
            catch (Exception ex)
            {
                Log.Info("OnLoadData CounterResetDate Deserialize exception: " + ex.Message);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }


    public class NodePassengerCount : IDataContainer
    {
        public ushort nodeID;
        public ushort passengerCount;

        public void Serialize(DataSerializer s)
        {
            s.WriteInt16(nodeID);
            s.WriteInt16(passengerCount);
        }

        public void Deserialize(DataSerializer s)
        {
            try
            {
                nodeID = (ushort)s.ReadInt16();
                passengerCount = (ushort)s.ReadInt16();
            }
            catch (Exception ex)
            {
                Log.Info("OnLoadData NodePassengerCount Deserialize exception: " + ex.Message);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }

    public class DataModel : IDataContainer
    {
        public Dictionary<ushort, DateTime> resetTimes = new Dictionary<ushort, DateTime>();
        private CounterResetDate[] timesArray;

        public Dictionary<ushort, ushort> lastWeekPassengerCount = new Dictionary<ushort, ushort>();
        private NodePassengerCount[] countArray;

        public void Serialize(DataSerializer s)
        {
            var timesArray = resetTimes.Select(x => new CounterResetDate()
            {
                nodeID = x.Key,
                CountResetDateTime = x.Value
            }).ToArray();
            s.WriteObjectArray(timesArray);
        }

        public void Deserialize(DataSerializer s)
        {
            try
            {
                resetTimes = new Dictionary<ushort, DateTime>();
                
                timesArray = s.ReadObjectArray<CounterResetDate>();
               
            }
            catch (Exception ex)
            {
                Log.Info("OnLoadData Deserialize exception: " + ex.Message);
            }
        }

        public void AfterDeserialize(DataSerializer s)
        {
            foreach (var time in timesArray)
            {
                resetTimes[time.nodeID] = time.CountResetDateTime;
            }
        }
    }
}
