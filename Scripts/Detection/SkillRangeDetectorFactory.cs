namespace SkillFramework
{
    public class SkillRangeDetectorFactory
    {
        public static ISkillRangeDetector CreateRangeDetector(SkillRangeDetectorConfig config)
        {
            return config switch
            {
                _ => null,
            };
        }
    }
}