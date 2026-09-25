using ClassIsland.BirthdayReminder.Models;

namespace ClassIsland.BirthdayReminder.Views.SettingsPages;

/// <summary>
/// 用于在生日列表 DataGrid 中展示一行数据的只读快照。
/// 每次刷新列表时重新生成，避免复杂的双向绑定与实时重算逻辑纠缠在一起。
/// </summary>
public class BirthdayRowViewModel
{
    public BirthdayPerson Person { get; }

    public string Name => Person.Name;
    public string BirthdayText => Person.DisplayBirthday;
    public string Category => Person.Category;
    public string Notes => Person.Notes;
    public string AgeText { get; }
    public string CountdownText { get; }
    public string OffsetsText { get; }
    public string EnabledText => Person.IsEnabled ? "是" : "否";

    public BirthdayRowViewModel(BirthdayPerson person, string ageText, string countdownText, string offsetsText)
    {
        Person = person;
        AgeText = ageText;
        CountdownText = countdownText;
        OffsetsText = offsetsText;
    }
}
