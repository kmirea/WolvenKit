using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Catel.Data;
using Catel.IoC;
using ReactiveUI;
using WolvenKit.Functionality.Commands;
using WolvenKit.Functionality.Helpers;
using WolvenKit.Functionality.WKitGlobal.Helpers;
using WolvenKit.ViewModels.Shell;

namespace WolvenKit.Views.Shell
{
    public partial class MainView : IViewFor<WorkSpaceViewModel>
    {
        public WorkSpaceViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (WorkSpaceViewModel)value;
        }


        #region Constructors

        public MainView()
        {
            InitializeComponent();
            ViewModel = ServiceLocator.Default.ResolveType<WorkSpaceViewModel>();
            DataContext = ViewModel;

            StaticReferences.MainView = this;
        }


        #endregion Constructors

        #region Methods

        private void RibbonView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        #endregion Methods

    }
}
