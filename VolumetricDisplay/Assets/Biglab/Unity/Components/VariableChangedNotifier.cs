using System;
using System.Collections.Generic;

/// <summary>
/// You can use this class to manage detecting when public properties or fields change and automatically invoke delegates when they do.
/// </summary>
public class VariableChangedNotifier : SingletonMonobehaviour<VariableChangedNotifier>
{
    /// <summary>
    /// The delegate that handles the OnChange event of a variable.
    /// </summary>
    /// <param name="previous">The previous value of the variable.</param>
    /// <param name="current">The current value of the variable.</param>
    public delegate void VariableChangedHandler(object previous, object current);


    /// <summary>
    /// The delegate that handles detecting if a variable has changed.
    /// </summary>
    /// <param name="previous">The previous value of the variable.</param>
    /// <param name="current">The current value of the variable.</param>
    public delegate bool ChangeDetector(object previous, object current);

    private List<VariableChangeDetector> _variableDetectors;

    private class VariableChangeDetector
    {
        public object PreviousValue;
        public VariableChangedHandler OnChangedHandler;
        public Func<object> GetCurrentValue;
        public ChangeDetector HasChanged;
    }

    #region monobehaviour

    protected override void Awake()
    {
        base.Awake();

        _variableDetectors = new List<VariableChangeDetector>();
    }

    private void Update()
    {
        foreach (var variableDetector in _variableDetectors)
        {
            var previousValue = variableDetector.PreviousValue;
            var currentValue = variableDetector.GetCurrentValue();

            if (!variableDetector.HasChanged(previousValue, currentValue))
            {
                continue; // No change detected
            }

            variableDetector.PreviousValue = currentValue;
            variableDetector.OnChangedHandler?.DynamicInvoke(previousValue, currentValue);
        }
    }

    #endregion

    /// <summary>
    /// Subscribes to changes in the given properties. Use nameof() to get the names of the properties.
    /// </summary>
    /// <param name="container">The containing object.</param>
    /// <param name="detector">The function that will detect if the property has changed. See <see cref="ChangeDetector"/></param>
    /// <param name="handler">The handler that will be called when a property changes.</param>
    /// <param name="propertyNames">The property names in the containing object.</param>
    public void PropertySubscribe(object container, ChangeDetector detector, VariableChangedHandler handler, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            Func<object> valueGetter = () => container.GetType().GetProperty(propertyName)?.GetValue(container);
            VariableSubscribe(detector, handler, valueGetter);
        }
    }

    /// <summary>
    /// Subscribes to changes in the given fields. Use nameof() to get the names of the fields.
    /// </summary>
    /// <param name="container">The containing object.</param>
    /// <param name="detector">The function that will detect if the property has changed. See <see cref="ChangeDetector"/></param>
    /// <param name="handler">The handler that will be called when a field changes.</param>
    /// <param name="fieldNames">The field names in the containing object</param>
    public void FieldSubscribe(object container, ChangeDetector detector, VariableChangedHandler handler, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            Func<object> valueGetter = () => container.GetType().GetField(fieldName)?.GetValue(container);
            VariableSubscribe(detector, handler, valueGetter);
        }
    }

    private void VariableSubscribe(ChangeDetector detector, VariableChangedHandler handler, Func<object> valueGetter)
        => _variableDetectors.Add(new VariableChangeDetector
        {
            OnChangedHandler = handler,
            GetCurrentValue = valueGetter,
            PreviousValue = valueGetter(),
            HasChanged = detector
        });
}