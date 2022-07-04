using Andja.Controller;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {

    public enum TrackedStatisticGroups { Unit, City, Structure, Diplomatic, War }

    public enum TrackedUnitStatistics { Built, Destroyed, Lost, PiratesDestroyed }

    public enum TrackedStructureStatistics { Built, Destroyed, Lost, }

    public enum TrackedCityStatistics { Created, Lost, /*Captured*/ }

    public enum TrackedDiplomaticStatistics { Standings, /*Captured*/ }

    public enum TrackedWarStatistics { Times, Declared, Attacked /*Captured*/ }

    /// <summary>
    /// Tracks alot of diffrent Stats so it can be used for achievements and other related things.
    /// </summary>
    [JsonObject]
    public class Statistics {
        private UnitStatistic unitStatistic;
        private CityStatistic cityStatistic;
        private StructureStatistic structureStatistic;
        private DiplomaticStatistic diplomaticStatistic;
        private WarStatistics warStatistic;

        public Statistics(int playerNumber) {
            unitStatistic = new UnitStatistic(playerNumber);
            cityStatistic = new CityStatistic(playerNumber);
            structureStatistic = new StructureStatistic(playerNumber);
            diplomaticStatistic = new DiplomaticStatistic(playerNumber);
            warStatistic = new WarStatistics(playerNumber);
        }

        public void SetUnitStatistic(TrackedUnitStatistics stat) {
            unitStatistic.AddStat(stat);
        }

        public void SetCityStatistic(TrackedCityStatistics stat) {
            cityStatistic.AddStat(stat);
        }

        public void SetStructureStatistic(TrackedStructureStatistics stat) {
            structureStatistic.AddStat(stat);
        }

        public void SetWarStatistic(TrackedWarStatistics stat) {
            warStatistic.AddStat(stat);
        }

        public void SetDiplomaticStatistic(TrackedDiplomaticStatistics stat) {
            diplomaticStatistic.AddStat(stat);
        }

        [JsonObject]
        private abstract class Statistic {
            public int PlayerNumber;

            public Statistic() {
                Setup();
            }

            protected abstract void Setup();

            public Statistic(int playerNumber) {
                PlayerNumber = playerNumber;
            }

            public bool IsTracker(int playernumber) {
                return playernumber == PlayerNumber;
            }
        }

        [JsonObject]
        private class UnitStatistic : Statistic {
            public int Built;
            public int Destroyed;
            public int Lost;
            public int PiratesDestroyed;

            public UnitStatistic(int playerNumber) : base(playerNumber) {
                Setup();
            }

            protected override void Setup() {
                WorldController.Instance.RegisterWorldUnitCreated(OnUnitCreated);
                WorldController.Instance.RegisterWorldUnitDestroyed(OnUnitDestroy);
            }

            private void OnUnitCreated(Unit unit) {
                if (IsTracker(unit.playerNumber)) {
                    AddStat(TrackedUnitStatistics.Built);
                }
            }

            private void OnUnitDestroy(Unit destroyed, IWarfare destroyer) {
                if (IsTracker(destroyed.playerNumber)) {
                    AddStat(TrackedUnitStatistics.Lost);
                }
                if (destroyer != null && IsTracker(destroyer.PlayerNumber)) {
                    if (destroyed.playerNumber == Pirate.Number) {
                        AddStat(TrackedUnitStatistics.PiratesDestroyed);
                    }
                    else {
                        AddStat(TrackedUnitStatistics.Destroyed);
                    }
                }
            }

            public void AddStat(TrackedUnitStatistics stat) {
                switch (stat) {
                    case TrackedUnitStatistics.Built:
                        Built++;
                        break;

                    case TrackedUnitStatistics.Destroyed:
                        Destroyed++;
                        break;

                    case TrackedUnitStatistics.Lost:
                        Lost++;
                        break;

                    case TrackedUnitStatistics.PiratesDestroyed:
                        PiratesDestroyed++;
                        break;
                }
            }
        }

        [JsonObject]
        private class StructureStatistic : Statistic {
            public int Built;
            public int Destroyed;
            public int Lost;

            public StructureStatistic(int playerNumber) : base(playerNumber) {
                Setup();
            }

            protected override void Setup() {
                BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
                BuildController.Instance.RegisterStructureDestroyed(OnStructureDestroyed);
            }

            private void OnStructureDestroyed(Structure str, IWarfare destroyer) {
                if (IsTracker(str.PlayerNumber)) {
                    AddStat(TrackedStructureStatistics.Lost);
                }
                if (destroyer != null && IsTracker(destroyer.PlayerNumber)) {
                    AddStat(TrackedStructureStatistics.Destroyed);
                }
            }

            private void OnStructureCreated(Structure str, bool load) {
                if (load) {
                    return;
                }
                if (IsTracker(str.PlayerNumber)) {
                    AddStat(TrackedStructureStatistics.Built);
                }
            }

            public void AddStat(TrackedStructureStatistics stat) {
                switch (stat) {
                    case TrackedStructureStatistics.Built:
                        Built++;
                        break;

                    case TrackedStructureStatistics.Destroyed:
                        Destroyed++;
                        break;

                    case TrackedStructureStatistics.Lost:
                        Lost++;
                        break;
                }
            }
        }

        [JsonObject]
        private class CityStatistic : Statistic {
            public int Created;
            public int Lost;

            public CityStatistic(int playerNumber) : base(playerNumber) {
                Setup();
            }

            protected override void Setup() {
                BuildController.Instance.RegisterCityCreated(OnCityCreated);
                BuildController.Instance.RegisterAnyCityDestroyed(OnCityDestroyed);
            }

            private void OnCityDestroyed(City city) {
                if (IsTracker(city.PlayerNumber)) {
                    AddStat(TrackedCityStatistics.Lost);
                }
            }

            private void OnCityCreated(City city) {
                if (IsTracker(city.PlayerNumber)) {
                    AddStat(TrackedCityStatistics.Created);
                }
            }

            public void AddStat(TrackedCityStatistics stat) {
                switch (stat) {
                    case TrackedCityStatistics.Created:
                        Created++;
                        break;

                    case TrackedCityStatistics.Lost:
                        Lost++;
                        break;
                }
            }
        }

        [JsonObject]
        private class DiplomaticStatistic : Statistic {

            public DiplomaticStatistic(int playerNumber) : base(playerNumber) {
                Setup();
            }

            protected override void Setup() {
                PlayerController.Instance.RegisterPlayersDiplomacyStatusChange(OnDiplomacyChange);
            }

            private void OnDiplomacyChange(Player one, Player two, DiplomacyType oldType, DiplomacyType newType) {
                if (IsTracker(one.Number) == false && IsTracker(two.Number) == false)
                    return;
            }

            public void AddStat(TrackedDiplomaticStatistics stat) {
                switch (stat) {
                    case TrackedDiplomaticStatistics.Standings:
                        break;
                }
            }
        }

        [JsonObject]
        private class WarStatistics : Statistic {

            public WarStatistics(int playerNumber) : base(playerNumber) {
                Setup();
            }

            protected override void Setup() {
                PlayerController.Instance.RegisterPlayersDiplomacyStatusChange(OnDiplomacyChange);
            }

            private void OnDiplomacyChange(Player one, Player two, DiplomacyType oldType, DiplomacyType newType) {
                if (newType == DiplomacyType.War) {
                    if (IsTracker(one.Number)) {
                        AddStat(TrackedWarStatistics.Times);
                        AddStat(TrackedWarStatistics.Declared);
                    }
                    if (IsTracker(two.Number)) {
                        AddStat(TrackedWarStatistics.Times);
                        AddStat(TrackedWarStatistics.Attacked);
                    }
                }
            }

            public void AddStat(TrackedWarStatistics stat) {
                switch (stat) {
                    case TrackedWarStatistics.Times:
                        break;

                    case TrackedWarStatistics.Declared:
                        break;

                    case TrackedWarStatistics.Attacked:
                        break;
                }
            }

            private struct WarInfo {
                private Time start;
                private Time end;
                private int playerOne;
                private int playerTwo;
            }
        }
    }
}