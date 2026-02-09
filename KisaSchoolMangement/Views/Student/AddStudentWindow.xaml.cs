using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;

namespace KisaSchoolMangement.Views.Student
{
    public partial class AddStudentWindow : Window
    {
        private readonly StudentService _studentService;
        private readonly ClassService _classService;
        private readonly SectionService _sectionService;
        private readonly StudentModel _studentToEdit;

        // Photo properties
        public string StudentPhotoPath { get; private set; }
        public string CnicPhotoPath { get; private set; }
        public string FatherCnicPhotoPath { get; private set; }
        public string MotherCnicPhotoPath { get; private set; }

        public AddStudentWindow()
        {
            InitializeComponent();
            _studentService = new StudentService();
            _classService = new ClassService();
            _sectionService = new SectionService();
            Title = "Add New Student";

            LoadClassesAndSections();
            SetDefaultValues();
        }

        public AddStudentWindow(StudentModel student) : this()
        {
            _studentToEdit = student;
            Title = "Edit Student";
            LoadStudentData();
        }

        private void LoadClassesAndSections()
        {
            try
            {
                // Load classes
                var classes = _classService.GetAllClasses();
                cmbClass.ItemsSource = classes;

                // Load sections
                var sections = _sectionService.GetAllSections();
                cmbSection.ItemsSource = sections;

                if (classes.Count > 0)
                    cmbClass.SelectedIndex = 0;
                if (sections.Count > 0)
                    cmbSection.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading classes/sections: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDefaultValues()
        {
            dpDOB.SelectedDate = DateTime.Today.AddYears(-5); // Default 5 years old
            dpAdmissionDate.SelectedDate = DateTime.Today;
            dpLeftDate.SelectedDate = null; // Left date initially empty
            txtAdmissionYear.Text = DateTime.Now.Year.ToString();
            txtFamilyMembers.Text = "1";
            txtChildPosition.Text = "1";
            txtMonthlyFee.Text = "0";
            txtFatherIncome.Text = "0";
            txtMotherIncome.Text = "0";

            // Default status
            cmbStatus.SelectedIndex = 0; // "active"
        }

        private void LoadStudentData()
        {
            if (_studentToEdit != null)
            {
                txtGrNo.Text = _studentToEdit.GrNo;
                txtStudentName.Text = _studentToEdit.StudentName;

                if (DateTime.TryParse(_studentToEdit.DOB, out DateTime dob))
                    dpDOB.SelectedDate = dob;

                cmbGender.Text = _studentToEdit.Gender;

                // Set class and section
                foreach (var item in cmbClass.Items)
                {
                    if (item is ClassModel classItem && classItem.Id == _studentToEdit.ClassId)
                    {
                        cmbClass.SelectedItem = item;
                        break;
                    }
                }

                foreach (var item in cmbSection.Items)
                {
                    if (item is SectionModel sectionItem && sectionItem.Id == _studentToEdit.SectionId)
                    {
                        cmbSection.SelectedItem = item;
                        break;
                    }
                }

                if (DateTime.TryParse(_studentToEdit.AdmissionDate, out DateTime admissionDate))
                    dpAdmissionDate.SelectedDate = admissionDate;

                txtAdmissionYear.Text = _studentToEdit.AdmissionYear.ToString();
                txtAdmissionClass.Text = _studentToEdit.AdmissionClass;
                txtFatherName.Text = _studentToEdit.FatherName;
                txtFamilyCode.Text = _studentToEdit.FamilyCode.ToString();
                txtDistrictCode.Text = _studentToEdit.DistrictCode.ToString();
                cmbSyed.SelectedItem = _studentToEdit.IsSyed ? cmbSyed.Items[0] : cmbSyed.Items[1];
                txtFamilyMembers.Text = _studentToEdit.FamilyMembers.ToString();
                txtChildPosition.Text = _studentToEdit.ChildPosition.ToString();

                if (!string.IsNullOrEmpty(_studentToEdit.StudentCategory))
                    cmbStudentCategory.Text = _studentToEdit.StudentCategory;

                // CNIC Information
                txtChildCNIC.Text = _studentToEdit.ChildCNICNumber;
                txtFatherCNIC.Text = _studentToEdit.FatherCNIC;
                txtFatherOccupation.Text = _studentToEdit.FatherOccupation;
                txtFatherIncome.Text = _studentToEdit.FatherMonthlyIncome.HasValue ? _studentToEdit.FatherMonthlyIncome.Value.ToString() : "0";

                // Mother Information
                txtMotherName.Text = _studentToEdit.MotherName;
                txtMotherCNIC.Text = _studentToEdit.MotherCNICNumber;
                txtMotherIncome.Text = _studentToEdit.MotherMonthlyIncome.HasValue ? _studentToEdit.MotherMonthlyIncome.Value.ToString() : "0";

                // Guardian Information
                txtGuardianName.Text = _studentToEdit.GuardianName;
                txtGuardianPhone.Text = _studentToEdit.GuardianPhone;
                txtMotherPhone.Text = _studentToEdit.MotherPhone;
                txtMotherWhatsapp.Text = _studentToEdit.MotherWhatsapp;

                // Residential Information
                if (!string.IsNullOrEmpty(_studentToEdit.HomeStatus))
                    cmbHomeStatus.Text = _studentToEdit.HomeStatus;

                txtAddress.Text = _studentToEdit.Address;
                txtScholarshipReason.Text = _studentToEdit.ReasonOfScholarship;

                if (_studentToEdit.MonthlyFee.HasValue)
                    txtMonthlyFee.Text = _studentToEdit.MonthlyFee.Value.ToString();

                chkIsActive.IsChecked = _studentToEdit.IsActive;

                // NEW FIELDS: Left Information
                txtLeftMonth.Text = _studentToEdit.LeftMonth;

                if (!string.IsNullOrEmpty(_studentToEdit.LeftDate) && DateTime.TryParse(_studentToEdit.LeftDate, out DateTime leftDate))
                    dpLeftDate.SelectedDate = leftDate;

                txtLeftReason.Text = _studentToEdit.ReasonOfSchoolLeft;

                // Set Status
                if (!string.IsNullOrEmpty(_studentToEdit.Status))
                {
                    foreach (ComboBoxItem item in cmbStatus.Items)
                    {
                        if (item.Content.ToString() == _studentToEdit.Status)
                        {
                            cmbStatus.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Load photos if they exist
                // Assuming images are stored relative to app directory, or absolute paths if legacy.
                // We will try to resolve relative paths to absolute for display.
                
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                if (!string.IsNullOrEmpty(_studentToEdit.Photo))
                {
                    StudentPhotoPath = _studentToEdit.Photo; // Store relative or absolute
                    LoadImage(GetFullPath(StudentPhotoPath), imgStudentPhoto); 
                }

                if (!string.IsNullOrEmpty(_studentToEdit.CNICImage))
                {
                    CnicPhotoPath = _studentToEdit.CNICImage;
                    LoadImage(GetFullPath(CnicPhotoPath), imgCnicPhoto);
                }

                if (!string.IsNullOrEmpty(_studentToEdit.FatherCNICPhoto))
                {
                    FatherCnicPhotoPath = _studentToEdit.FatherCNICPhoto;
                    LoadImage(GetFullPath(FatherCnicPhotoPath), imgFatherCnicPhoto);
                }

                if (!string.IsNullOrEmpty(_studentToEdit.MotherCNICPhoto))
                {
                    MotherCnicPhotoPath = _studentToEdit.MotherCNICPhoto;
                    LoadImage(GetFullPath(MotherCnicPhotoPath), imgMotherCnicPhoto);
                }
            }
        }

        private string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Path.IsPathRooted(path)) return path; // Already absolute
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        // Photo Upload Methods
        private void btnUploadStudentPhoto_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowsePhoto(imgStudentPhoto);
            if (path != null) StudentPhotoPath = path; // Temporarily store source path
        }

        private void btnUploadCnicPhoto_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowsePhoto(imgCnicPhoto);
             if (path != null) CnicPhotoPath = path;
        }

        private void btnUploadFatherCnicPhoto_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowsePhoto(imgFatherCnicPhoto);
            if (path != null) FatherCnicPhotoPath = path;
        }

        private void btnUploadMotherCnicPhoto_Click(object sender, RoutedEventArgs e)
        {
             var path = BrowsePhoto(imgMotherCnicPhoto);
             if (path != null) MotherCnicPhotoPath = path;
        }

        private string BrowsePhoto(System.Windows.Controls.Image imageControl)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg; *.jpeg; *.png; *.bmp",
                Title = "Select Photo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadImage(openFileDialog.FileName, imageControl);
                    return openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return null;
        }

