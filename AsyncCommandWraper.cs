using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ScenarioEditor.ViewModel.Commands.Async
{
    /// <summary>
    /// Статический класс для создания асинхронных управляемых команд
    /// </summary>
    public static class AsyncCommand
    {
        public static AsyncCommand<TResult> Create<TResult>(Func<Task<TResult>> fExecute, Func<bool> canExecute = null)
        {
            if (fExecute == null) throw new NullReferenceException("Входной параметр fExecute==null ");
            var cmd = new AsyncCommand<TResult>(_ => fExecute(), canExecute);

            Debug.Assert(cmd != null, "cmd!=null");
            return cmd;
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<CancellationToken, Task<TResult>> fExecute, Func<bool> canExecute = null)
        {
            if (fExecute == null) throw new NullReferenceException("Входной параметр fExecute==null ");
            var cmd = new AsyncCommand<TResult>(fExecute, canExecute);

            Debug.Assert(cmd != null, "cmd!=null");
            return cmd;
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<CancellationToken, Task> fExecute, Func<bool> canExecute = null)
        {
            if (fExecute == null) throw new NullReferenceException("Входной параметр fExecute==null ");
            var cmd = new AsyncCommand<TResult>(fExecute, canExecute);

            Debug.Assert(cmd != null, "cmd!=null");
            return cmd;
        }
    }
}
