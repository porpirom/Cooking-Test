using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGrid2x2 : MonoBehaviour
{
    #region Fields
    private GridLayoutGroup grid;
    private RectTransform rectTransform;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // Cache components
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = transform as RectTransform;

        // Force grid into 2-column layout
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        UpdateCellSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        // Update cell size automatically when parent rect changes
        if (!IsValidRect()) return;
        UpdateCellSize();
    }
    #endregion

    #region Private Methods
    private void UpdateCellSize()
    {
        if (grid == null || rectTransform == null) return;

        float totalWidth = rectTransform.rect.width - grid.padding.left - grid.padding.right;
        float totalHeight = rectTransform.rect.height - grid.padding.top - grid.padding.bottom;

        if (totalWidth <= 0 || totalHeight <= 0) return;

        float cellWidth = (totalWidth - grid.spacing.x) / 2f;
        float cellHeight = (totalHeight - grid.spacing.y) / 2f;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    private bool IsValidRect()
    {
        return rectTransform != null &&
               rectTransform.rect.width > 0 &&
               rectTransform.rect.height > 0;
    }
    #endregion
}
