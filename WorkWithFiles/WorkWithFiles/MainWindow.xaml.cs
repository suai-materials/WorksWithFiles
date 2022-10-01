using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WorkWithFiles;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Создаём и очищаем файл
        var streamWriter = new StreamWriter("files.txt");
        streamWriter.Write("");
        streamWriter.Close();
        InitializeComponent();
        // Заполняем поле дисков
        foreach (var drive in DriveInfo.GetDrives())
            HardDrivesComboBox.Items.Add(drive.Name);
        HardDrivesComboBox.SelectedIndex = 0;
    }

    private void HardDrives_OnSelected(object sender, RoutedEventArgs e)
    {
        FoldersListBox.Items.Clear();
        FilesListBox.Items.Clear();
        var hardDrive = HardDrivesComboBox.SelectedItem.ToString()!;
        foreach (var directory in Directory.GetDirectories(hardDrive))
            FoldersListBox.Items.Add(directory);
        // Получаем информацию о диске
        var driveInfo = DriveInfo.GetDrives().Where(info => info.Name == hardDrive).ToList()[0];
        HardDriveInformation.Text = @$"Объем диска: {driveInfo.TotalSize / (1024 * 1024)} MB
Свободное пространство: {driveInfo.TotalFreeSpace / (1024 * 1024)} MB";
    }


    private void FoldersListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FilesListBox.Items.Clear();
        FolderInformation.Text = "";
        try
        {
            var path = FoldersListBox.SelectedItem.ToString()!;
            var directoryInfo = new DirectoryInfo(path);
            FolderInformation.Text = @$"Полное название каталога: {directoryInfo.Name}
Время создания каталога: {directoryInfo.CreationTime}
Корневой каталог: {directoryInfo.Parent?.Name ?? string.Empty}";
            foreach (var file in Directory.GetFiles(path))
                FilesListBox.Items.Add(file);
        }
        catch (NullReferenceException)
        {
            /* При очистке поле Selection сбрасывается и нас выкидывает сюда, так как стоит проверка на null */
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Доступ запрещён к этой папке.");
        }
    }

    /* На открытие любого файла или при закрытии приложения
     производим проверку все ли записанные файлы, открывались 10 секунд назад
     */
    private void fileOpened(string? path = null)
    {
        HashSet<(string, DateTime)> notDelete = new();
        if (path != null)
            notDelete.Add((path, DateTime.Now));
        var streamReader = new StreamReader("files.txt");
        while (!streamReader.EndOfStream)
        {
            var line = streamReader.ReadLine()!;
            if (line.Trim() != "")
            {
                var runnedTime = DateTime.Parse(line.Split(',')[1]);
                if (DateTime.Now - runnedTime < TimeSpan.FromSeconds(10))
                    notDelete.Add((line.Split(',')[0], runnedTime));
            }
        }

        streamReader.Close();
        var streamWriter = new StreamWriter("files.txt");
        foreach (var pair in notDelete)
            streamWriter.WriteLine($"{pair.Item1},{pair.Item2}");
        streamWriter.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        fileOpened();
        base.OnClosed(e);
    }
    
    private void FilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var path = FilesListBox.SelectedItem.ToString()!;
            new Process
            {
                StartInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                }
            }.Start();
            fileOpened(path);
            FilesListBox.SelectedItem = null;
        }
        catch (NullReferenceException)
        {
        }
    }
}