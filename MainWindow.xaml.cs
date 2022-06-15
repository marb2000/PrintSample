using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Printing;
using System;
using Windows.Graphics.Printing;

namespace PrintSample
{
    public sealed partial class MainWindow : Window
    {
        private PrintManager printMan;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;
        public MainWindow()
        {
            this.InitializeComponent();
            RegisterPrint();
        }

        #region Register for printing

        void RegisterPrint()
        {
            // Register for PrintTaskRequested event
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            printMan = PrintManagerInterop.GetForWindow(hWnd);
            printMan.PrintTaskRequested += PrintTaskRequested;

            // Build a PrintDocument and register for callbacks
            printDoc = new PrintDocument();
            printDocSource = printDoc.DocumentSource;
            printDoc.Paginate += Paginate;
            printDoc.GetPreviewPage += GetPreviewPage;
            printDoc.AddPages += AddPages;
        }

        #endregion

        #region Showing the print dialog

        private async void PrintButtonClick(object sender, RoutedEventArgs e)
        {
            if (PrintManager.IsSupported())
            {
                try
                {
                    // Show print UI
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
                }
                catch
                {
                    // Printing cannot proceed at this time
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        XamlRoot = (sender as Button).XamlRoot,
                        Title = "Printing error",
                        Content = "\nSorry, printing can' t proceed at this time.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                }
            }
            else
            {
                // Printing is not supported on this device
                ContentDialog noPrintingDialog = new ContentDialog()
                {
                    XamlRoot = (sender as Button).XamlRoot,
                    Title = "Printing not supported",
                    Content = "\nSorry, printing is not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await noPrintingDialog.ShowAsync();
            }
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            // Create the PrintTask.
            // Defines the title and delegate for PrintTaskSourceRequested
            var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);

            // Handle PrintTask.Completed to catch failed print jobs
            printTask.Completed += PrintTaskCompleted;
        }

        private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args)
        {
            // Set the document source.
            args.SetSource(printDocSource);
        }

        #endregion

        #region Print preview

        private void Paginate(object sender, PaginateEventArgs e)
        {
            // As I only want to print one Rectangle, so I set the count to 1
            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            // Provide a UIElement as the print preview.
            printDoc.SetPreviewPage(e.PageNumber, this.RectangleToPrint);
        }

        #endregion

        #region Add pages to send to the printer

        private void AddPages(object sender, AddPagesEventArgs e)
        {
            printDoc.AddPage(this.RectangleToPrint);

            // Indicate that all of the print pages have been provided
            printDoc.AddPagesComplete();
        }

        #endregion

        #region Print task completed

        private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            // Notify the user when the print operation fails.
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Printing error",
                        Content = "\nSorry, failed to print.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                });
            }
        }

        #endregion
    }
}
