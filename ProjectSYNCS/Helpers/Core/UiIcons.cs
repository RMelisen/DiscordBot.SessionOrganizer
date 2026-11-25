namespace ProjectSYNCS.Helpers;

// Interface marks drawn by tools/item-art (ui.<key>), uploaded as application emojis like the item
// icons (ItemEmojis, prefix ui_). Each falls back to its Unicode until the upload is done.
public static class UiIcons
{
    public const string CheckKey = "ui.check";

    private static readonly string[] Keys = [CheckKey];

    public static bool IsIconKey(string key) => Keys.Contains(key);

    // A small green check, for « owned » and « ready » beside a list line: ✅ fills the whole emoji
    // box and outweighs the price next to it.
    public static string Check => ItemEmojis.For(CheckKey) ?? "✅";
}
