using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using ScenarioEditor.ViewModel.ViewModels.Abstract.Classes;

namespace ScenarioEditor.ViewModel.Commands.Async
{
    /// <summary>
    /// Класс для создания асинхронных не контролируемых комманд
    /// </summary>
    public class DelegateAsyncCommand : AsyncCommandBase
    {
        private readonly Func<object, Task> mCommand;
        private readonly Func<bool> mCondition;

        public DelegateAsyncCommand(Func<object, Task> command, Func<bool> condition = null)
        {
            Contract.Requires(command != null);
            mCommand = command;
            mCondition = condition;
        }

        public override bool CanExecute(object parameter)
        {
            return mCondition == null || mCondition();
        }

        public override Task ExecuteAsync(object parameter)
        {
            return mCommand(parameter);
        }
    }
}
