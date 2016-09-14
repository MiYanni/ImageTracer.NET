namespace ImageTracerNet
{
    internal class InterpolationPoint : Point<double>
    {
        public Heading Direction { get; set; } = Heading.Center;
    }
}
