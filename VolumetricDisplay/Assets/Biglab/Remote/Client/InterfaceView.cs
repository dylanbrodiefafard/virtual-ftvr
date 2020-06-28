using System.Linq;

using Biglab.Extensions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Biglab.Remote.Client
{
    public class InterfaceView
    {
        private enum PanelType
        {
            Horizontal,
            Vertical
        }

        // Constants
        private readonly Vector2 _uiElementSize = new Vector2(280f, 30f);
        private Vector2 _uiPanelSize;

        /*
         * Objects needed to draw
         * Hierarchy follows:
         *              Display
         *          /             \ 
         *      _canvas         _menuCanvas
         *         |               |
         *      _elementPanel   Static Menu Button
         */

        private Canvas _commandCanvas;
        private Canvas _overlayCanvas;

        private GameObject _elementPanel;

        // The model that we are rendering for the view
        private readonly RemoteClientMenuController _menuController;

        public InterfaceView(RemoteClientMenuController menuController)
        {
            _menuController = menuController;
            _menuController.ElementsChanged += Draw;

            CreateOverlayCanvas();
            CreateCommandCanvas();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_commandCanvas.transform as RectTransform);

            // call first draw
            Draw();
        }

        #region Setup

        private void CreateCommandCanvas()
        {
            // Create command canvas
            _commandCanvas = CreateCanvas("Command Canvas");
            _commandCanvas.gameObject.SetActive(false);

            // Create background to help show menu items
            var background = _commandCanvas.gameObject.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.66F);

            // We need an event system for canvas interaction
            var eventSystem = _menuController.FindOrCreateComponentReference<EventSystem>();
            eventSystem.gameObject.GetOrAddComponent<StandaloneInputModule>();
            eventSystem.transform.SetParent(_commandCanvas.transform.parent);

            // Create scroll view ( root )
            var scrollView = Object.Instantiate(Resources.Load("ScrollView") as GameObject);
            scrollView.transform.SetParent(_commandCanvas.transform);

            // Fit to full canvas
            var scrollViewTransform = scrollView.GetComponent<RectTransform>();
            scrollViewTransform.offsetMax = new Vector2(0, 0);
            scrollViewTransform.offsetMin = new Vector2(0, 0);

            // Get element panel ( content region )
            _elementPanel = scrollView.GetComponent<ScrollRect>().content.gameObject;

            // Generate the panels that constitute the layout
            var commandCanvasTransform = _commandCanvas.GetComponent<RectTransform>().rect;
            _uiPanelSize = new Vector2(commandCanvasTransform.width, 30f);
        }

        private void CreateOverlayCanvas()
        {
            // Create overlay canvas
            _overlayCanvas = CreateCanvas("Overlay Canvas");
            _overlayCanvas.sortingOrder = 1;

            // 
            var showMenuButtonGameObject = Object.Instantiate(Resources.Load("Button")) as GameObject;
            showMenuButtonGameObject.name = "Command Menu Button";

            SetParent(showMenuButtonGameObject, _overlayCanvas.gameObject);

            var showMenuButtonRectTransform = showMenuButtonGameObject.transform as RectTransform;

            Debug.Assert(showMenuButtonRectTransform != null, nameof(showMenuButtonRectTransform) + " != null");

            showMenuButtonRectTransform.anchorMin = new Vector2(1f, 1f);
            showMenuButtonRectTransform.anchorMax = new Vector2(1f, 1f);
            showMenuButtonRectTransform.pivot = new Vector2(1f, 1f);
            showMenuButtonRectTransform.offsetMin = new Vector2(0, 0);
            showMenuButtonRectTransform.offsetMax = new Vector2(-10, -10);
            showMenuButtonRectTransform.sizeDelta = new Vector2(_uiElementSize.y, _uiElementSize.y);

            AddLayoutConstraint(showMenuButtonGameObject);

            UpdateTextElement(showMenuButtonGameObject.GetComponentInChildren<Text>(), "☰", TextAnchor.MiddleCenter);

            // Bind click event
            var showMenuButton = showMenuButtonGameObject.GetComponent<Button>();
            showMenuButton.onClick.AddListener(() =>
            {
                var go = _commandCanvas.gameObject;
                go.SetActive(!go.activeSelf);
                _menuController.OverlayActive = go.activeSelf;
            });
        }

        #endregion

        #region Utility Functions

        private void AddLayoutConstraint(GameObject go)
            => AddLayoutConstraint(go, _uiElementSize.x);

        private void AddLayoutConstraint(GameObject go, float width)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = _uiElementSize.y;
            layout.minHeight = _uiElementSize.y;
        }

        private static void SetParent(GameObject child, GameObject parent)
            => child.transform.SetParent(parent.transform, false);

        private static void SetParent(GameObject child, Component parent)
            => child.transform.SetParent(parent.transform, false);

        private static void SetParent(Component child, GameObject parent)
            => child.transform.SetParent(parent.transform, false);

        private static void SetParent(Component child, Component parent)
            => child.transform.SetParent(parent.transform, false);

        private void UpdateTextElement(Text text, string content, TextAnchor align)
        {
            text.text = content;
            text.alignment = align;
            text.fontSize = 16;
        }

        private GameObject InstantiateResource(string path, Transform parent)
        {
            var gameObject = Object.Instantiate(Resources.Load(path)) as GameObject;
            Debug.Assert(gameObject != null, $"Unable to load and instantiate instance of '{path}'. Either not a {nameof(GameObject)} or non-existant.");

            // Set parent (if given)
            if (parent != null)
            { gameObject.transform.parent = parent; }

            return gameObject;
        }

        #endregion

        #region Create Elements

        private Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.scaleFactor *= 1.75F;

            // Set parent to menu
            go.transform.SetParent(_menuController.transform, false);

            // 
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            return canvas;
        }

        private GameObject CreateContainer()
        {
            var container = new GameObject("Container");
            container.transform.SetParent(_elementPanel.transform, false);

            var rectTransform = container.AddComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = _uiPanelSize;

            // Element panel will be Horizontally layed out and fit to the content
            var panelLayoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            panelLayoutGroup.childControlWidth = true;
            panelLayoutGroup.childForceExpandWidth = false;
            panelLayoutGroup.spacing = 10;
            panelLayoutGroup.padding = new RectOffset(0, 0, 0, 0); // Zero padding

            AddLayoutConstraint(container);

            return container;
        }

        private Button CreateButton(ButtonData e)
        {
            var buttonGameObject = Object.Instantiate(Resources.Load("Button")) as GameObject;
            SetParent(buttonGameObject, _elementPanel);

            Debug.Assert(buttonGameObject != null, nameof(buttonGameObject) + " != null");

            UpdateTextElement(buttonGameObject.GetComponentInChildren<Text>(), e.Label, TextAnchor.MiddleCenter);

            AddLayoutConstraint(buttonGameObject);

            var btn = buttonGameObject.GetComponent<Button>();
            btn.onClick.AddListener(() => _menuController.Controller.HandleButtonPress(e));

            return btn;
        }

        private GameObject CreateInputField(TextboxData inputElement)
        {
            var container = CreateContainer();

            var inputFieldGameObject = Object.Instantiate(Resources.Load("InputField")) as GameObject;
            var buttonGameObject = Object.Instantiate(Resources.Load("Button")) as GameObject;

            Debug.Assert(buttonGameObject != null, nameof(buttonGameObject) + " != null");

            UpdateTextElement(buttonGameObject.GetComponentInChildren<Text>(), "Submit", TextAnchor.MiddleCenter);

            // Put text field and button in container
            SetParent(inputFieldGameObject, container);
            SetParent(buttonGameObject, container);

            // Add layout constraints
            AddLayoutConstraint(inputFieldGameObject);
            AddLayoutConstraint(buttonGameObject);

            // Update placeholder and initial text
            Debug.Assert(inputFieldGameObject != null, nameof(inputFieldGameObject) + " != null");

            var inputField = inputFieldGameObject.GetComponent<InputField>();
            UpdateTextElement(inputField.placeholder.GetComponent<Text>(), inputElement.Placeholder, TextAnchor.MiddleCenter);

            // Set text value
            // UpdateText(inputField.textComponent, inputElement.Value);
            inputField.text = inputElement.Value;

            // Bind button event
            var button = buttonGameObject.GetComponent<Button>();
            button.onClick.AddListener(() => _menuController.Controller.HandleInputSubmit(inputField, inputElement));

            return container;
        }

        private GameObject CreateDropdownField(DropdownData e)
        {
            var dropdownGameObject = Object.Instantiate(Resources.Load("Dropdown")) as GameObject;
            SetParent(dropdownGameObject, _elementPanel);

            Debug.Assert(dropdownGameObject != null, nameof(dropdownGameObject) + " != null");

            // 
            var dropdown = dropdownGameObject.GetComponent<Dropdown>();

            // Add options
            dropdown.ClearOptions(); // TODO: CC: Isn't it empty by default?
            dropdown.AddOptions(e.OptionList);

            // Set initial selected option
            dropdown.value = e.Selected;

            // 
            dropdown.onValueChanged.AddListener(val =>
            {
                Debug.Log($"Changing Dropdown: {val}/{dropdown.value} on '{e.Id}'");
                _menuController.Controller.HandleDropdownChange(dropdown, e);
                dropdown.RefreshShownValue();
                e.Selected = val;
            });

            AddLayoutConstraint(dropdownGameObject);

            return dropdownGameObject;
        }

        private GameObject GenerateSlider(SliderData e)
        {
            var sliderContainer = CreateContainer();

            var sliderGameObject = Object.Instantiate(Resources.Load("Slider")) as GameObject;
            var labelGameObject = Object.Instantiate(Resources.Load("Text")) as GameObject;

            SetParent(labelGameObject, sliderContainer);
            SetParent(sliderGameObject, sliderContainer);

            Debug.Assert(sliderGameObject != null, nameof(sliderGameObject) + " != null");

            var slider = sliderGameObject.GetComponent<Slider>();
            slider.minValue = e.MinValue;
            slider.maxValue = e.MaxValue;
            slider.value = e.Value;

            Debug.Assert(labelGameObject != null, nameof(labelGameObject) + " != null");

            UpdateTextElement(labelGameObject.GetComponent<Text>(), e.Label, TextAnchor.MiddleLeft);

            AddLayoutConstraint(labelGameObject, _uiElementSize.x * 0.3F);
            AddLayoutConstraint(sliderGameObject, _uiElementSize.x * 0.7F);

            // TODO: CC: Where did `eventTrigger.triggers[0]` come from?
            var eventTrigger = slider.gameObject.GetComponent<EventTrigger>();
            eventTrigger.triggers[0].callback.AddListener((ev) => { e.ClearAllButLast(); });

            slider.onValueChanged.AddListener(val => _menuController.Controller.HandleSliderDrag(slider, e));

            return sliderGameObject;
        }

        private GameObject CreateToggle(ToggleData element)
        {
            var toggleGameObject = Object.Instantiate(Resources.Load("Toggle")) as GameObject;
            SetParent(toggleGameObject, _elementPanel);

            // Set toggle state
            Debug.Assert(toggleGameObject != null, nameof(toggleGameObject) + " != null");

            var toggle = toggleGameObject.GetComponent<Toggle>();
            toggle.isOn = element.Selected;

            // Set toggle text label
            var text = toggleGameObject.GetComponentInChildren<Text>();
            UpdateTextElement(text, element.Label, TextAnchor.MiddleLeft);

            AddLayoutConstraint(toggleGameObject);

            // Bind toggle event
            toggle.onValueChanged.AddListener(val => _menuController.Controller.HandleToggle(toggle, element));

            return toggleGameObject;
        }

        private GameObject CreateLabel(LabelData element)
        {
            var textGameObject = Object.Instantiate(Resources.Load("Text")) as GameObject;
            SetParent(textGameObject, _elementPanel);

            // Set label content
            Debug.Assert(textGameObject != null, nameof(textGameObject) + " != null");

            var text = textGameObject.GetComponent<Text>();
            text.text = element.Text;

            AddLayoutConstraint(textGameObject);

            return textGameObject;
        }

        private GameObject CreateTitle(string title)
        {
            var textGameObject = Object.Instantiate(Resources.Load("Text")) as GameObject;
            SetParent(textGameObject, _elementPanel);

            // Set label content
            Debug.Assert(textGameObject != null, nameof(textGameObject) + " != null");

            var text = textGameObject.GetComponent<Text>();
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = false;
            text.fontSize = 12;
            text.text = title;

            AddLayoutConstraint(textGameObject);

            return textGameObject;
        }

        #endregion

        private void Draw()
        {
            // Destroy all previous elements
            ClearCanvasHierachy();

            // Reconstruct menu
            // TODO: CC: Should probably use a (re)construction approach that doesn't destroy existing elements but only update their state.
            // For each group of elemented ordered and grouped by their group string
            foreach (var group in _menuController.InterfaceElements.Values.OrderBy(e => e.Group).GroupBy(x => x.Group?.Trim()))
            {
                // Create title if not empty or null
                if (!string.IsNullOrEmpty(group.Key))
                {
                    CreateTitle(group.Key);
                }

                // For each element in group ordered by group sorting number
                foreach (var element in group.OrderBy(e => e.Order))
                {
                    DrawElement(element);
                }
            }
        }

        private void DrawElement(ElementData e)
        {
            switch (e.Type)
            {
                case ElementType.Button:
                    CreateButton((ButtonData)e);
                    break;

                case ElementType.InputField:
                    CreateInputField((TextboxData)e);
                    break;

                case ElementType.Dropdown:
                    CreateDropdownField((DropdownData)e);
                    break;

                case ElementType.Slider:
                    GenerateSlider((SliderData)e);
                    break;

                case ElementType.Toggle:
                    CreateToggle((ToggleData)e);
                    break;

                case ElementType.Label:
                    CreateLabel((LabelData)e);
                    break;
            }
        }

        private void ClearCanvasHierachy()
        {
            foreach (var transform in _elementPanel.transform.GetChildren())
            {
                Object.Destroy(transform.gameObject);
            }
        }
    }
}