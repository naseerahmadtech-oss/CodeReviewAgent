using System.ComponentModel;

namespace CodeReviewAgent
{

    public class TimeTools
    {
        [Description("Gets the current date and time.")]
        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("F");
        }
    }
}
