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

    // เรียกเมื่อ RectTransform ของ Panel เปลี่ยนขนาด
    void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        RectTransform rt = (RectTransform)transform;

        float totalWidth = rt.rect.width - grid.padding.left - grid.padding.right;
        float totalHeight = rt.rect.height - grid.padding.top - grid.padding.bottom;

        float cellWidth = (totalWidth - grid.spacing.x) / 2f;
        float cellHeight = (totalHeight - grid.spacing.y) / 2f;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
