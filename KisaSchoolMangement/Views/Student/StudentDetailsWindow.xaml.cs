using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;

namespace KisaSchoolMangement.Views.Student
{
    public partial class StudentDetailsWindow : Window
    {
        private readonly StudentService _studentService;
        private StudentModel _student;

        public StudentDetailsWindow(int studentId)
        {
            InitializeComponent();
            _studentService = new StudentService();
            LoadStudent(studentId);
        }

        private void LoadStudent(int id)
        {
            try
            {
                _student = _studentService.GetStudentById(id);
                if (_student == null)
                {
                    MessageBox.Show("Student not found.", "Not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.DialogResult = false;
                    this.Close();
                    return;
                }

                // Bind model to UI
                this.DataContext = _student;

                // Load photo if available
                if (!string.IsNullOrEmpty(_student.Photo))
                {
                    try
                    {
                        string path = _student.Photo;
                        if (!File.Exists(path))
                        {
                            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            string alt = System.IO.Path.Combine(exeDir, _student.Photo.TrimStart('\\', '/'));
                            if (File.Exists(alt)) path = alt;
                        }

                        if (File.Exists(path))
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(path, UriKind.Absolute);
                            bmp.EndInit();
                            imgPhoto.Source = bmp;
                        }
                    }
                    catch
                    {
                        // ignore image load errors
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading student details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}