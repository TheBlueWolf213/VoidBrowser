
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VoidBrowser;

public partial class MainWindow : Window
{
    private CoreWebView2Environment? _environment;

    private readonly string _userDataFolder =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoidBrowser",
            "UserData"
        );

    private readonly Dictionary<TabItem, TextBlock> _tabTitles = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    // Initialize the shared browser environment
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_userDataFolder);

            _environment = await CoreWebView2Environment.CreateAsync(
                null,
                _userDataFolder
            );

            await AddNewTabAsync("https://www.google.com");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Could not initialize Void Browser:\n\n" + ex.Message,
                "Void Browser Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    // Get currently selected browser tab
    private WebView2? CurrentWebView =>
        (Tabs.SelectedItem as TabItem)?.Content as WebView2;

    // Create a new browser tab
    private async Task AddNewTabAsync(string url)
    {
        if (_environment == null)
            return;

        var webView = new WebView2();

        var tab = new TabItem
        {
            Content = webView
        };

        // Create tab title
        var titleText = new TextBlock
        {
            Text = "Loading...",
            Foreground = Brushes.White,
            Width = 125,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };

        _tabTitles[tab] = titleText;

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        header.Children.Add(titleText);

        // Tab close button
        var closeButton = new Button
        {
            Content = "×",
            Width = 22,
            Height = 22,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0),
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 16,
            Cursor = Cursors.Hand
        };

        closeButton.Click += (_, _) => CloseTab(tab);

        header.Children.Add(closeButton);
        tab.Header = header;

        Tabs.Items.Add(tab);
        Tabs.SelectedItem = tab;

        // Initialize WebView2 using shared browser profile
        await webView.EnsureCoreWebView2Async(_environment);

        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

        webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
        {
            UpdateTabTitle(tab, webView.CoreWebView2.DocumentTitle);
        };

        webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            UpdateTabTitle(tab, webView.CoreWebView2.DocumentTitle);

            if (Tabs.SelectedItem == tab)
                UpdateAddressBar();
        };

        webView.CoreWebView2.Navigate(url);
    }

    // Update the tab title
    private void UpdateTabTitle(TabItem tab, string title)
    {
        Dispatcher.Invoke(() =>
        {
            if (_tabTitles.TryGetValue(tab, out var text))
            {
                text.Text = string.IsNullOrWhiteSpace(title)
                    ? "New tab"
                    : title;
            }
        });
    }

    // Close a browser tab
    private async void CloseTab(TabItem tab)
    {
        if (tab.Content is WebView2 webView)
            webView.CoreWebView2?.Stop();

        Tabs.Items.Remove(tab);
        _tabTitles.Remove(tab);

        if (Tabs.Items.Count == 0)
            await AddNewTabAsync("https://google.com");
    }

    // New tab button
    private async void NewTabButton_Click(object sender, RoutedEventArgs e)
    {
        await AddNewTabAsync("https://www.google.com");
    }

    // Back button
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentWebView?.CoreWebView2?.CanGoBack == true)
            CurrentWebView.CoreWebView2.GoBack();
    }

    // Forward button
    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentWebView?.CoreWebView2?.CanGoForward == true)
            CurrentWebView.CoreWebView2.GoForward();
    }

    // Refresh button
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentWebView?.CoreWebView2?.Reload();
    }

    // Home button
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentWebView?.CoreWebView2?.Navigate("https://www.google.com");
    }

    // Address bar keyboard input
    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateFromAddressBar();
            e.Handled = true;
        }
    }

    // Navigate to a URL or search query
    private void NavigateFromAddressBar()
    {
        string input = AddressBar.Text.Trim();

        if (string.IsNullOrWhiteSpace(input))
            return;

        string target;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            target = uri.ToString();
        }
        else if (!input.Contains(' ') && input.Contains('.'))
        {
            target = "https://" + input;
        }
        else
        {
            target = "https://www.google.com/search?q="
                     + Uri.EscapeDataString(input);
        }

        CurrentWebView?.CoreWebView2?.Navigate(target);
    }

    // Update address bar when switching tabs
    private void Tabs_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.Source != Tabs)
            return;

        UpdateAddressBar();
    }

    private void UpdateAddressBar()
    {
        var webView = CurrentWebView;

        if (webView == null)
            return;

        AddressBar.Text = webView.Source?.ToString() ?? "";
    }
}
