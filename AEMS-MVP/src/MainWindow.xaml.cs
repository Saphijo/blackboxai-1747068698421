using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Threading;
using AEMSApp.Services;
using AEMSApp.Models;
using Forms = System.Windows.Forms;
using WinForms = System.Windows.Forms;

namespace AEMSApp
{
    public partial class MainWindow : Window
    {
        private readonly CurriculumProcessingService _curriculumService;
        private readonly MaterialIngestionService _materialIngestionService;
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly FileProcessingService _fileProcessingService;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _databaseService = new DatabaseService();
            _settingsService = new SettingsService(_databaseService);
            _fileProcessingService = new FileProcessingService();

            // Initialize AI API endpoint from settings
            string aiApiEndpoint = _settingsService.GetAiEndpointAsync().Result;
            
            _curriculumService = new CurriculumProcessingService(aiApiEndpoint, _fileProcessingService, _databaseService);
            _materialIngestionService = new MaterialIngestionService(aiApiEndpoint, _fileProcessingService, _databaseService);

            LoadCurriculumButton.Click += LoadCurriculumButton_Click;
            ImportFilesButton.Click += ImportFilesButton_Click;
            ImportFolderButton.Click += ImportFolderButton_Click;

            // Load saved curriculum and materials
            LoadSavedDataAsync();
        }

        private async void LoadSavedDataAsync()
        {
            try
            {
                var curriculum = await _curriculumService.LoadSavedCurriculumAsync();
                UpdateCurriculumTreeView(curriculum);

                var materials = await _materialIngestionService.LoadSavedMaterialsAsync();
                UpdateMaterialListView(materials);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading saved data: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private async void LoadCurriculumButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Select Curriculum PDF"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string pdfPath = openFileDialog.FileName;
                StatusTextBlock.Text = "Extracting text from PDF...";
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;

                try
                {
                    string extractedText = _curriculumService.ExtractTextFromPdf(pdfPath);
                    StatusTextBlock.Text = "Analyzing curriculum structure...";
                    
                    var curriculumNodes = await _curriculumService.SendTextToAiApiAsync(extractedText);
                    UpdateCurriculumTreeView(curriculumNodes);
                    
                    StatusTextBlock.Text = "Curriculum processed successfully.";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error processing curriculum: {ex.Message}", 
                        "Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                    StatusTextBlock.Text = $"Error: {ex.Message}";
                }
                finally
                {
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void ImportFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Supported files|*.docx;*.pptx;*.xlsx;*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.avi;*.mov;*.wmv",
                Multiselect = true,
                Title = "Select Materials to Import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var files = openFileDialog.FileNames;
                await ProcessMaterialFilesAsync(files);
            }
        }

        private async void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select Folder Containing Materials"
            };

            if (folderDialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string folderPath = folderDialog.SelectedPath;
                var files = _materialIngestionService.GetSupportedFilesFromFolder(folderPath);
                await ProcessMaterialFilesAsync(files);
            }
        }

        private async Task ProcessMaterialFilesAsync(IEnumerable<string> files)
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = files is ICollection<string> collection ? collection.Count : 100;
            ProgressBar.Value = 0;

            List<MaterialFileMetadata> processedFiles = new List<MaterialFileMetadata>();

            foreach (var file in files)
            {
                StatusTextBlock.Text = $"Processing {System.IO.Path.GetFileName(file)}...";
                var metadata = await _materialIngestionService.ProcessFileAsync(file);
                processedFiles.Add(metadata);
                ProgressBar.Value += 1;
                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            UpdateMaterialListView(processedFiles);
            StatusTextBlock.Text = $"Processed {processedFiles.Count} files.";
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void UpdateCurriculumTreeView(List<CurriculumNode> nodes)
        {
            CurriculumTreeView.Items.Clear();
            foreach (var node in nodes)
            {
                CurriculumTreeView.Items.Add(CreateTreeViewItem(node));
            }
        }

        private TreeViewItem CreateTreeViewItem(CurriculumNode node)
        {
            var item = new TreeViewItem
            {
                Header = $"{node.Title} ({node.Type})",
                Tag = node
            };

            foreach (var child in node.Children)
            {
                item.Items.Add(CreateTreeViewItem(child));
            }

            return item;
        }

        private void UpdateMaterialListView(List<MaterialFileMetadata> materials)
        {
            MaterialListView.Items.Clear();
            foreach (var material in materials)
            {
                MaterialListView.Items.Add(material);
            }
        }
    }
}
