using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Text;

namespace HanaGami;

public partial class MainWindow : Window
{
    private List<Note> listNotes = new List<Note>();
    private Note? selectedNote = null;

    private string htmlContent = "";

    public MainWindow()
    {
        InitializeComponent();
        LoadNotes();
    }

    private void LoadNotes(string keyword = "")
    {
        listNotes = Exec.load_list();
        NotesListBox.ItemsSource = listNotes;
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        var searchText = SearchTextBox.Text ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            listNotes = Exec.load_local(searchText);
        }
        else
        {
            listNotes = Exec.load_list();
        }
        NotesListBox.ItemsSource = listNotes;
    }

    private void OnShareClick(object? sender, RoutedEventArgs e)
    {
        if (selectedNote == null || sender is not Button button)
            return;

        var menu = new ContextMenu();
        var pdfMenuItem = new MenuItem { Header = "PDF" };
        pdfMenuItem.Click += OnExportPdfClick;
        var htmlMenuItem = new MenuItem { Header = "HTML" };
        htmlMenuItem.Click += OnExportHtmlClick;
        menu.Items.Add(pdfMenuItem);
        menu.Items.Add(htmlMenuItem);
        menu.Open(button);
    }

    private async void OnExportPdfClick(object? sender, RoutedEventArgs e)
    {
        if (selectedNote == null)
            return;
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export Note as PDF",
            SuggestedFileName = $"{selectedNote.Name}.pdf",
        });
        if (file != null)
        {
            Exec.to_pdf(htmlContent, file.Path.LocalPath);
        }
    }

    private async void OnExportHtmlClick(object? sender, RoutedEventArgs e)
    {
        if (selectedNote == null)
            return;
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export Note as HTML",
            SuggestedFileName = $"{selectedNote.Name}.html",
        });
        if (file != null)
        {
            Exec.save_html(htmlContent, file.Path.LocalPath);
        }
    }

    private void OnNoteSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NotesListBox.SelectedItem is Note note)
        {
            selectedNote = note;
            ShowNoteDetail(note);
        } else {
            selectedNote = null;
            ShowEmptyState();
        }
    }

    private void ShowNoteDetail(Note note)
    {
        EmptyStatePanel.IsVisible = false;
        AddPanel.IsVisible = false;
        DetailPanel.IsVisible = true;
        
        NoteTitleText.Text = note.Name;
        SharedIcon.IsVisible = note.IsShared;
        var html = note.ReturnHtml();
        htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset=""utf-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
            <style>
                body {{
                    font-family: sans-serif;
                    padding: 20px;
                    line-height: 1.6;
                    color: #333;
                }}
                pre {{
                background: #f6f8fa;
                padding: 16px;
                border-radius: 6px;
                overflow-x: auto;
                }}
                code {{
                    background: #f6f8fa;
                    padding: 2px 6px;
                    border-radius: 3px;
                    font-family: 'Courier New', monospace;
                }}
                h1, h2, h3, h4, h5, h6 {{
                    margin-top: 24px;
                    margin-bottom: 16px;
                    font-weight: 600;
                }}
                blockquote {{
                    border-left: 4px solid #dfe2e5;
                    padding-left: 16px;
                    color: #6a737d;
                }}
                .page-break {{
                    page-break-before: always;
                }}
                pre, blockquote {{
                    page-break-inside: avoid;
                }}
                table {{
                    border-collapse: collapse;
                    width: 100%;
                    margin: 16px 0;
                    page-break-inside: avoid;
                }}
                table th {{
                    background-color: #f6f8fa;
                    font-weight: 600;
                    text-align: left;
                    padding: 6px 13px;
                    border: 1px solid #dfe2e5;
                }}
                table td {{
                    padding: 6px 13px;
                    border: 1px solid #dfe2e5;
                }}
            </style>
            <script>
                document.addEventListener('DOMContentLoaded', function() {{
                    document.addEventListener('click', function(e) {{
                        if (e.target.tagName === 'A') {{
                            var href = e.target.getAttribute('href');
                            if (href && !href.startsWith('http://') && !href.startsWith('https://')) {{
                                e.preventDefault();
                                return false;
                            }}
                        }}
                    }}, true);
                }});
            </script>
        </head>
        <body>
            {html}
        </body>
        </html>";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlContent));
        NoteWebView.Url = new Uri($"data:text/html;base64,{base64}");
    }

    private void ShowEmptyState()
    {
        DetailPanel.IsVisible = false;
        AddPanel.IsVisible = false;
        EmptyStatePanel.IsVisible = true;
    }

    private void addNote_Click(object? sender, RoutedEventArgs e)
    {
        DetailPanel.IsVisible = false;
        AddPanel.IsVisible = true;
        EmptyStatePanel.IsVisible = false;
    }

    private void setting_Click(object? sender, RoutedEventArgs e)
    {
        //
    }

    private void OnAddNoteSaveClick(object? sender, RoutedEventArgs e)
    {
        //
    }

    private void duumy(object? sender, RoutedEventArgs e)
    {
        // Dummy handler to prevent Avalonia warning about no handlers
    }
}