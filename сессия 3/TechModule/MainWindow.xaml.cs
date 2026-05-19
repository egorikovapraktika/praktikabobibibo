using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TechModule.Services;

namespace TechModule
{
    public partial class MainWindow : Window
    {
        private ApiService _api;
        private string _currentCaptchaCode;
        private int _selectedOrderId;

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService();
            
            BtnLogin.Click += BtnLogin_Click;
            BtnShowRegister.Click += BtnShowRegister_Click;
            BtnRegister.Click += BtnRegister_Click;
            BtnShowLogin.Click += BtnShowLogin_Click;
            BtnLogout.Click += BtnLogout_Click;
            BtnRefreshCaptcha.Click += BtnRefreshCaptcha_Click;
            BtnRefreshRegCaptcha.Click += BtnRefreshRegCaptcha_Click;
            
            BtnAddProduct.Click += BtnAddProduct_Click;
            BtnAddRecipe.Click += BtnAddRecipe_Click;
            BtnAddTechMap.Click += BtnAddTechMap_Click;
            BtnAddOrder.Click += BtnAddOrder_Click;
            BtnStartBatch.Click += BtnStartBatch_Click;
            BtnRefreshBatches.Click += BtnRefreshBatches_Click;
            BtnAddDeviation.Click += BtnAddDeviation_Click;
            BtnRefreshDeviations.Click += BtnRefreshDeviations_Click;
            BtnRefreshNotifications.Click += BtnRefreshNotifications_Click;
            BtnRefreshAll.Click += BtnRefreshAll_Click;
            
            BtnReportBatches.Click += BtnReportBatches_Click;
            BtnReportDeviations.Click += BtnReportDeviations_Click;
            BtnReportQuality.Click += BtnReportQuality_Click;
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCaptcha();
            await LoadRegCaptcha();
            
            // Проверка связи с API
            var isConnected = await _api.TestConnection();
            if (!isConnected)
            {
                MessageBox.Show("Не удалось подключиться к API!\n\nУбедитесь, что API запущен на http://localhost:5000", 
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadCaptcha()
        {
            var (imageBytes, code) = CaptchaGenerator.GenerateCaptcha();
            _currentCaptchaCode = code;
            
            var bitmap = new BitmapImage();
            using (var ms = new System.IO.MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            ImgCaptcha.Source = bitmap;
        }

        private async Task LoadRegCaptcha()
        {
            var (imageBytes, code) = CaptchaGenerator.GenerateCaptcha();
            _currentCaptchaCode = code;
            
            var bitmap = new BitmapImage();
            using (var ms = new System.IO.MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            ImgRegCaptcha.Source = bitmap;
        }

        private async void BtnRefreshCaptcha_Click(object sender, RoutedEventArgs e) => await LoadCaptcha();
        private async void BtnRefreshRegCaptcha_Click(object sender, RoutedEventArgs e) => await LoadRegCaptcha();

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtCaptcha.Text.ToUpper() != _currentCaptchaCode.ToUpper())
                {
                    TxtLoginError.Text = "Неверный код с картинки!";
                    await LoadCaptcha();
                    return;
                }

                TxtLoginError.Text = "Подключение к API...";
                var result = await _api.LoginAsync(TxtLoginEmail.Text, TxtLoginPassword.Password);
                
                TxtUserInfo.Text = $"👤 {result.User.FullName} ({result.User.Role})";
                
                LoginPanel.Visibility = Visibility.Collapsed;
                MainPanel.Visibility = Visibility.Visible;
                
                await LoadAllData();
                TxtLoginError.Text = "";
            }
            catch (Exception ex)
            {
                TxtLoginError.Text = $"Ошибка: {ex.Message}";
                await LoadCaptcha();
            }
        }

        private void BtnShowRegister_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
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

                if (TxtRegCaptcha.Text.ToUpper() != _currentCaptchaCode.ToUpper())
                {
                    TxtRegError.Text = "Неверный код с картинки!";
                    await LoadRegCaptcha();
                    return;
                }

                var roleMap = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"Технолог", "technologist"},
                    {"Оператор", "operator"},
                    {"Лаборант", "laboratory"},
                    {"Администратор", "admin"},
                    {"Начальник производства", "manager"}
                };
                
