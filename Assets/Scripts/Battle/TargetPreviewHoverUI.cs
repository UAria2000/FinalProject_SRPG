using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TargetPreviewHoverUI : HoverPopupUIBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text hitChanceText;
    [SerializeField] private TMP_Text damageRangeText;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private Transform statusRoot;
    [SerializeField] private StatusChanceEntryUI statusEntryPrefab;

    private readonly List<StatusChanceEntryUI> spawnedEntries = new List<StatusChanceEntryUI>();

    public void Show(TargetPreviewData data, Vector2 pointerScreenPosition)
    {
        if (data == null)
        {
            Hide();
            return;
        }

        ShowRootAt(root, pointerScreenPosition);

        if (hitChanceText != null)
        {
            hitChanceText.gameObject.SetActive(data.showHitChance);
            if (data.showHitChance)
                hitChanceText.text = $"명중률 {data.hitChancePercent}%";
        }

        if (damageRangeText != null)
        {
            damageRangeText.gameObject.SetActive(data.showDamageRange);
            if (data.showDamageRange)
                damageRangeText.text = $"피해 {data.damageMin}~{data.damageMax}";
        }

        if (successText != null)
        {
            successText.gameObject.SetActive(data.showSuccessOnly);
            if (data.showSuccessOnly)
                successText.text = $"성공률 {data.successPercent}%";
        }

        RefreshStatuses(data.statusChances);
    }

    public void Hide()
    {
        HideRoot(root);
    }

    private void RefreshStatuses(List<StatusChancePreviewData> statuses)
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
                Destroy(spawnedEntries[i].gameObject);
        }
        spawnedEntries.Clear();

        if (statusRoot == null || statusEntryPrefab == null || statuses == null)
            return;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusChanceEntryUI entry = Instantiate(statusEntryPrefab, statusRoot);
            entry.Bind(statuses[i]);
            spawnedEntries.Add(entry);
        }
    }
}
