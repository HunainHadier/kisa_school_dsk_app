using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KisaSchoolMangement.Views.Section
{
    public partial class SectionsManagementView : Window
    {
        private readonly SectionService _sectionService;
        private ObservableCollection<SectionModel> _sections;
        private ObservableCollection<SectionModel> _filteredSections;

        public SectionsManagementView()
        {
            InitializeComponent();
            _sectionService = new SectionService();
            LoadSections();
        }

        private void LoadSections()
        {
            try
            {
                _sections = _sectionService.GetAllSections();
                _filteredSections = new ObservableCollection<SectionModel>(_sections);
                dgSections.ItemsSource = _filteredSections;
                UpdateSectionCount();
                txtStatus.Text = $"Loaded {_sections.Count} sections";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sections: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSectionCount()
        {
            txtSectionsCount.Text = $"📊 Total Sections: {_filteredSections.Count}";
        }

        private void BtnAddSection_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddSectionWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                LoadSections();
            }
        }

        private void BtnEditSection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SectionModel sectionModel)
            {
                var editWindow = new AddSectionWindow(sectionModel);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    LoadSections();
                }
            }
        }

        private void BtnDeleteSection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SectionModel sectionModel)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete section {sectionModel.Name}?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_sectionService.DeleteSection(sectionModel.Id))
                    {
                        MessageBox.Show("Section deleted successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadSections();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete section.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void TxtSearchSection_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSections();
        }

        private void FilterSections()
        {
            try
            {
                string searchText = txtSearchSection.Text.ToLower();

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredSections = new ObservableCollection<SectionModel>(_sections);
                }
                else
                {
                    var filtered = _sections.Where(s =>
                        (!string.IsNullOrWhiteSpace(s.Name) && s.Name.ToLower().Contains(searchText)) ||
                        (!string.IsNullOrWhiteSpace(s.ClassName) && s.ClassName.ToLower().Contains(searchText)))
                        .ToList();

                    _filteredSections = new ObservableCollection<SectionModel>(filtered);
                }

                dgSections.ItemsSource = _filteredSections;
                UpdateSectionCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering sections: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSections();
        }
    }
}
