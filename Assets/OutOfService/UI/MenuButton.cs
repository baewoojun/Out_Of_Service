using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OutOfService
{
    // 메뉴 글자 버튼의 강조 표시. 마우스를 올려도, 방향키로 골라도 같은 모양이 된다.
    // 일시정지 중(timeScale 0)에도 움직이도록 unscaled 시간을 쓴다.
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class MenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] TMP_Text label;
        [SerializeField] UnityEngine.UI.Graphic marker;
        [SerializeField] UnityEngine.UI.Graphic highlight;
        [SerializeField] Color normalColor = new Color(0.62f, 0.64f, 0.61f);
        [SerializeField] Color selectedColor = new Color(0.97f, 0.95f, 0.90f);
        [SerializeField] float slide = 14f;

        bool selected;
        float amount;
        Vector2 labelBase;

        void Awake()
        {
            if (label) labelBase = label.rectTransform.anchoredPosition;
            Apply(0f);
        }

        void OnDisable()
        {
            selected = false;
            amount = 0f;
            Apply(0f);
        }

        void Update()
        {
            float target = selected ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * 9f);
            Apply(Mathf.SmoothStep(0f, 1f, amount));
        }

        void Apply(float t)
        {
            if (label)
            {
                label.color = Color.Lerp(normalColor, selectedColor, t);
                label.rectTransform.anchoredPosition = labelBase + new Vector2(slide * t, 0f);
            }
            if (marker) marker.canvasRenderer.SetAlpha(t);
            if (highlight) highlight.canvasRenderer.SetAlpha(t);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current && GetComponent<UnityEngine.UI.Button>().interactable)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData) { selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = false; }
        public void OnSubmit(BaseEventData eventData) { MenuSound.Click(); }
        public void OnPointerClick(PointerEventData eventData) { MenuSound.Click(); }
    }
}
