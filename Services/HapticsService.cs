using Microsoft.Maui.Devices;

namespace Dadchanger.Services;

public sealed class HapticsService : IHapticsService
{
	public void Tap()
	{
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}
}
