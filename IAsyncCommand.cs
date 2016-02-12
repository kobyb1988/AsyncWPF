using System.Threading.Tasks;
using System.Windows.Input;

namespace ScenarioEditor.ViewModel.ViewModels.Abstract.Interfaces
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}
