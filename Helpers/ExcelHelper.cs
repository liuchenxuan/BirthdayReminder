using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClassIsland.BirthdayReminder.Models;
using ClosedXML.Excel;

namespace ClassIsland.BirthdayReminder.Helpers;

/// <summary>
/// Excel（.xlsx）导入导出工具。
/// 表格列顺序：姓名 | 出生日期 | 分类 | 备注 | 提醒时机(天,逗号分隔,0=当天) | 是否启用
/// 出生日期支持：
///   完整日期：2005-06-15 / 2005/6/15
///   仅月日（未知出生年份）：--06-15 / 06-15 / 6-15
/// </summary>
public static class ExcelHelper
{
    private static readonly string[] Headers =
    {
        "姓名", "出生日期", "分类", "备注", "提醒时机(天,逗号分隔,0=当天)", "是否启用(是/否)"
    };

    /// <summary>
    /// 导出人员列表到 Excel 文件
    /// </summary>
    public static void Export(string path, IEnumerable<BirthdayPerson> people)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("生日列表");

        for (var i = 0; i < Headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var p in people)
        {
            sheet.Cell(row, 1).Value = p.Name;
            sheet.Cell(row, 2).Value = p.HasKnownYear
                ? $"{p.BirthYear:D4}-{p.BirthMonth:D2}-{p.BirthDay:D2}"
                : $"--{p.BirthMonth:D2}-{p.BirthDay:D2}";
            sheet.Cell(row, 3).Value = p.Category;
            sheet.Cell(row, 4).Value = p.Notes;
            sheet.Cell(row, 5).Value = string.Join(",", p.ReminderOffsets);
            sheet.Cell(row, 6).Value = p.IsEnabled ? "是" : "否";
            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    /// <summary>
    /// 生成一个带表头和示例数据的空白导入模板
    /// </summary>
    public static void ExportTemplate(string path)
    {
        var sample = new List<BirthdayPerson>
        {
            new()
            {
                Name = "张三", BirthYear = 1990, BirthMonth = 5, BirthDay = 20,
                HasKnownYear = true, Category = "朋友", Notes = "示例数据，可以删除",
                ReminderOffsets = new System.Collections.ObjectModel.ObservableCollection<int> { 0, 1, 3, 7 }
            },
            new()
            {
                Name = "李四", BirthYear = 0, BirthMonth = 12, BirthDay = 1,
                HasKnownYear = false, Category = "同事", Notes = "未知出生年份示例",
                ReminderOffsets = new System.Collections.ObjectModel.ObservableCollection<int> { 0, 3 }
            }
        };
        Export(path, sample);
    }

    /// <summary>
    /// 从 Excel 文件导入人员列表
    /// </summary>
    /// <param name="path">Excel 文件路径</param>
    /// <param name="errors">解析失败的行信息</param>
    public static List<BirthdayPerson> Import(string path, out List<string> errors)
    {
        errors = new List<string>();
        var result = new List<BirthdayPerson>();

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var name = sheet.Cell(row, 1).GetString().Trim();
            var dateText = sheet.Cell(row, 2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(dateText))
            {
                continue; // 跳过空行
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"第 {row} 行：姓名为空，已跳过");
                continue;
            }

            if (!TryParseBirthday(dateText, out var year, out var month, out var day, out var hasYear))
            {
                errors.Add($"第 {row} 行（{name}）：无法识别的出生日期“{dateText}”，已跳过");
                continue;
            }

            var category = sheet.Cell(row, 3).GetString().Trim();
            var notes = sheet.Cell(row, 4).GetString().Trim();
            var offsetsText = sheet.Cell(row, 5).GetString().Trim();
            var enabledText = sheet.Cell(row, 6).GetString().Trim();

            var offsets = new System.Collections.ObjectModel.ObservableCollection<int>();
            if (!string.IsNullOrWhiteSpace(offsetsText))
            {
                foreach (var part in offsetsText.Split(new[] { ',', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part, out var v))
                    {
                        offsets.Add(v);
                    }
                }
            }
            if (offsets.Count == 0)
            {
                offsets = new System.Collections.ObjectModel.ObservableCollection<int> { 0, 1, 3, 7 };
            }

            result.Add(new BirthdayPerson
            {
                Name = name,
                BirthYear = year,
                BirthMonth = month,
                BirthDay = day,
                HasKnownYear = hasYear,
                Category = string.IsNullOrWhiteSpace(category) ? "其他" : category,
                Notes = notes,
                ReminderOffsets = offsets,
                IsEnabled = !enabledText.Equals("否", StringComparison.OrdinalIgnoreCase)
            });
        }

        return result;
    }

    private static bool TryParseBirthday(string text, out int year, out int month, out int day, out bool hasYear)
    {
        year = 0;
        month = 0;
        day = 0;
        hasYear = false;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        // 仅月日格式： --06-15 / 06-15 / 6-15 / 06/15
        if (text.StartsWith("--"))
        {
            text = text[2..];
        }

        var normalized = text.Replace('/', '-').Replace('.', '-');
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var d) && m is >= 1 and <= 12)
            {
                month = m;
                day = d;
                year = DateTime.Now.Year;
                hasYear = false;
                return IsValidMonthDay(month, day);
            }
            return false;
        }

        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m) && int.TryParse(parts[2], out var d))
            {
                year = y;
                month = m;
                day = d;
                hasYear = true;
                return year is >= 1900 and <= 2100 && IsValidMonthDay(month, day);
            }
            return false;
        }

        // 尝试使用标准日期解析作为兜底
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            year = dt.Year;
            month = dt.Month;
            day = dt.Day;
            hasYear = true;
            return true;
        }

        return false;
    }

    private static bool IsValidMonthDay(int month, int day)
    {
        if (month is < 1 or > 12)
        {
            return false;
        }
        // 使用 2004（闰年）作为参照年份，允许 2 月 29 日这种边界情况
        return day >= 1 && day <= DateTime.DaysInMonth(2004, month);
    }
}
