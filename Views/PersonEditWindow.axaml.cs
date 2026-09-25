using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.BirthdayReminder.Models;

namespace ClassIsland.BirthdayReminder.Views;

/// <summary>
/// 添加/编辑一条生日记录的窗口。通过 <see cref="Window.ShowDialog{TResult}"/> 使用，
/// 保存后返回编辑好的 <see cref="BirthdayPerson"/>，取消则返回 null。
/// </summary>
public partial class PersonEditWindow : Window
{
    private readonly bool _isEditMode;
    private string? _existingId;

    public PersonEditWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 创建一个添加/编辑窗口
    /// </summary>
    /// <param name="existing">要编辑的记录，为 null 时表示新增</param>
    /// <param name="existingCategories">已有分类列表，用于下拉选择</param>
    public PersonEditWindow(BirthdayPerson? existing, IEnumerable<string> existingCategories) : this()
    {
        _isEditMode = existing != null;
        _existingId = existing?.Id;
        HeaderText.Text = _isEditMode ? "编辑生日记录" : "添加生日记录";
        CategoryBox.ItemsSource = new ObservableCollection<string>(existingCategories.Distinct());

        var today = DateTime.Now;
        NameBox.Text = existing?.Name ?? "";
        KnownYearCheck.IsChecked = existing?.HasKnownYear ?? true;
        YearBox.Value = existing?.BirthYear ?? today.Year - 10;
        MonthBox.Value = existing?.BirthMonth ?? today.Month;
        DayBox.Value = existing?.BirthDay ?? today.Day;
        CategoryBox.SelectedItem = existing?.Category;
        if (existing != null)
        {
            CategoryBox.Text = existing.Category;
        }
        NotesBox.Text = existing?.Notes ?? "";
        EnabledCheck.IsChecked = existing?.IsEnabled ?? true;

        var offsets = existing?.ReminderOffsets ?? new ObservableCollection<int> { 0, 1, 3, 7 };
        Offset0Check.IsChecked = offsets.Contains(0);
        Offset1Check.IsChecked = offsets.Contains(1);
        Offset3Check.IsChecked = offsets.Contains(3);
        Offset7Check.IsChecked = offsets.Contains(7);
        Offset30Check.IsChecked = offsets.Contains(30);

        CustomOffsetCheck.IsChecked = existing?.UseCustomOffsets ?? false;
        CustomOffsetBox.Value = existing?.CustomOffsetDays ?? 15;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            NameBox.Focus();
            return;
        }

        var offsets = new ObservableCollection<int>();
        if (Offset0Check.IsChecked == true) offsets.Add(0);
        if (Offset1Check.IsChecked == true) offsets.Add(1);
        if (Offset3Check.IsChecked == true) offsets.Add(3);
        if (Offset7Check.IsChecked == true) offsets.Add(7);
        if (Offset30Check.IsChecked == true) offsets.Add(30);
        if (offsets.Count == 0)
        {
            offsets.Add(0);
        }

        var category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? "其他" : CategoryBox.Text!.Trim();

        var person = new BirthdayPerson
        {
            Id = _existingId ?? System.Guid.NewGuid().ToString("N"),
            Name = name,
            HasKnownYear = KnownYearCheck.IsChecked == true,
            BirthYear = (int)(YearBox.Value ?? DateTime.Now.Year - 10),
            BirthMonth = Math.Clamp((int)(MonthBox.Value ?? 1), 1, 12),
            BirthDay = Math.Clamp((int)(DayBox.Value ?? 1), 1, 31),
            Category = category,
            Notes = NotesBox.Text ?? "",
            ReminderOffsets = offsets,
            UseCustomOffsets = CustomOffsetCheck.IsChecked == true,
            CustomOffsetDays = (int)(CustomOffsetBox.Value ?? 15),
            IsEnabled = EnabledCheck.IsChecked == true
        };

        Close(person);
    }
}
