using System.Drawing;
using Svg;
using Svg.Skia;
using Svg.Transforms;
using VectorToCursor.Rendering.Animation;

namespace VectorToCursor.Tests.Rendering.Animation;

/// <summary>Plays translated CSS animations with Svg.Skia's SMIL engine and compares the values with what a browser shows.</summary>
public sealed class CssAnimationPlaybackTests
{
    private const double Tolerance = 0.05;

    [Fact]
    public void Grow_FollowsItsCubicBezier()
    {
        SvgCircle arc = BusyArcAt(TimeSpan.FromMilliseconds(220));

        Assert.Equal(84.10, arc.StrokeDashArray[0].Value, Tolerance);
        Assert.Equal(275.90, arc.StrokeDashArray[1].Value, Tolerance);
    }

    [Theory]
    [InlineData(220, -20)]
    [InlineData(550, -56.80)]
    public void Move_EasesEachIntervalWithItsOwnTimingFunction(int milliseconds, double expectedOffset)
    {
        SvgCircle arc = BusyArcAt(TimeSpan.FromMilliseconds(milliseconds));

        Assert.Equal(expectedOffset, arc.StrokeDashOffset.Value, Tolerance);
    }

    [Theory]
    [InlineData(1099, 0x30AA51)]
    [InlineData(1101, 0xEF4D38)]
    [InlineData(2201, 0x458AFF)]
    [InlineData(3301, 0xFFBE00)]
    [InlineData(5499, 0xEF4D38)]
    public void Color_SwitchesAtEveryFifthOfTheLoop(int milliseconds, int expectedRgb)
    {
        SvgCircle arc = BusyArcAt(TimeSpan.FromMilliseconds(milliseconds));

        Color stroke = Assert.IsType<SvgColourServer>(arc.Stroke).Colour;
        Assert.Equal(expectedRgb, stroke.ToArgb() & 0xFFFFFF);
    }

    [Fact]
    public void Spin_RotatesLinearly()
    {
        SvgCircle arc = BusyArcAt(TimeSpan.FromMilliseconds(1375));

        SvgRotate rotation = Assert.IsType<SvgRotate>(Assert.Single(arc.Transforms));
        Assert.Equal(90, rotation.Angle, Tolerance);
    }

    [Fact]
    public void DelayOfEndlessAnimation_ShiftsItsPhaseInsteadOfHoldingTheStart()
    {
        SvgCircle arc = ArcAt(".arc { animation: fade 1s linear 250ms infinite; } @keyframes fade { from { opacity: 1; } to { opacity: 0; } }", TimeSpan.FromMilliseconds(100));

        Assert.Equal(0.15, arc.Opacity, Tolerance);
    }

    private static SvgCircle BusyArcAt(TimeSpan time) => ArcAt(TestSvg.BusyCss, time);

    private static SvgCircle ArcAt(string css, TimeSpan time)
    {
        SvgDocument document = TestSvg.Create(css, TestSvg.Arc);
        CssAnimationTranslator.Translate(document);
        SmilTimingReader.ReadAndNormalize(document);
        using SvgAnimationController controller = new(document, wallclockTimeOrigin: null);
        return controller.CreateAnimatedDocument(time).GetElementById<SvgCircle>("arc");
    }
}
