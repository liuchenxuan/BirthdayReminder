namespace ClassIsland.BirthdayReminder.Models;

/// <summary>
/// 一条已发送提醒的记录，用于防止同一天重复提醒。
/// Key 由 “人员ID + 提醒偏移天数 + 触发日期” 组成。
/// </summary>
public class ReminderLogEntry
{
    /// <summary>
    /// 人员 ID
    /// </summary>
    public string PersonId { get; set; } = "";

    /// <summary>
    /// 触发这条提醒的偏移天数（0 = 当天）
    /// </summary>
    public int OffsetDays { get; set; }

    /// <summary>
    /// 触发日期（yyyy-MM-dd）
    /// </summary>
    public string FiredOnDate { get; set; } = "";
}
