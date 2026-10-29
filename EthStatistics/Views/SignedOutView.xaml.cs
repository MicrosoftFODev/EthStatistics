using System;
using System.Windows;
using System.Windows.Controls;

namespace EthStatistics.Views
{
    public partial class SignedOutView : UserControl
    {
        public event EventHandler?
            SignInRequested;


        public SignedOutView()
        {
            InitializeComponent();
        }


        private void SignInButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SignInRequested?.Invoke(
                this,
                EventArgs.Empty
            );
        }


        public void SetLoading(
            bool isLoading)
        {
            SignInButton.IsEnabled =
                !isLoading;

            LoadingPanel.Visibility =
                isLoading
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}