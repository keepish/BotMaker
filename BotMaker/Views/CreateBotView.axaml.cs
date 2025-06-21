using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Linq;

namespace BotMaker.Views;

public partial class CreateBotView : UserControl
{
    public CreateBotView()
    {
        InitializeComponent();
    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var collapsed = border.GetVisualDescendants()
                             .OfType<StackPanel>()
                             .FirstOrDefault(sp => sp.Name == "CollapsedView");
            var expanded = border.GetVisualDescendants()
                                 .OfType<StackPanel>()
                                 .FirstOrDefault(sp => sp.Name == "ExpandedView");
            if (collapsed != null && expanded != null)
            {
                collapsed.IsVisible = false;
                expanded.IsVisible = true;
            }
        }
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var collapsed = border.GetVisualDescendants()
                             .OfType<StackPanel>()
                             .FirstOrDefault(sp => sp.Name == "CollapsedView");
            var expanded = border.GetVisualDescendants()
                                 .OfType<StackPanel>()
                                 .FirstOrDefault(sp => sp.Name == "ExpandedView");
            if (collapsed != null && expanded != null)
            {
                collapsed.IsVisible = true;
                expanded.IsVisible = false;
            }
        }
    }

}