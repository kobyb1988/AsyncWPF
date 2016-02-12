using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Neolant.ASRM.RCPManager.Styles.CodeBehind
{
    public class ErrorHandlingControl : ContentControl
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof (ICommand), typeof (ErrorHandlingControl), new PropertyMetadata(default(ICommand)));

        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
    }
}
