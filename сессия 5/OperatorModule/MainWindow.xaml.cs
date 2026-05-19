using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OperatorModule.Services;
using OperatorModule.Models;

namespace OperatorModule
{
    public partial class MainWindow : Window
    {
        private DbService _db;
        private int _selectedBatchId;
        private Batch _selectedBatch;
        private List<TechMapStep> _techSteps;
        private List<BatchStep> _completedSteps;
        private BatchStep _currentStep;
        private int _currentStepIndex;

        public MainWindow()
        {
            InitializeComponent();
            _db = new DbService();
            
            BtnLogin.Click += BtnLogin_Click;
            BtnShowRegister.Click += BtnShowRegister_Click;
            BtnRegister.Click += BtnRegister_Click;
            BtnShowLogin.Click += BtnShowLogin_Click;
            BtnLogout.Click += BtnLogout_Click;
            BtnRefreshBatches.Click += BtnRefreshBatches_Click;
            BtnStartStep.Click += BtnStartStep_Click;
            BtnCompleteStep.Click += BtnCompleteStep_Click;
            BtnReportDeviation.Click += BtnReportDeviation_Click;
            BtnBackToBatches.Click += BtnBackToBatches_Click;
            
            DgBatches.SelectionChanged += DgBatches_SelectionChanged;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtLoginEmail.Text))
                {
                    TxtLoginError.Text = "Введите email!";
                    return;
                }

                var user = await _db.LoginAsync(TxtLoginEmail.Text, TxtLoginPassword.Password);
                
                TxtUserInfo.Text = $"👨‍🔧 {user.FullName} (Аппаратчик)";
                
                LoginPanel.Visibility = Visibility.Collapsed;
                MainPanel.Visibility = Visibility.Visible;
                
