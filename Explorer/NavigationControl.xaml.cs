using System;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for NavigationControl.xaml
    /// </summary>
    public partial class NavigationControl : UserControl
    {
        private readonly ISubject<Uri> _refreshSubject = new Subject<Uri>(); 

        public NavigationControl()
        {
            InitializeComponent();
        }

        public IDisposable Subscribe(IObserver<Uri> observer)
        {
            return _refreshSubject.Subscribe(observer);
        }

        private void OnClickRefresh(object sender, RoutedEventArgs e)
        {
            Uri address;
            if( Uri.TryCreate(cmbServerUri.Text, UriKind.RelativeOrAbsolute, out address) )
                _refreshSubject.OnNext(address);
        }
    }
}
