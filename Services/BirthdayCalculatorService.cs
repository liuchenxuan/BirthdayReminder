using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.BirthdayReminder.Models;

namespace ClassIsland.BirthdayReminder.Services;

/// <summary>
/// 生日计算的纯逻辑服务：计算下一次生日、倒计时天数、年龄等。
/// 本服务不涉及任何 ClassIsland 特定 API，方便独立测试与拓展。
/// 注意：本插件只支持公历（阳历）生日，不支持农历。
/// </summary>
public class BirthdayCalculatorService
{
    /// <summary>
    /// 计算某人从 <paramref name="today"/> 起下一次生日的日期。
    /// 如果生日是 2 月 29 日而当年不是闰年，则按 2 月 28 日计算。
    /// </summary>
    public DateTime GetNextBirthday(BirthdayPerson person, DateTime today)
    {
        var todayDate = today.Date;
        var month = person.BirthMonth;
        var day = person.BirthDay;

        DateTime SafeDate(int year)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, Math.Min(day, daysInMonth));
        }

        var candidate = SafeDate(todayDate.Year);
        if (candidate < todayDate)
        {
            candidate = SafeDate(todayDate.Year + 1);
        }
        return candidate;
    }

    /// <summary>
    /// 计算距离下一次生日还有多少天（0 表示今天就是生日）
    /// </summary>
    public int GetDaysUntilNextBirthday(BirthdayPerson person, DateTime today)
    {
        var next = GetNextBirthday(person, today);
        return (next.Date - today.Date).Days;
    }

    /// <summary>
    /// 计算下一次生日时将满多少周岁。如果未知出生年份，返回 null。
    /// </summary>
    public int? GetUpcomingAge(BirthdayPerson person, DateTime today)
    {
        if (!person.HasKnownYear)
        {
            return null;
        }
        var next = GetNextBirthday(person, today);
        return next.Year - person.BirthYear;
    }

    /// <summary>
    /// 计算当前实际周岁（按今天算，未到生日则未加 1 岁）。如果未知出生年份，返回 null。
    /// </summary>
    public int? GetCurrentAge(BirthdayPerson person, DateTime today)
    {
        if (!person.HasKnownYear)
        {
            return null;
        }
        var age = today.Year - person.BirthYear;
        var hasHadBirthdayThisYear = today.Month > person.BirthMonth ||
                                      (today.Month == person.BirthMonth && today.Day >= person.BirthDay);
        if (!hasHadBirthdayThisYear)
        {
            age--;
        }
        return age;
    }

    /// <summary>
    /// 判断某人今天是否是生日
    /// </summary>
    public bool IsBirthdayToday(BirthdayPerson person, DateTime today)
    {
        return person.BirthMonth == today.Month && person.BirthDay == today.Day;
    }

    /// <summary>
    /// 判断某人是否在当前月份过生日
    /// </summary>
    public bool IsBirthdayThisMonth(BirthdayPerson person, DateTime today)
    {
        return person.BirthMonth == today.Month;
    }

    /// <summary>
    /// 获取一个人生效的提醒偏移天数集合（个人自定义优先，否则使用全局默认，另外叠加个人自定义天数）
    /// </summary>
    public List<int> GetEffectiveOffsets(BirthdayPerson person, IEnumerable<int> globalDefaultOffsets)
    {
        var offsets = new List<int>(person.ReminderOffsets.Count > 0
            ? person.ReminderOffsets
            : globalDefaultOffsets);
        if (person.UseCustomOffsets && !offsets.Contains(person.CustomOffsetDays))
        {
            offsets.Add(person.CustomOffsetDays);
        }
        return offsets.Distinct().OrderBy(x => x).ToList();
    }

    /// <summary>
    /// 获取本月过生日的人员列表，按日期排序
    /// </summary>
    public List<BirthdayPerson> GetBirthdaysThisMonth(IEnumerable<BirthdayPerson> people, DateTime today)
    {
        return people
            .Where(p => IsBirthdayThisMonth(p, today))
            .OrderBy(p => p.BirthDay)
            .ToList();
    }

    /// <summary>
    /// 获取今天过生日的人员列表（今日寿星）
    /// </summary>
    public List<BirthdayPerson> GetBirthdaysToday(IEnumerable<BirthdayPerson> people, DateTime today)
    {
        return people.Where(p => IsBirthdayToday(p, today)).ToList();
    }

    /// <summary>
    /// 获取下一个即将过生日的人（如果有多个同一天，返回全部）
    /// </summary>
    public List<(BirthdayPerson Person, int Days)> GetNextUpcoming(IEnumerable<BirthdayPerson> people, DateTime today)
    {
        var list = people.Select(p => (Person: p, Days: GetDaysUntilNextBirthday(p, today))).ToList();
        if (list.Count == 0)
        {
            return list;
        }
        var minDays = list.Min(x => x.Days);
        return list.Where(x => x.Days == minDays).ToList();
    }
}
