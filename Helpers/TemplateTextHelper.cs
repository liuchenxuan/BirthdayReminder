using System;
using System.Globalization;
using ClassIsland.BirthdayReminder.Models;
using ClassIsland.BirthdayReminder.Services;

namespace ClassIsland.BirthdayReminder.Helpers;

/// <summary>
/// 提醒文本模板占位符替换工具。
/// 支持的占位符：
///   {name}     姓名
///   {age}      年龄数字（若未知出生年份则为空字符串）
///   {ageText}  形如 “即将年满 18 岁，” 的年龄描述文本（若未知出生年份则为空字符串）
///   {category} 分类
///   {days}     距离生日的天数
///   {date}     生日日期（MM月dd日）
/// </summary>
public static class TemplateTextHelper
{
    public static string Render(string template, BirthdayPerson person, int days, BirthdayCalculatorService calculator, DateTime today)
    {
        var age = calculator.GetUpcomingAge(person, today);
        var ageText = age.HasValue ? $"即将年满 {age.Value} 岁，" : "";
        var dateText = $"{person.BirthMonth:D2}月{person.BirthDay:D2}日";

        return template
            .Replace("{name}", person.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{age}", age?.ToString(CultureInfo.InvariantCulture) ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{ageText}", ageText, StringComparison.OrdinalIgnoreCase)
            .Replace("{category}", person.Category, StringComparison.OrdinalIgnoreCase)
            .Replace("{days}", days.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", dateText, StringComparison.OrdinalIgnoreCase);
    }
}
