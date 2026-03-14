namespace IntellAccount;

public static class ExtensionMethods
{
    public static bool IsImage(this IFormFile file)
    {
        string[] extensions = new string[] { ".JPG", ".GIF", ".BMP", ".JPEG", ".PNG", ".TIF", ".PDF" };
        foreach (var item in extensions)
        {
            if (file.FileName.ToUpper().Contains(item))
                return true;
        }
        return false;
    }

    public static string ToPercent(this double value)
    {
        return (value * 100).ToString("##0.000") + " %";
    }
}