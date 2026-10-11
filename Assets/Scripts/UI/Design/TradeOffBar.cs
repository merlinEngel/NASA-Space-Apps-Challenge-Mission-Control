using System.Collections.Generic;
using System.Linq;
using MissionCore;
using TMPro;
using UnityEngine;

namespace MissionGame.UI
{
    public class TradeOffBar : MonoBehaviour
    {
        [SerializeField] TMP_Text lastChangeValueText;
        [SerializeField] List<DeltaChip> deltaChips;
        [SerializeField] RectTransform content;

        // One entry per ModifyDesign call, newest first. An entry can hold several
        // changes (e.g. orbit preset + altitude + inclination), which are undone together.
        readonly List<List<DesignChange>> undoHistory = new();
        const int UndoHistoryMaxLength = 10;

        // True while UndoLastChange runs, so the reverting change is not recorded again.
        bool isUndoing;

        DesignChange? LastChange => undoHistory.Count > 0 ? undoHistory[0][0] : null;

        void Start()
        {
            if (MissionDesignManager.Instance != null)
                MissionDesignManager.Instance.DesignChanged += OnDesignChanged;

            content.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (MissionDesignManager.Instance != null)
                MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
        }

        DesignReport lastCurrent, lastPrevious;

        void OnDesignChanged(DesignReport current, DesignReport previous, List<DesignChange> changes)
        {
            if (!isUndoing && changes != null && changes.Count > 0)
            {
                undoHistory.Insert(0, changes);
                if (undoHistory.Count > UndoHistoryMaxLength)
                    undoHistory.RemoveRange(UndoHistoryMaxLength, undoHistory.Count - UndoHistoryMaxLength);
            }

            lastCurrent = current;
            lastPrevious = previous;
            Redraw();
        }

        void Redraw()
        {
            // Nothing to compare against yet (first evaluation) or nothing left to show.
            if (lastPrevious == null || LastChange == null)
            {
                content.gameObject.SetActive(false);
                return;
            }

            lastChangeValueText.text = LastChange.Value.Format(CatalogManager.Instance.Texts, CatalogManager.Instance.Formatter);
            content.gameObject.SetActive(true);
            UpdateDeltaChips(BuildDeltas(lastPrevious, lastCurrent));
        }

        static Dictionary<BudgetKind, MetricDelta> BuildDeltas(DesignReport before, DesignReport after) => new()
        {
            [BudgetKind.Mass] = new(after.MassResult.TotalMetric, before.MassResult.Total, after.MassResult.Total),
            [BudgetKind.Data] = new(after.DataResult.TotalMetric, before.DataResult.Total, after.DataResult.Total),
            [BudgetKind.Power] = new(after.PowerResult.TotalMetric, before.PowerResult.Total, after.PowerResult.Total),
            [BudgetKind.Cost] = new(after.CostResult.TotalMetric, before.CostResult.Total, after.CostResult.Total),
            [BudgetKind.DeltaV] = new(after.DeltaVResult.TotalMetric, before.DeltaVResult.Total, after.DeltaVResult.Total),
        };

        void UpdateDeltaChips(Dictionary<BudgetKind, MetricDelta> deltas)
        {
            foreach ((BudgetKind kind, MetricDelta delta) in deltas)
            {
                DeltaChip chip = deltaChips.FirstOrDefault(dc => dc.kind == kind);
                if (chip != null) chip.UpdateDelta(delta);
            }
        }

        public void UndoLastChange()
        {
            if (undoHistory.Count == 0) return;

            List<DesignChange> entry = undoHistory[0];
            undoHistory.RemoveAt(0);

            isUndoing = true;
            try
            {
                // Undo in reverse order, all in one ModifyDesign, so it is one evaluation.
                MissionDesignManager.Instance.ModifyDesign(d =>
                {
                    for (int i = entry.Count - 1; i >= 0; i--)
                        entry[i].Undo(d);
                });
            }
            finally
            {
                isUndoing = false;
            }
        }
    }
}
