using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace BotMaker.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Stack<Control> _navigationStack = new Stack<Control>();
        private ContentControl _currentContentControl;

        public NavigationService(ContentControl initialContentControl)
        {
            _currentContentControl = initialContentControl;
        }

        public void NavigateTo<TView>() where TView : Control, new()
        {
            if (_currentContentControl.Content != null)
            {
                _navigationStack.Push((Control)_currentContentControl.Content);
            }

            var view = new TView();

            var viewModelTypeName = typeof(TView).FullName!.Replace("View", "ViewModel");
            var viewModelType = Type.GetType(viewModelTypeName);

            if (viewModelType != null)
            {
                var vm = App.Services.GetService(viewModelType);
                if (vm != null)
                    view.DataContext = vm;
            }

            _currentContentControl.Content = view;
        }


        public void GoBack()
        {
            if (CanGoBack())
            {
                _currentContentControl.Content = _navigationStack.Pop();
            }
        }

        public bool CanGoBack() => _navigationStack.Count > 0;
    }
}