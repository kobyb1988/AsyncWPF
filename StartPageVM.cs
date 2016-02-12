using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Feedback;
using Neolant.Foundation.MVVM;
using ScenarioEditor.ViewModel.Commands.Async;
using ScenarioEditor.ViewModel.Commands.EditPanel;
using ScenarioEditor.ViewModel.Extensions.Helpers;
using ScenarioEditor.ViewModel.Infrastructure.Factories;
using ScenarioEditor.ViewModel.Model;
using ScenarioEditor.ViewModel.Model.CustomEvents;
using ScenarioEditor.ViewModel.Model.DataManager;
using ScenarioEditor.ViewModel.ProviderServices;
using ScenarioEditor.ViewModel.ViewModels.Abstract.Interfaces;
using ScenarioEditor.ViewModel.ViewModels.EditPanelViewModels;
using ScenarioEditor.ViewModel.ViewModels.MenuBarViewModels;
using TSCore.Classes;
using TSCore.Classes.Helpers;
using TSCore.Classes.ProviderServices;
using TSCore.Classes.Slides.Enums;
using TSCore.ViewModel;
using Application = System.Windows.Application;
using Constants = ScenarioEditor.ViewModel.Model.Constants;
using ListBox = System.Windows.Controls.ListBox;
using Logger = Feedback.Logger;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ScenarioEditor.ViewModel.ViewModels.StartPageViewModels
{
    public sealed partial class StartPageVM : BaseMenuBarViewModel
    {
        #region Properies And Fields

        private const string cScenarioFolderLocation = ".\\Scenarios";
        private const string cKuNESFolderName = "KuNES";
        private const string cLNPPFolderName = "LNPP";
        private const string cNVPPFolderName = "NvNPP";

        private string mFileName;

        private CancellationTokenSource SceneLoadedTokenSource;

        public delegate void SceneChangedEventHandler(object sender, SceneChangedEventArgs args);

        /// <summary>
        /// Событие вызываемое при выборе мира
        /// </summary>
        public event SceneChangedEventHandler SelectedScene = delegate { };

        private readonly ServiceDialogVMFactory mDialogVMFactory;

        private ObservableCollection<NPP> mScenes;

        /// <summary>
        /// Список Миров
        /// </summary>
        public ObservableCollection<NPP> Scenes
        {
            get
            {
                if (mScenes == null)
                {
                    mScenes = new ObservableCollection<NPP>(NPPManager.Instance.AddRange(new[]
                    {
                        //new NPP
                        //{
                        //    Name = "Курская АЭС",
                        //    AppShortName = "TSApplication",
                        //    StartUpDir = Path.Combine(cScenarioFolderLocation, cKuNESFolderName),
                        //    ImPath = @"/ScenarioEditor.View;component/Resources/Stations/Kunes.png",
                        //    ShortName = "КуАЭС",
                        //    LatitudeNW = 51.845052,
                        //    LongitudeNW = 35.285803,
                        //    LatitudeSE = 51.449083,
                        //    LongitudeSE = 35.918556,
                        //    mapRectangle = new Rectangle {X = -2172, Y = -2250, Width = 4344, Height = 4500},
                        //    MapPath = Directory.GetCurrentDirectory() + "\\KuNES\\#Source Assets\\MapKuNES.jpg"

                        //},
                        //new NPP
                        //{
                        //    Name = "Ленинградская АЭС",
                        //    AppShortName = "LNPP",
                        //    StartUpDir = Path.Combine(cScenarioFolderLocation, cLNPPFolderName),
                        //    ImPath = @"/ScenarioEditor.View;component/Resources/Stations/LNPP.png",
                        //    ShortName = "ЛАЭС",
                        //    LatitudeNW = 60.192479,
                        //    LongitudeNW = 28.675127,
                        //    LatitudeSE = 59.8325078,
                        //    LongitudeSE = 29.39477296,
                        //    mapRectangle = new Rectangle {X = 0, Y = 0, Width = 4855, Height = 4855},
                        //    MapPath = Directory.GetCurrentDirectory() + "\\LNPP\\#Source Assets\\MapLNPP.jpg"
                        //},
                        new NPP
                        {
                            Name = "Нововоронежская АЭС",
                            AppShortName = "NvNPP",
                            StartUpDir = Path.Combine(cScenarioFolderLocation, cNVPPFolderName),
                            ImPath = @"/ScenarioEditor.View;component/Resources/Stations/NVPP.png",
                            ShortName = "НАЭС",
                            LatitudeNW = 51.400001,
                            LongitudeNW = 38.995583,
                            LatitudeSE = 51.129972,
                            LongitudeSE = 39.426014,
                            mapRectangle = new Rectangle {X = 0, Y = 0, Width = 3000, Height = 3000},
                            MapPath = Directory.GetCurrentDirectory() + "\\NvNPP\\#Source Assets\\mapNvNPP.jpg"
                        }
                    }).NPPItems);

                    CurrentScene = mScenes.Single(x => x.AppShortName == "NvNPP");
                    //TODO: Сделать загрузку последнего открытого мира
                }
                return mScenes;
            }
        }

        private ObservableCollection<ScenarioItem> mScenarios;
        /// <summary>
        /// Список открытых сценариев
        /// </summary>
        public ObservableCollection<ScenarioItem> Scenarios
        {
            get { return mScenarios ?? (mScenarios = new ObservableCollection<ScenarioItem>()); }
        }

        ///<summary>
        ///Экземпляр для работы с ProgressBar
        ///</summary>
        public IProgressDialogService ProgressDialogService { get; private set; }

        public NPP CurrentScene
        {
            get { return NPPManager.Instance.CurrentNPP; }
            set
            {
                NPPManager.Instance.SetCurrentNPP(value);
                SelectedScene(this, new SceneChangedEventArgs(value));
                OnPropertyChanged("CurrentScene");
            }
        }


        #region Indicators

        private SolidColorBrush mIndicatorCreate;

        /// <summary>
        /// Индикатор кнопки создать сценарий
        /// </summary>
        public SolidColorBrush IndicatorCreate
        {
            get { return mIndicatorCreate; }
            set { SetProperty(ref mIndicatorCreate, value); }
        }

        private SolidColorBrush mIndicatorLoad;

        /// <summary>
        /// Индикатор кнопки загрузить сценарий
        /// </summary>
        public SolidColorBrush IndicatorLoad
        {
            get { return mIndicatorLoad; }
            set { SetProperty(ref mIndicatorLoad, value); }
        }

        private SolidColorBrush mIndicatorStart3D;

        /// <summary>
        /// Индикатор кнопки запустить 3d
        /// </summary>
        public SolidColorBrush IndicatorStart3D
        {
            get { return mIndicatorStart3D; }
            set { SetProperty(ref mIndicatorStart3D, value); }
        }


        private SolidColorBrush mIndicatorSave;

        public SolidColorBrush IndicatorSave
        {
            get { return mIndicatorSave; }
            set { SetProperty(ref mIndicatorSave, value); }
        }

        #endregion

        /// <summary>
        /// Имя текущего сценария
        /// </summary>
        public string ScenarioName
        {
            get { return DataHolder.Instance.ScenarioProperties.ScenarioName; }
        }

        private string mDynamicText;

        /// <summary>
        /// Текст, меняющийся при загрузки сценария
        /// </summary>
        public string DynamicText
        {
            get { return mDynamicText; }
            set { SetProperty(ref mDynamicText, value); }
        }

        #endregion

        public StartPageVM()
            : base(null)
        {
        }

        public StartPageVM(IProgressDialogService progressDialogService, ServiceDialogVMFactory dialogVMFactory)
            : base(null)
        {
            Init(Guid.NewGuid());
            Icon = @"pack://application:,,,/ScenarioEditor.View;component/Resources/StartPageIcon.png";

            mDialogVMFactory = dialogVMFactory;
            InitStartPage();
            EditPanel = new EditPanelVM(typeof(StartPageVM), Id);
            EditPanel.AddTypes(new List<ICommand> { new VisibileContentCmd("MenuBar", typeof(ListBox), "MenuBarItemTitle", typeof(TextBlock)) });
            Subscribe();
            ProgressDialogService = progressDialogService;
            Task.Factory.StartNew(DataHolder.Instance.PrepareTmpDirectory);
        }

        #region Methods

        private List<BaseMenuBarViewModel> FillCommonAssets()
        {
            var menu = new List<BaseMenuBarViewModel>(mDialogVMFactory.CreateDialogHoldersVM());

            CommonAssets.AddRange(menu);
            return menu;
        }

        private async void SelectedSceneChanged(object sender, SceneChangedEventArgs args)
        {
            if (Window3DVMContext.Current.Start3DVisible) return;

            try
            {
                await Change3DScene(args.NewScene);
            }
            catch (Exception ex)
            {
                string error = string.Format("Ошибка при попытки сменить сцену в 3D. {0}", ex.Message);
                Debug.WriteLine(error);
                Logger.AddMessage(MessageType.Error, error);
            }
        }

        private async Task Change3DScene(NPP newScene)
        {
            SceneLoadedTokenSource = new CancellationTokenSource();
            await Window3DVMContext.Current.LoadSceneAsync(newScene, SceneLoadedTokenSource, ProgressDialogService);
        }

        /// <summary>
        /// Инициализация начальных данных
        /// </summary>
        private void InitStartPage()
        {
            DisplayName = "Стартовая страница";
            IndicatorCreate = Indicators.Warn.GetColor();
            IndicatorLoad = Indicators.Accept.GetColor();
            IndicatorStart3D = Indicators.Accept.GetColor();
            IndicatorSave = Indicators.Accept.GetColor();
            DynamicText = Constants.cLoadScenarioText;
        }

        #region Обработчики загрузки и выгрузки сценария

        /// <summary>
        /// Обработчик команды загрузки сценария
        /// </summary>
        private async Task LoadScenarioHandlerAsync(object param)
        {
            await CheckLoadParameter(param, null, false);
        }

        /// <summary>
        /// Проверка параметров загрузки сценария
        /// </summary>
        /// <param name="param"></param>
        /// <param name="fileName"></param>
        /// <param name="loadingMode"></param>
        /// <returns></returns>
        private async Task<bool> CheckLoadParameter(object param, string fileName, bool loadingMode)
        {
            bool result = false;
            if (IndicatorLoad.IsEqual(Indicators.Warn.GetColor()))
            {
                if (MessageBox.Show(Constants.cInsureChangeScenarioText, Constants.cAttention, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await ChangeScenarioAsync(fileName, loadingMode).ConfigureAwait(false);
                    result = true;
                }
            }
            else if (IndicatorLoad.IsEqual(Indicators.Accept.GetColor()))
            {
                await ChangeScenarioAsync(fileName, loadingMode).ConfigureAwait(false);
                result = true;
            }
            await TaskExtension.Delay(1);
            CommandManager.InvalidateRequerySuggested();
            return await TaskExtension.FromResult(result);
        }

        /// <summary>
        /// Асинхронная смена сценария
        /// </summary>
        /// <returns></returns>
        private async Task ChangeScenarioAsync(string fileName, bool isQuickLoad)
        {
            if (isQuickLoad)
            {
                mFileName = fileName;
                await ProgressDialogService.ExecuteAsync(LoadAsync).ConfigureAwait(false);
            }
            else
            {
                await TaskExtension.Delay(1).ConfigureAwait(false);

                var dialog = new OpenFileDialog { Filter = "Файл с описанием сценария (xmlinfo)|*.xmlinfo" };
                if (!string.IsNullOrEmpty(DataHolder.Instance.FullFolderPath))
                {
                    dialog.InitialDirectory = DataHolder.Instance.FullFolderPath;
                    dialog.FileName = Path.Combine(DataHolder.Instance.FullFolderPath, Constants.cScenarioDescriptor);
                }

                if (dialog.ShowDialog() == true)
                {
                    mFileName = dialog.FileName;
                    await ProgressDialogService.ExecuteAsync(LoadAsync).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Асинхронная загрузка сценария
        /// </summary>
        /// <param name="cancellation"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task LoadAsync(CancellationToken cancellation, IProgress<string> progress)
        {
            await await Task.Factory.StartNew(async () =>
              {
                  await DataHolder.Instance.ReleaseMedia(false, progress, cancellation);
                  progress.Report("Загрузка данных");
                  Thread.Sleep(3000);
                  if (DataHolder.Instance.LoadAllData(mFileName, progress, cancellation))
                      Debug.Assert(Path.GetDirectoryName(mFileName) != null, "Path.GetDirectoryName(mFileName) != null");
                  else
                      while (true)
                      {
                          if (cancellation.IsCancellationRequested)
                              break;
                          Thread.Sleep(100);
                      }
                  if (cancellation.IsCancellationRequested)
                  {
                      await DataHolder.Instance.ClearAllData();
                      return TaskExtension.Delay(1);
                  }

                  DispatcherServices.BeginInvoke(() =>
                  {
                      DynamicText = Constants.cChangeScenarioText;
                      IndicatorLoad = Indicators.Warn.GetColor();
                      IndicatorCreate = Indicators.Warn.GetColor();
                      IndicatorSave = Indicators.Accept.GetColor();
                      ScenarioItem loadedScenario = Scenarios.FirstOrDefault(s => s.ScenarioInfo.Key.Equals(mFileName));
                      if (loadedScenario == null)
                      {
                          loadedScenario = new ScenarioItem(new KeyValuePair<string, string>(mFileName, DataHolder.Instance.ScenarioProperties.ScenarioName));
                          loadedScenario.Selecting += NewScenatioItemOnSelecting;
                          Scenarios.Add(loadedScenario);
                      }
                      loadedScenario.SelecteItem();
                  });
                  return TaskExtension.Delay(1);
              }, cancellation).ConfigureAwait(false);
        }

        /// <summary>
        /// Асинхронное сохранение сценария
        /// </summary>
        /// <returns></returns>
        private async Task SaveScenarioHandlerAsync(object param)
        {
            if (IndicatorSave.IsEqual(Indicators.Accept.GetColor()))
            {
                if (!DataHolder.Instance.SlideDataHolders.Any(x => (x as ISaveValidator).With(valid => valid.OwnScenarioTypes.Contains(DataHolder.Instance.ScenarioProperties.ScenarioType) && !valid.IsValid)))
                {
                    var dialog = new FolderBrowserDialog
                    {
                        SelectedPath = DataHolder.Instance.FullFolderPath
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        await DataHolder.Instance.ReleaseMedia(true, null, new CancellationToken());
                        string directoryName = dialog.SelectedPath;
                        IEnumerable<string> scenarioInfos = Directory.GetFiles(directoryName)
                                                                                        .ToList()
                                                                                        .Where(x => x.Contains(".xmlinfo"))
                                                                                        .Select(Path.GetFileName)
                                                                                        .ToList();
                        if (!scenarioInfos.Any())
                            await ProgressDialogService.ExecuteAsync((cancellation, progress) => UploadScenarioAsync(dialog.SelectedPath, cancellation, progress)).ConfigureAwait(false);
                        else if (scenarioInfos.Any(x => x == Constants.cScenarioDescriptor))
                        {
                            switch (MessageBox.Show(Application.Current.MainWindow, Constants.cInsureRewriteScenario, "Сохранение сценария", MessageBoxButton.YesNo))
                            {
                                case MessageBoxResult.Yes:
                                    await ProgressDialogService.ExecuteAsync(async (cancellation, progress) => await UploadScenarioAsync(dialog.SelectedPath, cancellation, progress)).ConfigureAwait(false);
                                    break;
                                case MessageBoxResult.No:
                                    await TaskExtension.Delay(1);
                                    return;
                            }
                        }
                        else await ProgressDialogService.ExecuteAsync((cancellation, progress) => UploadScenarioAsync(dialog.SelectedPath, cancellation, progress)).ConfigureAwait(false);
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Данный Сценарий нельзя сохранить. Проверьте правильность запонения следующих вкладок для корректного сохранения \n{0}",
                        DataHolder.Instance.SlideDataHolders.Where(x => (x as ISaveValidator).With(valid => valid.OwnScenarioTypes.Contains(DataHolder.Instance.ScenarioProperties.ScenarioType) && !valid.IsValid)).Select(x => x.Name).EnumerableToString("{0}\n")), Constants.cScenarioEditorTitle,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            else throw new NotImplementedException("ScenarioEditor.ViewModel.ViewModels.StartPageViewModelsSaveScenarioHandlerAsync");

            await TaskExtension.Delay(1);
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Асинхронная выгрузка сценария
        /// </summary>
        /// <param name="path">путь к выгрузки</param>
        /// <param name="cancellation"></param>
        /// <param name="progress"></param>
        /// <param name="rewrite">True-перезаписать старые данные</param>
        /// <returns></returns>
        private async Task UploadScenarioAsync(string path, CancellationToken cancellation, IProgress<string> progress, bool rewrite = true)
        {
            await Task.Factory.StartNew(async () =>
            {
                progress.Report("Сохранение сценария");
                Thread.SpinWait(1000);
                DataHolder.Instance.TargetDirectory = path;
                if (rewrite)
                {
                    progress.Report("Очистка старых данных");
                    DataHolder.Instance.PrepareForRewriteScenario();
                }
                if (!DataHolder.Instance.UpLoadData(path, progress, cancellation))
                    //    DataHolder.Instance.ScenarioProperties.Folder = Path.GetDirectoryName(mFileName);
                    //else
                    while (true)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;
                        Thread.Sleep(100);
                    }
                if (cancellation.IsCancellationRequested)
                {
                    await DataHolder.Instance.ClearAllData();
                    return TaskExtension.Delay(1);
                }

                DispatcherServices.BeginInvoke(() =>
                {
                    DynamicText = Constants.cChangeScenarioText;
                    IndicatorLoad = Indicators.Warn.GetColor();
                    IndicatorCreate = Indicators.Warn.GetColor();
                    IndicatorSave = Indicators.Accept.GetColor();
                });
                return TaskExtension.Delay(1);
            }, cancellation).Unwrap().ConfigureAwait(false);

        }
        private async Task CreateNewScenarioAsync(CancellationToken token)
        {
            await await Task.Factory.StartNew(async () =>
            {
                if (MessageBox.Show(Constants.cInsureCreateScenarioText, Constants.cAttention, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    await TaskExtension.Delay(1);
                    return;
                }
                await DataHolder.Instance.ReleaseMedia(false, null, token);
                await DataHolder.Instance.ClearAllData().ConfigureAwait(false);
                await UnselectCurrentScenario();
                await DataHolder.Instance.CreateStartUpData().ConfigureAwait(false);
            }, token, TaskCreationOptions.AttachedToParent, TaskScheduler.FromCurrentSynchronizationContext());
            IndicatorCreate = Indicators.Warn.GetColor();

        }

        /// <summary>
        /// Обработка события выбора нового сценария из списка ранее открытых
        /// </summary>
        /// <param name="newScenario">Выбираемый сценарий</param>
        /// <returns>Возвращает значение, которое определяет можно ли продолжать открытие выбираемого сценария</returns>
        private async Task<bool> NewScenatioItemOnSelecting(ScenarioItem newScenario)
        {
            string key = newScenario.ScenarioInfo.Key;
            if (key.Equals(mFileName))
            {
                Scenarios.Where(s => !s.ScenarioInfo.Key.Equals(key)).ToList().ForEach(s => s.UnselecteItem());
                return await TaskExtension.FromResult(true);
            }
            if (await CheckLoadParameter(this, key, true))
            {
                Scenarios.ToList().ForEach(s => s.UnselecteItem());
                return await TaskExtension.FromResult(true);
            }
            return await TaskExtension.FromResult(false);
        }

        private async Task UnselectCurrentScenario()
        {
            await Task.Factory.StartNew(() =>
            {
                mFileName = string.Empty;
                Scenarios.ToList().ForEach(s => s.UnselecteItem());
            });
        }

        #endregion

        #endregion

        #region ICommand

        #region AsyncCommands
        public IAsyncCommand LoadScenario { get { return mLoadScenario ?? (mLoadScenario = new DelegateAsyncCommand(LoadScenarioHandlerAsync)); } }

        private IAsyncCommand mLoadScenario;

        private IAsyncCommand mSaveScenario;
        public IAsyncCommand SaveScenario { get { return mSaveScenario ?? (mSaveScenario = new DelegateAsyncCommand(SaveScenarioHandlerAsync)); } }

        private IAsyncCommand mCreateNewScenario;
        public IAsyncCommand CreateNewScenario
        {
            get
            {
                return mCreateNewScenario ?? (mCreateNewScenario = AsyncCommand.Create<Task>(token => CreateNewScenarioAsync(token)));
            }
        }

        #endregion

        #region SyncCommands

        private ICommand mLaunchWorldCmd;
        public ICommand LaunchWorldCmd
        {
            get
            {
                return mLaunchWorldCmd ?? (mLaunchWorldCmd = new RelayCommand(obj =>
                {
                    var world = (NPP)obj;
                    NPPManager.Instance.SetCurrentNPP(world);
                    if (world != null) SelectedScene(world, null);
                }, obj =>
                {
                    return obj != null &&
                           obj is NPP;
                }));
            }
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Класс, содержащий краткую информацию о сценарии для отображения их в списке открытых сценариев
    /// </summary>
    public class ScenarioItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Событие выбора сценария
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public delegate Task<bool> SelectingHandler(ScenarioItem sender);
        public event SelectingHandler Selecting;

        /// <summary>
        /// Пара путь к сценарию - его название
        /// </summary>
        public KeyValuePair<string, string> ScenarioInfo { get; set; }

        /// <summary>
        /// Флаг, указывающий выбран ли сценаряий в данный момент
        /// </summary>
        private bool mIsSelected;
        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                mIsSelected = value;
                OnPropertyChanged();
            }
        }

        public ScenarioItem(KeyValuePair<string, string> scenarioInfo)
        {
            ScenarioInfo = scenarioInfo;
        }

        /// <summary>
        /// Клик по сценарию мышкой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async void PreviewLeftMouseButtonDown(object sender, MouseButtonEventArgs args)
        {
            IsSelected = await ScenarioSelecting();
        }

        /// <summary>
        /// Генерация события выбора сценария
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ScenarioSelecting()
        {
            if (Selecting != null)
                return await Selecting(this);
            return false;
        }

        /// <summary>
        /// Выбор нового сценария после его открытия
        /// </summary>
        public async void SelecteItem()
        {
            IsSelected = await ScenarioSelecting();
        }

        /// <summary>
        /// Снятие флага выбора сценария
        /// </summary>
        public void UnselecteItem()
        {
            IsSelected = false;
        }

        #region Реализация INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Реализация INotifyPropertyChanged

    }
}
