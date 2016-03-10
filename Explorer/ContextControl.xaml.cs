using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for ContextControl.xaml
    /// </summary>
    public partial class ContextControl : UserControl
    {
        private readonly ISubject<List<DimensionValues>> _querySubject = new Subject<List<DimensionValues>>();
        private readonly ISubject<HashSet<Measure>> _getSubject = new Subject<HashSet<Measure>>();
        private readonly ISubject<Measure> _measureSubject = new Subject<Measure>();

        public ContextControl()
        {
            InitializeComponent();
        }

        public IDisposable Subscribe(IObserver<List<DimensionValues>> observer)
        {
            return _querySubject.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<HashSet<Measure>> observer)
        {
            return _getSubject.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<Measure> observer)
        {
            return _measureSubject.Subscribe(observer);
        }

        public void Reset(List<DimensionValues> dimensionValuesList)
        {
            RemoveDimensionControls();

            foreach(var dimensionValues in dimensionValuesList)
            {
                var control = DimensionControl.Create(dimensionValues);
                control.Subscribe(_measureSubject);
                stackPanel.Children.Add(control);
            }
        }

        private void RemoveDimensionControls()
        {
            var count = stackPanel.Children.Count;
            stackPanel.Children.RemoveRange(1, count - 1);
        }

        private List<DimensionValues> GetDimensionValuesList()
        {
            return stackPanel
                .Children
                .OfType<DimensionControl>()
                .Select(dimensionControl => dimensionControl.GetDimensionValues())
                .ToList();
        }

        private HashSet<Measure> GetMeasures()
        {
            var request = new HashSet<Measure>();

            foreach (var control in stackPanel.Children)
            {
                var dimensionControl = control as DimensionControl;
                var measure = dimensionControl?.GetMeasure();
                if( measure != null )
                    request.Add(measure);
            }

            return request;
        }

        private void OnClickQuery(object sender, RoutedEventArgs e)
        {
            _querySubject.OnNext( GetDimensionValuesList() );
        }
        private void OnClickGet(object sender, RoutedEventArgs e)
        {
            _getSubject.OnNext(GetMeasures());
        }
    }
}