                var selectedRole = (CmbRegRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                var role = roleMap[selectedRole];

                TxtRegError.Text = "Регистрация...";
                await _api.RegisterAsync(TxtRegEmail.Text, TxtRegPassword.Password, TxtRegFullName.Text, role);
                
                MessageBox.Show("✅ Регистрация прошла успешно!", "Успех");
                
                LoginPanel.Visibility = Visibility.Visible;
                RegisterPanel.Visibility = Visibility.Collapsed;
                TxtLoginEmail.Text = TxtRegEmail.Text;
                TxtRegError.Text = "";
            }
            catch (Exception ex)
            {
                TxtRegError.Text = ex.Message;
                await LoadRegCaptcha();
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

        private async Task LoadAllData()
        {
            await LoadProducts();
            await LoadRecipes();
            await LoadTechMaps();
            await LoadOrders();
            await LoadBatches();
            await LoadDeviations();
            await LoadNotifications();
        }

        private async Task LoadProducts()
        {
            try { DgProducts.ItemsSource = await _api.GetProductsAsync(); } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Products error: {ex.Message}"); }
        }

        private async Task LoadRecipes()
        {
            try { DgRecipes.ItemsSource = await _api.GetRecipesAsync(); } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Recipes error: {ex.Message}"); }
        }

        private async Task LoadTechMaps()
        {
            try { DgTechMaps.ItemsSource = await _api.GetTechMapsAsync(); } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"TechMaps error: {ex.Message}"); }
        }

        private async Task LoadOrders()
        {
            try 
            { 
                var orders = await _api.GetProductionOrdersAsync();
                DgOrders.ItemsSource = orders;
                TxtPendingOrders.Text = orders.Count(o => o.Status == "planned").ToString();
            } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Orders error: {ex.Message}"); }
        }

        private async Task LoadBatches()
        {
            try 
            { 
                var batches = await _api.GetBatchesAsync();
                DgBatches.ItemsSource = batches;
                TxtActiveBatches.Text = batches.Count(b => b.Status == "running").ToString();
                TxtCompletedBatches.Text = batches.Count(b => b.Status == "completed").ToString();
            } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Batches error: {ex.Message}"); }
        }

        private async Task LoadDeviations()
        {
            try 
            { 
                var deviations = await _api.GetDeviationsAsync();
                DgDeviations.ItemsSource = deviations;
                TxtDeviations.Text = deviations.Count(d => !d.IsResolved).ToString();
            } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Deviations error: {ex.Message}"); }
        }

        private async Task LoadNotifications()
        {
            try { DgNotifications.ItemsSource = await _api.GetNotificationsAsync(); } 
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Notifications error: {ex.Message}"); }
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var product = new Product
                {
                    Name = TxtProductName.Text,
                    Type = TxtProductType.Text,
                    Form = TxtProductForm.Text,
                    Status = "active"
                };
                await _api.CreateProductAsync(product);
                await LoadProducts();
                TxtProductName.Text = TxtProductType.Text = TxtProductForm.Text = "";
                MessageBox.Show("✅ Продукт добавлен!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnAddRecipe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var recipe = new Recipe
                {
                    Name = TxtRecipeName.Text,
                    ProductName = TxtRecipeProduct.Text,
                    Version = 1,
                    Status = "draft",
                    OutputQuantityKg = decimal.TryParse(TxtRecipeOutput.Text, out var qty) ? qty : 0
                };
                await _api.CreateRecipeAsync(recipe);
                await LoadRecipes();
                TxtRecipeName.Text = TxtRecipeProduct.Text = TxtRecipeOutput.Text = "";
                MessageBox.Show("✅ Рецептура добавлена!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnAddTechMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var techMap = new TechMap
                {
                    Name = TxtTechMapName.Text,
                    RecipeId = int.Parse(TxtTechMapRecipeId.Text),
                    Version = 1,
                    Status = "draft"
                };
                await _api.CreateTechMapAsync(techMap);
                await LoadTechMaps();
                TxtTechMapName.Text = TxtTechMapRecipeId.Text = "";
                MessageBox.Show("✅ Технологическая карта добавлена!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var order = new ProductionOrder
                {
                    RecipeId = int.Parse(TxtOrderRecipeId.Text),
                    PlannedQuantityKg = decimal.Parse(TxtOrderQty.Text),
                    PlannedStart = DtOrderStart.SelectedDate ?? DateTime.Now,
                    PlannedEnd = DtOrderEnd.SelectedDate ?? DateTime.Now.AddDays(1),
                    Status = "planned"
                };
                await _api.CreateProductionOrderAsync(order);
                await LoadOrders();
                TxtOrderRecipeId.Text = TxtOrderQty.Text = "";
                MessageBox.Show("✅ Заказ создан!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void DgOrders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DgOrders.SelectedItem is ProductionOrder order)
                _selectedOrderId = order.Id;
        }

        private async void BtnStartBatch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedOrderId == 0)
                {
                    MessageBox.Show("⚠️ Выберите заказ!", "Внимание");
                    return;
                }

                var order = DgOrders.SelectedItem as ProductionOrder;
                var techMaps = await _api.GetTechMapsAsync();
                var techMap = techMaps.FirstOrDefault(t => t.RecipeId == order.RecipeId);

                if (techMap == null)
                {
                    MessageBox.Show("⚠️ Нет техкарты!", "Ошибка");
                    return;
                }

                await _api.StartBatchAsync(_selectedOrderId, order.RecipeId, techMap.Id);
                await LoadBatches();
                await LoadOrders();
                MessageBox.Show("✅ Партия запущена!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnRefreshBatches_Click(object sender, RoutedEventArgs e) => await LoadBatches();
        private async void BtnRefreshDeviations_Click(object sender, RoutedEventArgs e) => await LoadDeviations();
        private async void BtnRefreshNotifications_Click(object sender, RoutedEventArgs e) => await LoadNotifications();
        private async void BtnRefreshAll_Click(object sender, RoutedEventArgs e) => await LoadAllData();

        private async void BtnAddDeviation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var severityMap = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"Низкая", "low"},
                    {"Средняя", "medium"},
                    {"Высокая", "high"},
                    {"Критическая", "critical"}
                };
                
                var deviation = new Deviation
                {
                    BatchId = int.Parse(TxtDevBatchId.Text),
                    DeviationType = TxtDevType.Text,
                    Description = TxtDevDesc.Text,
                    Severity = severityMap[(CmbDevSeverity.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString()],
                    IsResolved = false
                };
                await _api.CreateDeviationAsync(deviation);
                await LoadDeviations();
                TxtDevBatchId.Text = TxtDevType.Text = TxtDevDesc.Text = "";
                MessageBox.Show("⚠️ Отклонение зафиксировано!", "Уведомление");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnReportBatches_Click(object sender, RoutedEventArgs e)
        {
            var batches = await _api.GetBatchesAsync();
            DgStats.ItemsSource = batches;
            MessageBox.Show($"📊 Партий: {batches.Count}", "Отчет");
        }

        private async void BtnReportDeviations_Click(object sender, RoutedEventArgs e)
        {
            var deviations = await _api.GetDeviationsAsync();
            DgStats.ItemsSource = deviations;
            MessageBox.Show($"⚠️ Отклонений: {deviations.Count}", "Отчет");
        }

        private async void BtnReportQuality_Click(object sender, RoutedEventArgs e)
        {
            // Для отчета по качеству
            MessageBox.Show("🔬 Отчет по качеству", "Отчет");
        }
    }
}