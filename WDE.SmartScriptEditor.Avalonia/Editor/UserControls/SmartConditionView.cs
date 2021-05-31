﻿using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using WDE.Common.Avalonia.Controls;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartConditionView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllRequest));

        public static readonly AvaloniaProperty DeselectAllButConditionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButConditionsRequest));

        public static readonly AvaloniaProperty EditConditionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditConditionCommand));
        
        public static readonly AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));
        
        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DirectEditParameter
        {
            get => (ICommand) GetValue(DirectEditParameterProperty);
            set => SetValue(DirectEditParameterProperty, value);
        }
        
        public ICommand DeselectAllButConditionsRequest
        {
            get => (ICommand) GetValue(DeselectAllButConditionsRequestProperty);
            set => SetValue(DeselectAllButConditionsRequestProperty, value);
        }

        public ICommand EditConditionCommand
        {
            get => (ICommand) GetValue(EditConditionCommandProperty);
            set => SetValue(EditConditionCommandProperty, value);
        }

        protected override void OnEdit()
        {
            EditConditionCommand?.Execute(DataContext);
        }

        protected override void OnDirectEdit(object context)
        {
            DirectEditParameter?.Execute(context);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            
            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    return;
                }

                DeselectAllButConditionsRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                e.Handled = true;
            }
        }
    }
}