using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace VisualPerformanceCounter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PerformanceCounterCategory SelectedCategory { get; set; }
        public string SelectedInstance { get; set; }
        public CounterItem SelectedCounter { get; set; }
        public List<PerformanceCounter> Counters { get; set; } = new();
        public MainWindow()
        {
            InitializeComponent();
            CategoriesListView.ItemsSource = PerformanceCounterCategory.GetCategories()
                .OrderBy(x => x.CategoryName);
            var categoryView = (CollectionView)CollectionViewSource.GetDefaultView(CategoriesListView.ItemsSource);
            categoryView.Filter = CategoryFilter;

            _ = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Render, Callback, Dispatcher.CurrentDispatcher);
        }

        private void Callback(object? sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            CountersListView.Items.Clear();
            foreach (var performanceCounter in Counters)
            {
                var item = new CounterItem
                {
                    Name = performanceCounter.CounterName,
                    Description = performanceCounter.CounterHelp
                };
                try
                {
                    item.Value = performanceCounter.NextValue().ToString("G17");
                }
                catch (Exception)
                {
                    // ignored
                }

                CountersListView.Items.Add(item);
            }
        }

        private bool CategoryFilter(object item)
        {
            if (string.IsNullOrEmpty(CategoriesSearch.Text))
                return true;
            return ((PerformanceCounterCategory)item).CategoryName.Contains(CategoriesSearch.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void CategoriesListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is PerformanceCounterCategory category)
                {
                    InstancesListView.ItemsSource = null;
                    CountersListView.ItemsSource = null;
                    SelectedInstance = null;
                    SelectedCounter = null;
                    SelectedCategory = category;
                    CategoryDescription.Text = SelectedCategory.CategoryHelp;
                    var instances = SelectedCategory.GetInstanceNames();
                    if (instances.Any())
                    {
                        InstancesListView.ItemsSource = instances.OrderBy(x => x);
                    }
                    else
                    {
                        Counters = SelectedCategory.GetCounters().ToList();
                        Refresh();
                    }
                }
            }
        }

        private void InstancesListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is string instanceName)
                {
                    SelectedInstance = instanceName;
                    Counters = SelectedCategory.GetCounters(SelectedInstance).ToList();
                    Refresh();
                }
            }
        }

        private void CountersListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is CounterItem counter)
                {
                    SelectedCounter = counter;
                    CounterDescription.Text = SelectedCounter.Description;
                }
            }
        }

        private void CategoriesSearch_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(CategoriesListView.ItemsSource).Refresh();
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"Selected Category: {SelectedCategory?.CategoryName}{Environment.NewLine}Selected Instance: {SelectedInstance ?? "NO INSTANCES"}{Environment.NewLine}Selected Counter: {SelectedCounter?.Name}");
        }
    }
}
