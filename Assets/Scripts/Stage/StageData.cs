using System;

namespace TiraWantToCross.Stage
{
    [Serializable]
    public class StageData
    {
        public string stageId;
        public string title;
        public int optimalMoves;
        public BoatData boat;
        public LocationData[] locations;
        public RouteData[] routes;
        public EntityData[] entities;
        public FailConditionData[] failConditions;
    }

    [Serializable]
    public class BoatData
    {
        public int capacity;
        public string startLocation;
    }

    [Serializable]
    public class LocationData
    {
        public string locationId;
        public string displayName;
    }

    [Serializable]
    public class RouteData
    {
        public string routeId;
        public string from;
        public string to;
        public bool bidirectional;
    }

    [Serializable]
    public class EntityData
    {
        public string entityId;
        public string displayName;
        public string startLocation;
        public bool canOperateBoat;
    }

    [Serializable]
    public class FailConditionData
    {
        // failConditions の将来拡張向けプレースホルダー
    }
}
