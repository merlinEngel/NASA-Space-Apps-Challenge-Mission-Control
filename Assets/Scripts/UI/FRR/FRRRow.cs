using MissionCore.Design;
using MissionCore.Rules;
using MissionCore.Text;
using MissionGame;
using TMPro;
using Unity.Theme.Binders;
using UnityEngine;

public class FRRRow : MonoBehaviour
{
    [SerializeField] ImageColorBinderRequired indicatorColorBinder;
    [SerializeField] ImageColorBinderRequired statusBorderColorBinder;
    [SerializeField] TextMeshProColorBinderRequired statusTextColorBinder;
    TMP_Text statusText;

    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text detailText;

    TextTable Texts => CatalogManager.Instance.Texts;
    DisplayFormatter Formatter => CatalogManager.Instance.Formatter;

    public ReviewItem Item { get; private set; }
    public ReviewCode Code { get; private set; }

    void Awake()
    {
        statusText = statusTextColorBinder.GetComponent<TMP_Text>();
    }

    public void SetStatus(ReviewItem item)
    {
        Item = item;
        
        titleText.text  = Texts.Get(ReviewText.TitleKey(item.Code));
        detailText.text = Texts.Get(ReviewText.Key(item), ReviewText.Args(item, Formatter));

        indicatorColorBinder.SetColorByName(Constants.StatusColorKeys[Item.Status]);
        statusBorderColorBinder.SetColorByName(Constants.StatusColorKeys[Item.Status]);
        statusTextColorBinder.SetColorByName(Constants.StatusColorKeys[Item.Status]);
        
        switch (Item.Status)
        {
            case BudgetStatus.Green:
            statusText.text = Texts.Get("review.status.ok");
            break;

            case BudgetStatus.Yellow:
            statusText.text = Texts.Get("review.status.risk");
            break;

            case BudgetStatus.Red:
            statusText.text = Texts.Get("review.status.blocked");
            break;
        }
    }
}