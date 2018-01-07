using System;
using System.Collections.Generic;
using ColossalFramework;
using RushHour.Events;

namespace RushHour.StatisticsFix
{
    public class CustomTransportLineAI : TransportLineAI
    {
        public void CustomNodeSimulationStep(ushort nodeID, ref NetNode data)
        {
            if ((Singleton<SimulationManager>.instance.m_currentFrameIndex & 4095u) >= 3840u)
            {
                data.m_finalCounter += data.m_tempCounter;

                data.m_tempCounter = 0;

                var date = DateTime.Now;
                var resetCounter = false;
                if (StatisticsFixManager.instance.SaveData.resetTimes.ContainsKey(nodeID))
                {
                    date = StatisticsFixManager.instance.SaveData.resetTimes[nodeID];
                   
                    if (CityEventManager.CITY_TIME.Year != date.Year ||
                        CityEventManager.CITY_TIME.StartOfWeek(DayOfWeek.Monday).DayOfYear !=
                        date.StartOfWeek(DayOfWeek.Monday).DayOfYear)
                    {
                        resetCounter = true;
                    }
                }
                else
                {
                    resetCounter = true;
                }
                
                if (resetCounter)
                {
                    StatisticsFixManager.instance.SaveData.resetTimes[nodeID] =
                        CityEventManager.CITY_TIME;
                    data.m_tempCounter = 0;
                    data.m_finalCounter = 0;
                }
                if (StatisticsFixManager.instance.SaveData.lastWeekPassengerCount == null)
                { 
                    StatisticsFixManager.instance.SaveData.lastWeekPassengerCount = new Dictionary<ushort, ushort>();
                }

                StatisticsFixManager.instance.SaveData.lastWeekPassengerCount[nodeID] = data.m_finalCounter;
            }

            if (this.m_publicTransportAccumulation != 0)
            {
                NetManager instance = Singleton<NetManager>.instance;
                int num = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort segment = data.GetSegment(i);
                    if (segment != 0 && (instance.m_segments.m_buffer[(int)segment].m_flags & NetSegment.Flags.PathFailed) == NetSegment.Flags.None)
                    {
                        num += this.m_publicTransportAccumulation >> 1;
                    }
                }
                if (num != 0)
                {
                    int budget = Singleton<EconomyManager>.instance.GetBudget(this.m_info.m_class);
                    int productionRate = PlayerBuildingAI.GetProductionRate(100, budget);
                    num = num * productionRate / 100;
                }
                if (num != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.PublicTransport, num, data.m_position, this.m_publicTransportRadius);
                }
            }
            if ((data.m_problems & Notification.Problem.LineNotConnected) != Notification.Problem.None && data.CountSegments() <= 1 && (data.m_flags & NetNode.Flags.Temporary) == NetNode.Flags.None)
            {
                GuideController properties = Singleton<GuideManager>.instance.m_properties;
                if (properties != null)
                {
                    Singleton<NetManager>.instance.m_transportNodeNotConnected.Activate(properties.m_lineNotFinished, nodeID, Notification.Problem.LineNotConnected, false);
                }
            }
        }
    }
}
