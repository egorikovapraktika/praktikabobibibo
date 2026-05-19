using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LabModule.Services;
using LabModule.Models;

namespace LabModule
{
    public partial class MainWindow : Window
    {
        private DbService _db;
        private int _selectedBatchId;
        private int _selectedQualityId;
        private QualityControl _selectedQuality;

        public MainWindow()
        {
            InitializeComponent();
            _db = new DbService();
            
            BtnLogin.Click += BtnLogin_Click;
            BtnShowRegister.Click += BtnShowRegister_Click;
            BtnRegister.Click += BtnRegister_Click;
            BtnShowLogin.Click += BtnShowLogin_Click;
            BtnLogout.Click += BtnLogout_Click;
            BtnGoToTechModule.Click += BtnGoToTechModule_Click;
            
            BtnRefreshBatches.Click += BtnRefreshBatches_Click;
            BtnStartTest.Click += BtnStartTest_Click;
            BtnRefreshQuality.Click += BtnRefreshQuality_Click;
            BtnAddQuality.Click += BtnAddQuality_Click;
            BtnProcessSelected.Click += BtnProcessSelected_Click;
            BtnValidateTest.Click += BtnValidateTest_Click;
            BtnApproveTest.Click += BtnApproveTest_Click;
            BtnBlockTest.Click += BtnBlockTest_Click;
            BtnAddDeviation.Click += BtnAddDeviation_Click;
            BtnRefreshDeviations.Click += BtnRefreshDeviations_Click;
            
            DgBatches.SelectionChanged += DgBatches_SelectionChanged;
            DgQualityControls.SelectionChanged += DgQualityControls_SelectionChanged;
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

                TxtLoginError.Text = "Подключение к базе данных...";
                
                var user = await _db.LoginAsync(TxtLoginEmail.Text, TxtLoginPassword.Password);
                
                TxtUserInfo.Text = $"{user.FullName} (Лаборант)";
                
                LoginPanel.Visibility = Visibility.Collapsed;
                MainPanel.Visibility = Visibility.Visible;
                
                await LoadAllData();
                TxtLoginError.Text = "";
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
                    "laboratory"
                );

                MessageBox.Show($"✅ Регистрация успешна!\n\nEmail: {user.Email}\nФИО: {user.FullName}", 
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
        }

