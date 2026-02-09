using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KisaSchoolMangement.Views.Class
{
    public partial class ClassesManagementView : Window
    {
        private readonly ClassService _classService;
        private ObservableCollection<ClassModel> _classes;
        private ObservableCollection<ClassModel> _filteredClasses;

        public ClassesManagementView()
        {
            InitializeComponent();
            _classService = new ClassService();
            LoadClasses();
        }

        private void LoadClasses()
        {
            try
            {
                _classes = _classService.GetAllClasses();
                _filteredClasses = new ObservableCollection<ClassModel>(_classes);
                dgClasses.ItemsSource = _filteredClasses;
                UpdateClassCount();
                txtStatus.Text = $"Loaded {_classes.Count} classes";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading classes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateClassCount()
        {
            txtClassesCount.Text = $"📊 Total Classes: {_filteredClasses.Count}";
        }

        private void BtnAddClass_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddClassWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                LoadClasses();
            }
        }

        private void BtnEditClass_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ClassModel classModel)
            {
                var editWindow = new AddClassWindow(classModel);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    LoadClasses();
                }
            }
        }

        private void BtnDeleteClass_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ClassModel classModel)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {classModel.Name}?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_classService.DeleteClass(classModel.Id))
                    {
                        MessageBox.Show("Class deleted successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadClasses();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete class.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void TxtSearchClass_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterClasses();
        }

        private void FilterClasses()
        {
            try
            {
                string searchText = txtSearchClass.Text.ToLower();

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredClasses = new ObservableCollection<ClassModel>(_classes);
                }
                else
                {
                    var filtered = _classes.Where(c =>
                        (!string.IsNullOrWhiteSpace(c.Name) && c.Name.ToLower().Contains(searchText)) ||
                        (!string.IsNullOrWhiteSpace(c.Code) && c.Code.ToLower().Contains(searchText)) ||
                        (!string.IsNullOrWhiteSpace(c.Description) && c.Description.ToLower().Contains(searchText)))
                        .ToList();

                    _filteredClasses = new ObservableCollection<ClassModel>(filtered);
                }

                dgClasses.ItemsSource = _filteredClasses;
                UpdateClassCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering classes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClasses();
        }
    }
}
