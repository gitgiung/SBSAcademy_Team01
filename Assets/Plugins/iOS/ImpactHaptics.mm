#import <UIKit/UIKit.h>

// 0 가벼움, 1 중간, 2 강함. 짧은 임팩트만 울리고 시스템 긴 진동은 쓰지 않습니다.
extern "C" void SBSImpactHaptic(int style)
{
    if (@available(iOS 10.0, *))
    {
        UIImpactFeedbackStyle impact = UIImpactFeedbackStyleLight;
        if (style == 1) impact = UIImpactFeedbackStyleMedium;
        else if (style >= 2) impact = UIImpactFeedbackStyleHeavy;
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:impact];
        [generator prepare];
        [generator impactOccurred];
    }
}
