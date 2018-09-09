using System;
using System.Collections.Generic;
using ColossalFramework;
using ICities;

namespace AutomaticBulldozer.Source
{
    public class Info : IUserMod
    {
        public string Name
        {
            get { return "Automatic Bulldozer"; }
        }

        public string Description
        {
            get { return "Automatic bulldozing for abandoned buildings."; }
        }
    }

    public class Loader : LoadingExtensionBase
    {
        private AutomaticBulldozer _automaticBulldozer;

        public override void OnLevelLoaded(LoadMode mode)
        {
            _automaticBulldozer = new AutomaticBulldozer();
            base.OnLevelLoaded(mode);
        }

        public override void OnLevelUnloading()
        {
            if (_automaticBulldozer != null)
            {
                _automaticBulldozer.Destroy();
            }
            _automaticBulldozer = null;
            base.OnLevelUnloading();
        }
    }

    public class AutomaticBulldozer
    {
        private readonly BuildingManager _buildingManager;
        private readonly BuildingObserver _observer;

        public AutomaticBulldozer()
        {
            _buildingManager = Singleton<BuildingManager>.instance;
            _observer = new BuildingObserver(FindExistingBuildings());
            BindEvents();
        }

        private void BindEvents()
        {
            Timer.SimulationSecondPassed += OnSimulationSecondPassed;
        }

        private void OnSimulationSecondPassed(object source, EventArgs args)
        {
            var abandonedBuildings = FindAbandonedBuildings();
            if (abandonedBuildings.Count > 0) DestroyBuildings(abandonedBuildings);
        }

        private void DestroyBuildings(IEnumerable<ushort> abandonedBuildingIds)
        {
            foreach (var buildingId in abandonedBuildingIds)
            {
                if (_buildingManager.m_buildings.m_buffer[buildingId].Info.m_buildingAI.CheckBulldozing(buildingId, ref _buildingManager.m_buildings.m_buffer[buildingId]) != ToolBase.ToolErrors.None) return;
                _buildingManager.ReleaseBuilding(buildingId);
            }
        }

        private List<ushort> FindAbandonedBuildings()
        {
            var buildingIds = new List<ushort>(_observer.BuildingsIds);
            var abandonedBuildings = new List<ushort>();

            foreach (var buildingId in buildingIds)
            {
                var building = _buildingManager.m_buildings.m_buffer[buildingId];
                if (building.m_flags.IsFlagSet(Building.Flags.Abandoned)) abandonedBuildings.Add(buildingId);
            }

            return abandonedBuildings;
        }

        private List<ushort> FindExistingBuildings()
        {
            var buildingIds = new List<ushort>();
            for (var i = 0; i < _buildingManager.m_buildings.m_buffer.Length; i++)
                if (_buildingManager.m_buildings.m_buffer[i].m_flags != Building.Flags.None &&
                    !Building.Flags.Original.IsFlagSet(_buildingManager.m_buildings.m_buffer[i].m_flags))
                    buildingIds.Add((ushort) i);
            return buildingIds;
        }

        public void Destroy()
        {
            Timer.SimulationSecondPassed -= OnSimulationSecondPassed;
            _observer.Destroy();
        }
    }

    public class BuildingObserver
    {
        private readonly BuildingManager _buildingManager;

        public BuildingObserver(List<ushort> buildingsIds)
        {
            BuildingsIds = buildingsIds;
            _buildingManager = Singleton<BuildingManager>.instance;
            BindEvents();
        }

        public List<ushort> BuildingsIds { get; set; }

        private void BindEvents()
        {
            _buildingManager.EventBuildingCreated += OnBuildingCreated;
            _buildingManager.EventBuildingReleased += OnBuildingReleased;
        }

        public void OnBuildingCreated(ushort id)
        {
            BuildingsIds.Add(id);
        }

        public void OnBuildingReleased(ushort id)
        {
            BuildingsIds.Remove(id);
        }

        public void Destroy()
        {
            _buildingManager.EventBuildingCreated -= OnBuildingCreated;
            _buildingManager.EventBuildingReleased -= OnBuildingReleased;
        }
    }

    public class Timer : ThreadingExtensionBase
    {
        public delegate void SimulationSecondPassedEventHandler(object source, EventArgs args);

        private float _counter;

        public static event SimulationSecondPassedEventHandler SimulationSecondPassed;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // Called every 1 second simulation time (eg. when the game is not paused)
            if (_counter >= 1)
            {
                if (SimulationSecondPassed != null)
                {
                    SimulationSecondPassed(this, EventArgs.Empty);
                }
                _counter = 0;
            }
            else
            {
                _counter += simulationTimeDelta;
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
    }
}
