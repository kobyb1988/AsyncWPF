using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ScenarioEditor.ViewModel.ViewModels.Abstract.Interfaces;

namespace ScenarioEditor.ViewModel.ViewModels.Abstract.Classes
{
    public abstract class AsyncCommandBase : IAsyncCommand
    {
        public abstract bool CanExecute(object parameter);

        public abstract Task ExecuteAsync(object parameter);

        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