        private void BtnGoToTechModule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"C:\Users\Kab19-08\source\repos\TechModule\bin\Debug\net5.0-windows\TechModule.exe");
            }
            catch { }
        }

        private async Task LoadAllData()
        {
            await LoadBatches();
            await LoadQualityControls();
            await LoadDeviations();
            await LoadStats();
        }

        private async Task LoadBatches()
        {
            try
            {
                var batches = await _db.GetBatchesAsync();
                DgBatches.ItemsSource = batches;
                TxtPendingBatches.Text = $"На контроле: {batches.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки партий: {ex.Message}");
            }
        }

        private async Task LoadQualityControls()
        {
            try
            {
                var controls = await _db.GetQualityControlsAsync();
                DgQualityControls.ItemsSource = controls;
                TxtTotalTests.Text = $"Испытаний: {controls.Count}";
                TxtApprovedTests.Text = $"Одобрено: {controls.Count(c => c.Decision == "approved")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки испытаний: {ex.Message}");
            }
        }

        private async Task LoadDeviations()
        {
            try
            {
                var deviations = await _db.GetDeviationsAsync();
                DgDeviations.ItemsSource = deviations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отклонений: {ex.Message}");
            }
        }

        private async Task LoadStats()
        {
            try
            {
                var stats = await _db.GetDashboardStatsAsync();
                TxtCompletedBatches.Text = $"Проверено: {stats.CompletedBatchesCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private void DgBatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgBatches.SelectedItem is Batch batch)
            {
                _selectedBatchId = batch.Id;
            }
        }

        private void DgQualityControls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgQualityControls.SelectedItem is QualityControl quality)
            {
                _selectedQuality = quality;
                _selectedQualityId = quality.Id;
                
                TxtQualityId.Text = quality.Id.ToString();
                TxtSelectedBatchInfo.Text = quality.BatchId.ToString();
                TxtMeasuredValue.Text = quality.MeasuredValue?.ToString() ?? "";
                TxtStandardValue.Text = quality.StandardValue ?? "95.0-98.0";
                TxtTestComment.Text = quality.AnalystComment ?? "";
                
                if (quality.SampleType == "finished_product") CmbSampleType.SelectedIndex = 0;
                else if (quality.SampleType == "raw_material") CmbSampleType.SelectedIndex = 1;
                else CmbSampleType.SelectedIndex = 2;
                
                if (quality.ParameterName == "Концентрация") CmbParameter.SelectedIndex = 0;
                else if (quality.ParameterName == "Влажность") CmbParameter.SelectedIndex = 1;
                else if (quality.ParameterName == "pH") CmbParameter.SelectedIndex = 2;
                else CmbParameter.SelectedIndex = 0;
            }
        }

        private void BtnStartTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedBatchId == 0)
                {
                    MessageBox.Show("Выберите партию из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                MainTabControl.SelectedIndex = 2;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRefreshBatches_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
        }

        private async void BtnRefreshQuality_Click(object sender, RoutedEventArgs e)
        {
            await LoadQualityControls();
        }

        private async void BtnAddQuality_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedBatchId == 0)
                {
                    MessageBox.Show("Выберите партию из списка партий!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var control = new QualityControl
                {
                    BatchId = _selectedBatchId,
                    SampleType = "finished_product",
                    ParameterName = "Концентрация",
                    StandardValue = "95.0-98.0",
                    Unit = "%",
                    AnalystComment = ""
                };

                var result = await _db.CreateQualityControlAsync(control);
                
                if (result != null && result.Id > 0)
                {
                    MessageBox.Show($"✅ Испытание для партии {_selectedBatchId} создано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadQualityControls();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnProcessSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedQuality == null)
                {
                    MessageBox.Show("Выберите испытание из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                MainTabControl.SelectedIndex = 2;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnValidateTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtMeasuredValue.Text))
                {
                    MessageBox.Show("Введите значение!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var value = decimal.Parse(TxtMeasuredValue.Text);
                var standard = TxtStandardValue.Text;
                bool isValid = false;
                string result = "";

                if (standard == "95.0-98.0")
                {
                    if (value >= 95 && value <= 98)
                    {
                        isValid = true;
                        result = "✅ Соответствует";
                    }
                    else
                    {
                        result = "❌ Не соответствует";
                    }
                }
                else if (standard == "≤ 2.0")
                {
                    if (value <= 2.0m)
                    {
                        isValid = true;
                        result = "✅ Соответствует";
                    }
                    else
                    {
                        result = "❌ Не соответствует";
                    }
                }
                else if (standard == "6.5-7.5")
                {
                    if (value >= 6.5m && value <= 7.5m)
                    {
                        isValid = true;
                        result = "✅ Соответствует";
                    }
                    else
                    {
                        result = "❌ Не соответствует";
                    }
                }
                else
                {
                    result = "⚠️ Норматив не распознан";
                }

                TxtValidationResult.Text = result;
                
                if (isValid)
                    MessageBox.Show("✅ Значение соответствует нормативу!", "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("❌ Значение НЕ соответствует нормативу!", "Результат", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnApproveTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedQuality == null)
                {
                    MessageBox.Show("Выберите испытание из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(TxtMeasuredValue.Text))
                {
                    MessageBox.Show("Введите значение!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedQuality.MeasuredValue = decimal.Parse(TxtMeasuredValue.Text);
                _selectedQuality.StandardValue = TxtStandardValue.Text;
                _selectedQuality.SampleType = (CmbSampleType.SelectedItem as ComboBoxItem)?.Content.ToString();
                _selectedQuality.ParameterName = (CmbParameter.SelectedItem as ComboBoxItem)?.Content.ToString();
                _selectedQuality.Result = "pass";
                _selectedQuality.Decision = "approved";
                _selectedQuality.AnalystComment = TxtTestComment.Text;
                _selectedQuality.AnalysisDate = DateTime.Now;

                await _db.UpdateQualityControlAsync(_selectedQuality);
                MessageBox.Show($"✅ Испытание #{_selectedQualityId} ОДОБРЕНО!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                await LoadQualityControls();
                TxtValidationResult.Text = "";
                _selectedQuality = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnBlockTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedQuality == null)
                {
                    MessageBox.Show("Выберите испытание из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(TxtMeasuredValue.Text))
                {
                    MessageBox.Show("Введите значение!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Вы уверены, что хотите ЗАБРАКОВАТЬ испытание #{_selectedQualityId}?", 
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    _selectedQuality.MeasuredValue = decimal.Parse(TxtMeasuredValue.Text);
                    _selectedQuality.StandardValue = TxtStandardValue.Text;
                    _selectedQuality.SampleType = (CmbSampleType.SelectedItem as ComboBoxItem)?.Content.ToString();
                    _selectedQuality.ParameterName = (CmbParameter.SelectedItem as ComboBoxItem)?.Content.ToString();
                    _selectedQuality.Result = "fail";
                    _selectedQuality.Decision = "blocked";
                    _selectedQuality.AnalystComment = TxtTestComment.Text;
                    _selectedQuality.AnalysisDate = DateTime.Now;

                    await _db.UpdateQualityControlAsync(_selectedQuality);
                    MessageBox.Show($"❌ Испытание #{_selectedQualityId} ЗАБРАКОВАНО!", "Решение", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    await LoadQualityControls();
                    TxtValidationResult.Text = "";
                    _selectedQuality = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnAddDeviation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtDevBatchId.Text) || string.IsNullOrEmpty(TxtDevDesc.Text))
                {
                    MessageBox.Show("Заполните ID партии и описание!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedItem = CmbDevSeverity.SelectedItem as ComboBoxItem;
                var severity = selectedItem?.Content.ToString() ?? "Средняя";
                
                var deviation = new Deviation
                {
                    BatchId = int.Parse(TxtDevBatchId.Text),
                    DeviationType = "quality",
                    Description = TxtDevDesc.Text,
                    Severity = severity,
                    IsResolved = false
                };
                await _db.CreateDeviationAsync(deviation);
                await LoadDeviations();
                MessageBox.Show("⚠️ Отклонение добавлено!", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDevBatchId.Text = "";
                TxtDevDesc.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnRefreshDeviations_Click(object sender, RoutedEventArgs e)
        {
            await LoadDeviations();
        }
    }
}