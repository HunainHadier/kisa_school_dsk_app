using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KisaSchoolMangement.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DashboardService _dashboardService;
        private ObservableCollection<DashboardStat> _statistics = new ObservableCollection<DashboardStat>();
        private ObservableCollection<RecentActivity> _recentActivities = new ObservableCollection<RecentActivity>();
        private int _totalUsers;
        private int _totalStudents;
        private int _totalDonors;
        private int _totalTeachers;

        public ObservableCollection<DashboardStat> Statistics
        {
            get => _statistics;
            set
            {
                _statistics = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RecentActivity> RecentActivities
        {
            get => _recentActivities;
            set
            {
                _recentActivities = value;
                OnPropertyChanged();
            }
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set
            {
                _totalUsers = value;
                OnPropertyChanged();
            }
        }

        public int TotalStudents
        {
            get => _totalStudents;
            set
            {
                _totalStudents = value;
                OnPropertyChanged();
            }
        }

        public int TotalDonors
        {
            get => _totalDonors;
            set
            {
                _totalDonors = value;
                OnPropertyChanged();
            }
        }

        public int TotalTeachers
        {
            get => _totalTeachers;
            set
            {
                _totalTeachers = value;
                OnPropertyChanged();
            }
        }

        public double PaidFeePercentage { get; set; } = 120; // just visual demo
        public double PendingFeePercentage { get; set; } = 60;
        public string FeeSummary { get; set; } = "Paid 70%, Pending 30%";

        public ICommand NavigateToStudentRegistrationCommand { get; set; }
        public ICommand NavigateToFeeCollectionCommand { get; set; }
        public ICommand NavigateToAttendanceCommand { get; set; }
        public ICommand NavigateToExamEntryCommand { get; set; }
        public ICommand NavigateToDonorCommand { get; set; }

        public DashboardViewModel()
        {
            _dashboardService = new DashboardService();
            LoadDashboardData();

            // Commands
            NavigateToStudentRegistrationCommand = new RelayCommand(_ => Navigate("StudentRegistration"));
            NavigateToFeeCollectionCommand = new RelayCommand(_ => Navigate("FeeCollection"));
            NavigateToAttendanceCommand = new RelayCommand(_ => Navigate("Attendance"));
            NavigateToExamEntryCommand = new RelayCommand(_ => Navigate("Exam"));
            NavigateToDonorCommand = new RelayCommand(_ => Navigate("Donor"));
        }

        private void LoadDashboardData()
        {
            var counts = _dashboardService.GetCounts();

            TotalStudents = counts.TotalStudents;
            TotalTeachers = counts.TotalTeachers;
            TotalDonors = counts.TotalDonors;
            TotalUsers = counts.TotalUsers;

            Statistics = new ObservableCollection<DashboardStat>
            {
                new DashboardStat
                {
                    Title = "Students",
                    Count = counts.TotalStudents,
                    Description = "Total Registered",
                    Color = "#3498db"
                },
                new DashboardStat
                {
                    Title = "Teachers",
                    Count = counts.TotalTeachers,
                    Description = "Active",
                    Color = "#2ecc71"
                },
                new DashboardStat
                {
                    Title = "Donors",
                    Count = counts.TotalDonors,
                    Description = "Active Donors",
                    Color = "#e74c3c"
                },
                new DashboardStat
                {
                    Title = "Pending Fees",
                    Count = counts.PendingFees,
                    Description = "Awaiting Payments",
                    Color = "#f39c12"
                }
            };

            RecentActivities = _dashboardService.GetRecentActivities();
        }

        private void Navigate(string page)
        {
            System.Windows.MessageBox.Show($"Navigating to {page} page...");
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => canExecute == null || canExecute(parameter);
        public void Execute(object parameter) => execute(parameter);
        public event EventHandler CanExecuteChanged;
    }
}
