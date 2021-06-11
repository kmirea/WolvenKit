using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Catel.Data;
using ReactiveUI;
using WolvenKit.Functionality.Commands;
using WolvenKit.Functionality.Helpers;
using WolvenKit.Functionality.WKitGlobal.Helpers;
using WolvenKit.ViewModels.Shell;

namespace WolvenKit.Views.Shell
{
    public partial class MainView : ReactiveWindow<WorkSpaceViewModel>
    {
        #region fields


        #endregion fields

        #region Constructors

        public MainView()
        {
            InitializeComponent();

            StaticReferences.MainView = this;
        }

        #endregion Constructors

        #region Methods

       

        #endregion Methods


    }
}
