using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TargetPreviewHoverUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text hitChanceText;
    [SerializeField] private TMP_Text damageRangeText;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private Transform statusRoot;
    [SerializeField] private StatusChanceEntryUI statusEntryPrefab;

    private readonly List<StatusChanceEntryUI> spawnedEntries = new List<StatusChanceEntryUI>();

    public void Show(TargetPreviewData data, Vector3 screenPosition)
    {
        if (data == null)
        {
            Hide();
            return;
        }

        if (root != null)
        {
            root.SetActive(true);
            RectTransform rt = root.transform as RectTransform;
            if (rt != null)
                rt.position = screenPosition;
        }

        if (hitChanceText != null)
        {
            hitChanceText.gameObject.SetActive(data.showHitChance);
            if (data.showHitChance)
                hitChanceText.text = string.Format("명중률 {0}%", data.hitChancePercent);
        }

        if (damageRangeText != null)
        {
            damageRangeText.gameObject.SetActive(data.showDamageRange);
            if (data.showDamageRange)
                damageRangeText.text = string.Format("피해 {0}~{1}", data.damageMin, data.damageMax);
        }

        if (successText != null)
        {
            successText.gameObject.SetActive(data.showSuccessOnly);
            if (data.showSuccessOnly)
                successText.text = string.Format("성공률 {0}%", data.successPercent);
        }

        RefreshStatuses(data.statusChances);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
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
