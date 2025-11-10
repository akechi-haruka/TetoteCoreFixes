using System.Collections.Generic;
using Lod;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

namespace TetoteCustomMasterData;

public class MarkerAssetWorkaround {
    public int markerNum;
    public float synchroScoreBase;
    public MarkerChartData2[] charts;
    public EventChartData2 eventData;
    
}

public class MarkerChartData2 {
    public float begin;
    public ChartType type;
    public MarkerChartData.RepeatedlyGroupData[] groupList;
    public List<MarkerChartData.MarkerData> m_Markers;
}

public class EventChartData2 {
    public float begin;
    public ChartType type;
    public List<EventChartData.EventData> m_Events;
    public List<TempoCode> m_TempoMapCode;
    public List<EventChartData.ReadyAnimationData> m_ReadyAnimationCode;
}