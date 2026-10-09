using UnityEngine.UIElements;

namespace Game.Unity.UI.Cards
{
    /// <summary>
    /// Fills the face of a card in a container (#123): a title row (position, id, stage mark) and the compact summary,
    /// plus the full details as the container's hover tooltip. It only displays a <see cref="CardSummary"/>; sizes and
    /// colours live in <c>Styles/Common.uss</c> (<c>.card-title</c>, <c>.card-stage</c>, <c>.card-detail</c>).
    /// </summary>
    public static class CardFace
    {
        public const string TitleClass = "card-title";
        public const string StageClass = "card-stage";
        public const string DetailClass = "card-detail";
        public const string TitleRowClass = "card-title-row";

        /// <summary>Adds the card's title row and compact lines to <paramref name="container"/>, and its tooltip.</summary>
        /// <param name="container">The element that represents the card (a button, a cell...).</param>
        /// <param name="title">The heading, for example "2. card_id".</param>
        /// <param name="summary">What the card shows; null leaves only the title.</param>
        public static void Fill(VisualElement container, string title, CardSummary summary)
        {
            var row = new VisualElement();
            row.AddToClassList(TitleRowClass);
            row.pickingMode = PickingMode.Ignore;
            var titleLabel = new Label(title) { name = "card-title" };
            titleLabel.AddToClassList(TitleClass);
            titleLabel.pickingMode = PickingMode.Ignore;
            row.Add(titleLabel);
            if (summary != null && summary.HasStageMark)
            {
                var stage = new Label(summary.StageMarkText) { name = "card-stage" };
                stage.AddToClassList(StageClass);
                stage.pickingMode = PickingMode.Ignore;
                row.Add(stage);
            }

            container.Add(row);
            if (summary == null)
            {
                return;
            }

            var detail = new Label(summary.CompactText) { name = "card-detail" };
            detail.AddToClassList(DetailClass);
            detail.pickingMode = PickingMode.Ignore;
            container.Add(detail);
            HoverDetail.Set(container, summary.TooltipText);
        }
    }
}
