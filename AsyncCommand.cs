using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ScenarioEditor.ViewModel.ViewModels.Abstract.Classes;
using ScenarioEditor.ViewModel.ViewModels.TaskWrapperViewModels;

namespace ScenarioEditor.ViewModel.Commands.Async
{
    public sealed class AsyncCommand<TResult> : AsyncCommandBase, INotifyPropertyChanged
    {
        #region Properties andFields

        private readonly Func<bool> mCanExecute;
        /// <summary>
        /// Шаблон для работы с задачей.Принимает CancellationToken и возвращает Task или Task<TResult>
        /// </summary>
        private readonly dynamic mFTask;
        private readonly CancelAsyncCommand mCancelCommand;

        /// <summary>
        /// Для отмены задачи
        /// </summary>
        public ICommand CancelCommand
        {
            get { return mCancelCommand; }
        }
        /// <summary>
        /// Свойство для получение информации о задачи
        /// </summary>
        private NotifyTaskCompletion<TResult> mExecution;
        public NotifyTaskCompletion<TResult> Execution
        {
            get { return mExecution; }
            private set
            {
                mExecution = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructors
        public AsyncCommand(Func<CancellationToken, Task<TResult>> fResTask, Func<bool> canExecute)
        {
            if (fResTask == null) throw new NullReferenceException("Входной параметр fResTask==null");
            mFTask = fResTask;
            mCancelCommand = new CancelAsyncCommand();

            mCanExecute = canExecute ?? (() => true);
        }
        public AsyncCommand(Func<CancellationToken, Task> fTask, Func<bool> canExecute)
        {
            if (fTask == null) throw new NullReferenceException("Входной параметр fTask==null");
            mFTask = fTask;
            mCancelCommand = new CancelAsyncCommand();

            mCanExecute = canExecute ?? (() => true);
        }
        #endregion

        #region ICommand

        public override bool CanExecute(object parameter)
        {
            return mCanExecute() && (Execution == null || Execution.IsCompleted);
        }

        public override async Task ExecuteAsync(object parameter)
        {
            mCancelCommand.NotifyCommandStarting();

            Debug.Assert(mFTask is Func<CancellationToken, Task<TResult>> || mFTask is Func<CancellationToken, Task>, "mFTask должен быть одним из типов Func<CancellationToken, Task<TResult>> или Func<CancellationToken, Task");
            Execution = new NotifyTaskCompletion<TResult>(mFTask(mCancelCommand.Token));

            if (Execution.IsCompleted && Execution.IsSuccessfullyCompleted) return;

            RaiseCanExecuteChanged();
            await Execution.TaskCompletion;
            mCancelCommand.NotifyCommandFinished();
            RaiseCanExecuteChanged();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private sealed class CancelAsyncCommand : ICommand
        {
            private CancellationTokenSource mCts = new CancellationTokenSource();
            private bool mCommandExecuting;

            public CancellationToken Token { get { return mCts.Token; } }

            #region Methods
            public void NotifyCommandStarting()
            {
                mCommandExecuting = true;
                if (!mCts.IsCancellationRequested)
                    return;
                //на случай, если задача отменена при старте
                mCts = new CancellationTokenSource();
                RaiseCanExecuteChanged();
            }

            public void NotifyCommandFinished()
            {
                mCommandExecuting = false;
                RaiseCanExecuteChanged();
            }

            private void RaiseCanExecuteChanged()
            {
                CommandManager.InvalidateRequerySuggested();
            }
            #endregion

            #region ICommand

            bool ICommand.CanExecute(object parameter)
            {
                return mCommandExecuting && !mCts.IsCancellationRequested;
            }

            void ICommand.Execute(object parameter)
            {
                mCts.Cancel();
                RaiseCanExecuteChanged();
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            #endregion

        }
    }
}
