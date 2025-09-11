using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGrid2x2 : MonoBehaviour
{
    private GridLayoutGroup grid;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2; // 2 columns
        UpdateCellSize(); // เซ็ตครั้งแรก
    }

    void OnRectTransformDimensionsChange()
    {
        // แค่ guard ขนาด <= 0
        RectTransform rt = (RectTransform)transform;
        if (rt == null || rt.rect.width <= 0 || rt.rect.height <= 0) return;

        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        if (grid == null) return;
        RectTransform rt = (RectTransform)transform;
        if (rt == null) return;

        float totalWidth = rt.rect.width - grid.padding.left - grid.padding.right;
        float totalHeight = rt.rect.height - grid.padding.top - grid.padding.bottom;

        if (totalWidth <= 0 || totalHeight <= 0) return;

        float cellWidth = (totalWidth - grid.spacing.x) / 2f;
        float cellHeight = (totalHeight - grid.spacing.y) / 2f;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