                await LoadActiveBatches();
            }
            catch (Exception ex)
            {
                TxtLoginError.Text = ex.Message;
            }
        }

        private void BtnShowRegister_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
            TxtRegError.Text = "";
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtRegPassword.Password != TxtRegConfirmPassword.Password)
                {
                    TxtRegError.Text = "Пароли не совпадают!";
                    return;
                }

                if (string.IsNullOrEmpty(TxtRegEmail.Text))
                {
                    TxtRegError.Text = "Введите email!";
                    return;
                }

                if (string.IsNullOrEmpty(TxtRegFullName.Text))
                {
                    TxtRegError.Text = "Введите ФИО!";
                    return;
                }

                if (TxtRegPassword.Password.Length < 3)
                {
                    TxtRegError.Text = "Пароль должен быть не менее 3 символов!";
                    return;
                }

                TxtRegError.Text = "Регистрация...";

                var user = await _db.RegisterAsync(
                    TxtRegEmail.Text, 
                    TxtRegPassword.Password, 
                    TxtRegFullName.Text, 
                    "operator"
                );

                MessageBox.Show($"✅ Регистрация успешна!\n\nEmail: {user.Email}\nФИО: {user.FullName}\n\nТеперь вы можете войти в систему.", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoginPanel.Visibility = Visibility.Visible;
                RegisterPanel.Visibility = Visibility.Collapsed;
                TxtLoginEmail.Text = TxtRegEmail.Text;
                TxtRegError.Text = "";
            }
            catch (Exception ex)
            {
                TxtRegError.Text = ex.Message;
            }
        }

        private void BtnShowLogin_Click(object sender, RoutedEventArgs e)
        {
            RegisterPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            TxtLoginEmail.Text = "";
            TxtLoginPassword.Password = "";
            BatchesPanel.Visibility = Visibility.Visible;
            ProgramPanel.Visibility = Visibility.Collapsed;
        }

        private async Task LoadActiveBatches()
        {
            try
            {
                var batches = await _db.GetActiveBatchesAsync();
                DgBatches.ItemsSource = batches;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки партий: {ex.Message}");
            }
        }

        private async void BtnRefreshBatches_Click(object sender, RoutedEventArgs e)
        {
            await LoadActiveBatches();
        }

        private async void DgBatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgBatches.SelectedItem is Batch batch)
            {
                _selectedBatch = batch;
                _selectedBatchId = batch.Id;
                
                await LoadProgram(batch);
            }
        }

        private async Task LoadProgram(Batch batch)
        {
            try
            {
                _techSteps = await _db.GetTechMapStepsAsync(batch.TechMapId);
                _completedSteps = await _db.GetBatchStepsAsync(batch.Id);
                _currentStep = await _db.GetCurrentBatchStepAsync(batch.Id);
                
                TxtBatchNumber.Text = batch.BatchNumber;
                TxtBatchStatus.Text = batch.Status == "running" ? "В процессе" : "Запланирована";
                
                var product = await _db.GetProductByTechMapIdAsync(batch.TechMapId);
                TxtProductName.Text = product?.Name ?? "Неизвестно";
                
                var stepsWithStatus = _techSteps.Select(step => new
                {
                    step.StepOrder,
                    step.StepName,
                    step.StepType,
                    Status = _completedSteps.Any(s => s.StepOrder == step.StepOrder) ? "✅ Выполнен" :
                             (_currentStep != null && _currentStep.StepOrder == step.StepOrder) ? "🟢 В процессе" : "⏳ Ожидает"
                }).ToList();
                
                DgSteps.ItemsSource = stepsWithStatus;
                
                if (_currentStep != null)
                {
                    var currentTechStep = _techSteps.FirstOrDefault(s => s.StepOrder == _currentStep.StepOrder);
                    if (currentTechStep != null)
                    {
                        DisplayCurrentStep(currentTechStep);
                        _currentStepIndex = currentTechStep.StepOrder;
                    }
                }
                else
                {
                    var nextStep = _techSteps.FirstOrDefault(s => !_completedSteps.Any(c => c.StepOrder == s.StepOrder));
                    if (nextStep != null)
                    {
                        DisplayCurrentStep(nextStep);
                        _currentStepIndex = nextStep.StepOrder;
                    }
                }
                
                BatchesPanel.Visibility = Visibility.Collapsed;
                ProgramPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки программы: {ex.Message}");
            }
        }

        private void DisplayCurrentStep(TechMapStep step)
        {
            TxtCurrentStep.Text = step.StepName;
            TxtInstruction.Text = step.Instruction ?? "Нет инструкции";
            TxtPlannedTemp.Text = step.PlannedTempC?.ToString() ?? "—";
            TxtPlannedPressure.Text = step.PlannedPressureBar?.ToString() ?? "—";
            TxtPlannedDuration.Text = step.PlannedDurationMin?.ToString() ?? "—";
            
            TxtActualTemp.Text = "";
            TxtActualPressure.Text = "";
            TxtActualDuration.Text = "";
        }

        private async void BtnStartStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentStep != null)
                {
                    MessageBox.Show("Текущий шаг уже начат!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var stepToStart = _techSteps.FirstOrDefault(s => s.StepOrder == _currentStepIndex);
                if (stepToStart == null)
                {
                    MessageBox.Show("Нет шагов для выполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                _currentStep = await _db.StartBatchStepAsync(_selectedBatchId, stepToStart.StepOrder, stepToStart.StepName, _db.CurrentUser.Id);
                
                MessageBox.Show($"Шаг \"{stepToStart.StepName}\" начат!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                
                await LoadProgram(_selectedBatch);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnCompleteStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentStep == null)
                {
                    MessageBox.Show("Нет активного шага! Начните шаг сначала.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                decimal? actualTemp = null;
                decimal? actualPressure = null;
                int? actualDuration = null;
                
                if (!string.IsNullOrEmpty(TxtActualTemp.Text))
                    actualTemp = decimal.Parse(TxtActualTemp.Text);
                
                if (!string.IsNullOrEmpty(TxtActualPressure.Text))
                    actualPressure = decimal.Parse(TxtActualPressure.Text);
                
                if (!string.IsNullOrEmpty(TxtActualDuration.Text))
                    actualDuration = int.Parse(TxtActualDuration.Text);
                
                await _db.CompleteBatchStepAsync(_currentStep.Id, actualTemp, actualPressure, actualDuration, "");
                
                MessageBox.Show($"Шаг \"{_currentStep.StepName}\" завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _currentStep = null;
                _currentStepIndex++;
                
                await LoadProgram(_selectedBatch);
                
                var allSteps = _techSteps.Count;
                var completed = await _db.GetBatchStepsAsync(_selectedBatchId);
                
                if (completed.Count >= allSteps)
                {
                    await _db.CompleteBatchAsync(_selectedBatchId);
                    MessageBox.Show($"🎉 Партия {_selectedBatch.BatchNumber} полностью выполнена!", "Поздравляем!", MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnBackToBatches_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnReportDeviation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var comment = Microsoft.VisualBasic.Interaction.InputBox(
                    "Опишите отклонение:", 
                    "Сообщение об отклонении", 
                    "",
                    -1, -1);
                
                if (!string.IsNullOrEmpty(comment))
                {
                    var severity = "medium";
                    
                    await _db.ReportDeviationAsync(_selectedBatchId, _currentStep?.Id ?? 0, comment, severity);
                    MessageBox.Show("⚠️ Отклонение зафиксировано!", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnBackToBatches_Click(object sender, RoutedEventArgs e)
        {
            ProgramPanel.Visibility = Visibility.Collapsed;
            BatchesPanel.Visibility = Visibility.Visible;
            _selectedBatch = null;
            _currentStep = null;
        }
    }
}