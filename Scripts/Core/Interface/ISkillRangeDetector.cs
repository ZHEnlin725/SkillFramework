using System.Collections.Generic;

namespace SkillFramework
{
    public interface ISkillRangeDetector
    {
        List<ISkillTarget> Detect(SkillContext context, SkillRangeRequest request);
    }
}
