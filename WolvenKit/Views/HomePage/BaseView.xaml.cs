using WolvenKit.Functionality.Helpers;

namespace WolvenKit.Views.HomePage
{
    /// <summary>
    /// Interaction logic for ProjectEditorView.xaml
    /// </summary>
    public partial class BaseView
    {
        public BaseView()
        {
            InitializeComponent();

            StaticReferences.GlobalShell = this;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try
            {
                StaticReferences.GlobalShell.DragMove();
            } catch (System.Exception) { }
        }
    }
}
