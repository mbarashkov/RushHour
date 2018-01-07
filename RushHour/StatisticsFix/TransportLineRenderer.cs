using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using RushHour.Utils;
using UnityEngine;

namespace RushHour.StatisticsFix
{
    class TransportLineRenderer: TransportManager
    {
        private double squareDistance(Vector3 x, Vector3 y)
        {
            return (y.x - x.x) * (y.x - x.x) + (y.y - x.y) * (y.y - x.y);
        }

        private void CustomRenderLines(RenderManager.CameraInfo cameraInfo, int layerMask, int typeMask)
        {
            bool flag = false;

            for (int i = 0; i < 256; i++)
            {
                var line = this.m_lines.m_buffer[i];
                if (line.m_flags != TransportLine.Flags.None)
                {
                    if ((line.m_flags & (TransportLine.Flags.Hidden | TransportLine.Flags.Highlighted)) != TransportLine.Flags.Hidden)
                    {
                        if (this.m_lineMeshData[i] != null)
                        {
                            this.GetType().InvokeMember("UpdateMesh",
                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                BindingFlags.NonPublic, null, this,
                                new object[] {(ushort) i});
                        }
                        if ((line.m_flags & (TransportLine.Flags.Temporary | TransportLine.Flags.Selected | TransportLine.Flags.Highlighted)) != TransportLine.Flags.None)
                        {
                            flag = true;
                        }
                        else
                        {
                            line.RenderLine(cameraInfo, layerMask, typeMask, (ushort)i);
                            
                            Color color = line.GetColor();

                            var stationsList = new Dictionary<int, NetNode>();

                            for (var stopIndex = 0; stopIndex < line.CountStops(0); stopIndex++)
                            {
                                var stop = line.GetStop(stopIndex);

                                var station = Singleton<NetManager>.instance.m_nodes.m_buffer[stop];
                                stationsList[stop] = station;
                            }

                            var skipStopsList = new List<int>();

                            var maxPassengerFlow =
                                StatisticsFixManager.instance.SaveData.lastWeekPassengerCount.Values.Max();
                            if (maxPassengerFlow <= 10)
                            {
                                maxPassengerFlow = 100;
                            }

                            foreach (var stationEntry in stationsList)
                            {
                                if (skipStopsList.Contains(stationEntry.Key))
                                {
                                    continue;                                    
                                }
                                var position = stationEntry.Value.m_position;

                                var sisterStops = new List<KeyValuePair<int, NetNode>>();
                                var sisterStopsList =
                                    stationsList.Where(
                                        x => x.Key != stationEntry.Key && squareDistance(x.Value.m_position, stationEntry.Value.m_position) < 64).OrderBy(x => squareDistance(x.Value.m_position, stationEntry.Value.m_position)).ToList();
                                if (sisterStopsList.Any())
                                {
                                    sisterStops.Add(sisterStopsList.First());                                    
                                }

                                var avgPosition = new Vector3(0, 0, 0);

                                sisterStops.Add(stationEntry);

                                foreach (var sister in sisterStops)
                                {
                                    skipStopsList.Add(sister.Key);
                                    avgPosition.x += sister.Value.m_position.x;
                                    avgPosition.y += sister.Value.m_position.y;
                                    avgPosition.z += sister.Value.m_position.z;
                                }

                                avgPosition.x /= sisterStops.Count;
                                avgPosition.y /= sisterStops.Count;
                                avgPosition.z /= sisterStops.Count;

                                var passengerFlow = 0;

                                foreach (var stationStop in sisterStops)
                                {
                                    ushort count;
                                    StatisticsFixManager.instance.SaveData.lastWeekPassengerCount.TryGetValue(
                                        (ushort) stationStop.Key, out count);
                                    passengerFlow += count;
                                }
                                if (passengerFlow > 0)
                                {
                                    var baseRadius = 100f;
                                    baseRadius *= (float)passengerFlow / (float)maxPassengerFlow;
                                    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, avgPosition, baseRadius, position.y - 100f, position.y + 100f, false, true);                                  
                                }
                            }
                        }
                    }
                }
                else if (this.m_lineMeshes[i] != null)
                {
                    int num = this.m_lineMeshes[i].Length;
                    for (int j = 0; j < num; j++)
                    {
                        UnityEngine.Object.Destroy(this.m_lineMeshes[i][j]);
                    }
                    this.m_lineMeshes[i] = null;
                }
            }
            if (flag)
            {
                Log.Info("renderLines 3");
                for (int j = 0; j < 256; j++)
                {
                    if (this.m_lines.m_buffer[j].m_flags != TransportLine.Flags.None && (this.m_lines.m_buffer[j].m_flags & (TransportLine.Flags.Hidden | TransportLine.Flags.Highlighted)) != TransportLine.Flags.Hidden && (this.m_lines.m_buffer[j].m_flags & (TransportLine.Flags.Temporary | TransportLine.Flags.Selected | TransportLine.Flags.Highlighted)) != TransportLine.Flags.None)
                    {
                        this.m_lines.m_buffer[j].RenderLine(cameraInfo, layerMask, typeMask, (ushort)j);
                    }
                }
            }
        }
    }
}