        private void LoadImage(string filePath, System.Windows.Controls.Image imageControl)
        {
            if (File.Exists(filePath))
            {
                try {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Important for freeing file lock
                    bitmap.EndInit();
                    imageControl.Source = bitmap;
                } catch { }
            }
        }

        private string SaveImageToAppFolder(string sourcePath, string subFolder, string prefix)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;
            
            // If the path is already relative (meaning it's already in our DB/folder), return it as is
            if (!Path.IsPathRooted(sourcePath)) return sourcePath;

            // Define destination folder
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string destFolder = Path.Combine(appDir, "StudentImages", subFolder);

            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            string extension = Path.GetExtension(sourcePath);
            string fileName = $"{prefix}_{Guid.NewGuid()}{extension}";
            string destPath = Path.Combine(destFolder, fileName);

            try
            {
                File.Copy(sourcePath, destPath, true);
                // Return relative path
                return Path.Combine("StudentImages", subFolder, fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image {prefix}: {ex.Message}");
                return null;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    // Process Images first
                    string grNoSafe = txtGrNo.Text.Trim().Replace("/", "_").Replace("\\", "_"); // Sanitize GR no for filename if needed

                    // Only save if the path is rooted (new file selected)
                    // If it's relative, it means we loaded existing one and didn't change it, or we already saved it.
                    // Actually SaveImageToAppFolder handles the check.
                    
                    string savedStudentPhoto = SaveImageToAppFolder(StudentPhotoPath, "Profiles", "Student");
                    string savedCnicPhoto = SaveImageToAppFolder(CnicPhotoPath, "CNICs", "ChildCNIC");
                    string savedFatherCnicPhoto = SaveImageToAppFolder(FatherCnicPhotoPath, "CNICs", "FatherCNIC");
                    string savedMotherCnicPhoto = SaveImageToAppFolder(MotherCnicPhotoPath, "CNICs", "MotherCNIC");


                    var student = new StudentModel
                    {
                        GrNo = txtGrNo.Text.Trim(),
                        StudentName = txtStudentName.Text.Trim(),
                        DOB = dpDOB.SelectedDate?.ToString("yyyy-MM-dd"),
                        Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? cmbGender.Text,
                        ClassId = (cmbClass.SelectedItem as ClassModel)?.Id,
                        SectionId = (cmbSection.SelectedItem as SectionModel)?.Id,
                        AdmissionDate = dpAdmissionDate.SelectedDate?.ToString("yyyy-MM-dd"),
                        AdmissionYear = int.TryParse(txtAdmissionYear.Text, out int year) ? year : DateTime.Now.Year,
                        AdmissionClass = txtAdmissionClass.Text.Trim(),
                        FatherName = txtFatherName.Text.Trim(),
                        FamilyCode = int.TryParse(txtFamilyCode.Text, out int familyCode) ? familyCode : 0,
                        DistrictCode = int.TryParse(txtDistrictCode.Text, out int districtCode) ? districtCode : 0,
                        IsSyed = (cmbSyed.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Syed",
                        FamilyMembers = int.TryParse(txtFamilyMembers.Text, out int familyMembers) ? familyMembers : 1,
                        ChildPosition = int.TryParse(txtChildPosition.Text, out int childPosition) ? childPosition : 1,
                        StudentCategory = (cmbStudentCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() == "-- Select Category --" ?
                                         null : (cmbStudentCategory.SelectedItem as ComboBoxItem)?.Content?.ToString(),

                        // CNIC Information
                        ChildCNICNumber = txtChildCNIC.Text.Trim(),
                        FatherCNIC = txtFatherCNIC.Text.Trim(),
                        FatherOccupation = txtFatherOccupation.Text.Trim(),
                        FatherMonthlyIncome = decimal.TryParse(txtFatherIncome.Text, out decimal fatherIncome) ? fatherIncome : (decimal?)null,

                        // Mother Information
                        MotherName = txtMotherName.Text.Trim(),
                        MotherCNICNumber = txtMotherCNIC.Text.Trim(),
                        MotherMonthlyIncome = decimal.TryParse(txtMotherIncome.Text, out decimal motherIncome) ? motherIncome : (decimal?)null,

                        // Guardian Information
                        GuardianName = txtGuardianName.Text.Trim(),
                        GuardianPhone = txtGuardianPhone.Text.Trim(),
                        MotherPhone = txtMotherPhone.Text.Trim(),
                        MotherWhatsapp = txtMotherWhatsapp.Text.Trim(),

                        // Residential Information
                        HomeStatus = (cmbHomeStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() == "-- Select Status --" ?
                                    null : (cmbHomeStatus.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        Address = txtAddress.Text.Trim(),
                        ReasonOfScholarship = txtScholarshipReason.Text.Trim(),
                        MonthlyFee = decimal.TryParse(txtMonthlyFee.Text, out decimal fee) ? fee : (decimal?)null,

                        // NEW: Left Information
                        LeftMonth = txtLeftMonth.Text.Trim(),
                        LeftDate = dpLeftDate.SelectedDate?.ToString("yyyy-MM-dd"),
                        ReasonOfSchoolLeft = txtLeftReason.Text.Trim(),
                        Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "active",

                        // Photo paths (Saved Relative Paths)
                        Photo = savedStudentPhoto,
                        CNICImage = savedCnicPhoto,
                        FatherCNICPhoto = savedFatherCnicPhoto,
                        MotherCNICPhoto = savedMotherCnicPhoto,

                        IsActive = chkIsActive.IsChecked ?? true,
                        CreatedAt = _studentToEdit?.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    bool success;

                    if (_studentToEdit != null)
                    {
                        student.Id = _studentToEdit.Id;
                        success = _studentService.UpdateStudent(student);
                    }
                    else
                    {
                        success = _studentService.AddStudent(student);
                    }

                    if (success)
                    {
                        MessageBox.Show($"Student {(_studentToEdit != null ? "updated" : "added")} successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to {(_studentToEdit != null ? "update" : "add")} student. Please try again.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving student: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ValidateForm()
        {
            // Validate GR Number
            if (string.IsNullOrWhiteSpace(txtGrNo.Text))
            {
                MessageBox.Show("Please enter GR number!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGrNo.Focus();
                return false;
            }

            // Validate Student Name
            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            {
                MessageBox.Show("Please enter student name!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStudentName.Focus();
                return false;
            }

            // Validate DOB
            if (dpDOB.SelectedDate == null)
            {
                MessageBox.Show("Please select date of birth!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpDOB.Focus();
                return false;
            }

            // Validate Guardian Name
            if (string.IsNullOrWhiteSpace(txtGuardianName.Text))
            {
                MessageBox.Show("Please enter guardian name!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGuardianName.Focus();
                return false;
            }

            // Validate Guardian Phone
            if (string.IsNullOrWhiteSpace(txtGuardianPhone.Text))
            {
                MessageBox.Show("Please enter guardian phone number!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGuardianPhone.Focus();
                return false;
            }

            // Validate Address
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Please enter address!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAddress.Focus();
                return false;
            }

            // Validate Father Name
            if (string.IsNullOrWhiteSpace(txtFatherName.Text))
            {
                MessageBox.Show("Please enter father name!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFatherName.Focus();
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}